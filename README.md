# Mini Hacker News

[![build](https://github.com/Nemet360/mini-hacker-news/actions/workflows/build.yml/badge.svg)](https://github.com/Nemet360/mini-hacker-news/actions/workflows/build.yml)

A small posts API on .NET 8. Posts are stored persistently, and `GET /posts`
returns every stored post as JSON.

That is the whole of the current scope, on purpose. The tree holds **no** `POST`,
no validation layer, no Swagger, no health endpoint, no authentication, no
pagination and no seed data: those are milestones M2 onwards in the
[roadmap](#roadmap) and are written down rather than built. What is here is built
to the standard the rest will be held to - one commit per step, a test with every
behaviour, CI on every push.

---

## Contents

* [Quick start](#quick-start)
* [Build it yourself](#build-it-yourself)
* [Architecture](#architecture)
* [The data model](#the-data-model)
* [Design decisions](#design-decisions)
* [Tests and coverage](#tests-and-coverage)
* [Continuous integration](#continuous-integration)
* [Roadmap](#roadmap)
* [Repository conventions](#repository-conventions)
* [Filling the store with real data](#filling-the-store-with-real-data)

---

## Quick start

```bash
git clone https://github.com/Nemet360/mini-hacker-news.git
cd mini-hacker-news
dotnet run --project src/MiniHn.Api
```

```bash
curl -i http://localhost:5080/posts
```

```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

[]
```

With rows in the store the body is the array itself:

```json
[{"id":1,"content":"first post"},{"id":2,"content":"second post"}]
```

The host creates `src/MiniHn.Api/data/minihn.db` if it is missing and applies
pending migrations before it serves, so a fresh clone needs no setup step.

---

## Build it yourself

### Prerequisites

| Need | Version | Notes |
|---|---|---|
| .NET SDK | **8.0.404** | pinned in `global.json`, `rollForward: latestFeature` |
| git | any | the commit hooks are plain `bash` |
| Node | 20 (optional) | only for running `commitlint` locally |

Nothing else: the store is a local SQLite file, so there is no database server to
install, no container to start and no connection string to configure.

Install the SDK:

```bash
# Windows
winget install Microsoft.DotNet.SDK.8

# any OS, no administrator rights (installs under $HOME/.dotnet)
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --version 8.0.404
```

Verify the pin resolves - run inside the repository, which is where `global.json`
applies:

```bash
dotnet --version      # 8.0.404
```

### Build, test, run

```bash
dotnet tool restore                 # dotnet-ef, reportgenerator (versions pinned in .config)
dotnet restore --locked-mode        # fails if a lock file and a package version disagree
dotnet build -c Release
dotnet test
dotnet run --project src/MiniHn.Api
```

One step more, once per clone, so the commit rules apply locally too:

```bash
git config core.hooksPath .githooks
```

### If port 5080 is taken

The development port is 5080 (`src/MiniHn.Api/Properties/launchSettings.json`).
To run elsewhere, bypass the launch profile:

```bash
ASPNETCORE_URLS=http://localhost:5085 dotnet run --no-launch-profile --project src/MiniHn.Api
```

### Database commands

```bash
dotnet ef migrations list   --project src/MiniHn.Api
dotnet ef database update   --project src/MiniHn.Api   # the host does this at start anyway
dotnet ef migrations add <Name> --project src/MiniHn.Api -o Persistence/Migrations
```

### The whole gate, locally

```bash
dotnet format --verify-no-changes                         # style is a build error, not a review comment
bash scripts/test-hooks.sh                                # 15 commit-message vectors
bash scripts/readme-smoke.sh                              # runs this README's own commands
KEEP_DATA=1 bash scripts/readme-smoke.sh                  # ... without deleting a store you filled
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

---

## Architecture

One web project and one test project. A three-project solution for a single query
would be ceremony, so the **folders and namespaces inside the project mirror the
projects this will become** - extracting `MiniHn.Domain` and
`MiniHn.Infrastructure` later is a file move plus two `.csproj` edits, with no
renames.

```
                       HTTP
                        │
                        ▼
        ┌───────────────────────────────┐
        │  Features/Posts               │   MiniHn.Api.Features.Posts
        │  PostsEndpoints  MapGet       │   minimal API endpoint group
        │  PostResponse    wire shape   │   the contract, not the entity
        └───────────────┬───────────────┘
                        │ AppDbContext, injected
                        ▼
        ┌───────────────────────────────┐
        │  Persistence                  │   MiniHn.Infrastructure.Persistence
        │  AppDbContext                 │   DbSet<Post>
        │  Configurations/              │   table, key, constraints
        │  Migrations/                  │   EF migrations + model snapshot
        └───────────────┬───────────────┘
                        │ EF Core 8, SQLite provider
                        ▼
        ┌───────────────────────────────┐
        │  Domain                       │   MiniHn.Domain
        │  Post { long Id, Content }    │   no EF attributes, no HTTP types
        └───────────────────────────────┘
                        │
                        ▼
              data/minihn.db  (SQLite file)
```

```
src/MiniHn.Api/
  Program.cs                                  host: config → DbContext → migrate → MapPosts
  Domain/Post.cs                              the entity, persistence-ignorant
  Persistence/AppDbContext.cs                 the only DbContext
  Persistence/Configurations/
      PostConfiguration.cs                    posts table, pk_posts, length CHECK
  Persistence/Migrations/                      InitialCreate + model snapshot (generated)
  Features/Posts/PostsEndpoints.cs            MapGet("/posts")
  Features/Posts/PostResponse.cs              { id, content }
  appsettings.json                            ConnectionStrings:Posts

tests/MiniHn.Api.Tests/
  ApiFactory.cs                               the real host over a caller-supplied SQLite file
  TempDatabase.cs                             a unique temp .db per test, deleted on dispose
  HostTests.cs                                an unknown route is 404
  ConfigurationTests.cs                       no connection string → the host refuses to start
  PostTests.cs                                the entity
  PostConfigurationTests.cs                   migration, schema, the CHECK constraint
  StartupMigrationTests.cs                    a fresh file is created and migrated at start
  GetPostsTests.cs                            empty, populated, ordering, no-tracking, restart
```

**The request path, start to finish.** `Program.cs` reads
`ConnectionStrings:Posts`, refuses to start without it, registers
`AppDbContext` on the SQLite provider, creates the database directory, applies
pending migrations, then maps the endpoint group. A `GET /posts` resolves a
scoped `AppDbContext`, reads `Posts` with `AsNoTracking()` ordered by `Id`,
projects each row into `PostResponse` in the SQL projection (so the entity never
leaves the data layer) and returns `TypedResults.Ok`.

**Dependency direction.** `Features` → `Persistence` → `Domain`, never back.
`Domain/Post.cs` has no EF attribute and no HTTP type on it; the mapping lives in
`PostConfiguration`, which is where the table name, the key name and the length
constraint are declared.

---

## The data model

```sql
CREATE TABLE posts (
    id      INTEGER NOT NULL CONSTRAINT pk_posts PRIMARY KEY AUTOINCREMENT,
    content TEXT    NOT NULL,
    CONSTRAINT ck_posts_content_length CHECK (length(content) BETWEEN 1 AND 10000)
);
```

`id` is 64-bit from the first commit. The `CHECK` is a *schema* bound, not
application validation: there is no write path in this milestone for a validator
to sit on, and a column with no bound is a column that accepts a 2 GB row.

---

## Design decisions

**SQLite.** The requirement is a persistent store. SQLite is a single file, so a
clone runs with nothing installed, and EF Core hides it behind the same API
PostgreSQL uses later. The connection string is configuration, so the provider
swap at M10 touches no application code.

**A real migration from the first table, never `EnsureCreated`.** The schema is
reviewable in a diff and replayable against a database that already holds rows.
`EnsureCreated` can only create a database that does not exist yet, which makes
the second schema change a manual job. CI runs
`dotnet ef migrations has-pending-model-changes`, so the model and the snapshot
cannot drift apart unnoticed.

**Migrate at start-up.** Persistence must not depend on a step the reader has to
be told about. With one process and one small schema this is correct; a
multi-instance deployment moves the migrate step into the release pipeline, and
that is called out in M15.

**`long` identity.** SQLite stores `INTEGER` either way, and widening an `int`
key after rows exist is a table rewrite.

**A response record, not the entity.** `PostResponse` is the wire contract. The
table and the JSON are then free to change independently, and the projection
happens in SQL rather than in memory.

**Ordered by `id`.** An unordered `SELECT` is only accidentally stable. A
reviewer comparing two runs must not see a different array because SQLite chose
another plan.

**`AsNoTracking()`.** Nothing on this path writes, so the change tracker is pure
cost - and `Get_posts_does_not_track_entities` pins it.

**The in-memory EF provider is banned**, not merely avoided:
`Directory.Build.props` fails the build if
`Microsoft.EntityFrameworkCore.InMemory` is ever referenced. The requirement
under test is *persistence*, and a provider a second host cannot reopen is unable
to express it. Tests run against SQLite - the engine that ships - using a unique
temporary **file** per test.

---

## Tests and coverage

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
dotnet tool run reportgenerator \
  -reports:'coverage/**/coverage.cobertura.xml' \
  -targetdir:coverage/report -reporttypes:TextSummary
cat coverage/report/Summary.txt
```

Current state of `main`: **11 tests, line coverage 100%, branch coverage 100%**
(the number CI prints in its job summary).

`coverlet.runsettings` excludes `Persistence/Migrations/**`. That is deliberate
and it is the only exclusion: a generated `Down()` nobody calls would otherwise
set the coverage number, while the migration's actual effect *is* asserted -
`Migration_applies_to_an_empty_database_and_creates_posts_table` reads
`sqlite_master` and checks the table, `AUTOINCREMENT` and the named constraint.

What the suite buys, test by test:

| Test | What breaks it |
|---|---|
| `Host_boots_and_unknown_route_returns_404` | an unplanned route appearing |
| `Host_refuses_to_start_without_a_connection_string` | a host that boots storeless and 500s per request |
| `Post_requires_content_and_defaults_id_to_zero` | the caller assigning identity |
| `Context_round_trips_a_post_through_a_temp_sqlite_file` | a broken mapping |
| `Migration_applies_to_an_empty_database_and_creates_posts_table` | schema drift from the configuration |
| `Content_longer_than_the_bound_is_refused_by_the_store` | the length `CHECK` being dropped |
| `Fresh_database_file_is_created_and_migrated_on_host_start` | losing the no-setup-step promise |
| `Get_posts_on_empty_database_returns_200_and_empty_array` | `204`, `null`, or a wrapper object |
| `Get_posts_returns_every_stored_post_with_id_and_content` | a field rename or a casing change |
| `Posts_survive_a_host_restart` | storage that is not actually persistent |
| `Get_posts_does_not_track_entities` | a read path that tracks |

---

## Continuous integration

Two workflows, because they answer different questions.

**`build.yml` - the project's build.** On every push to every branch, on `v*`
tags and on demand: the pinned SDK, `dotnet restore --locked-mode`, a Release
build, the migration-drift check, the tests with coverage printed into the job
summary, this README's own commands via `scripts/readme-smoke.sh`, a
`dotnet publish`, and the published output plus the coverage report uploaded as
artifacts.

**`ci.yml` - the pull-request gate.** Four independent jobs, so a red light names
its own cause:

| Job | Fails when |
|---|---|
| `commitlint` | a commit on the branch is not a Conventional Commit, or a hook vector regresses |
| `build-test` | the build, the migration-drift check or a test fails |
| `format` | `dotnet format --verify-no-changes` finds anything |
| `vulnerable-packages` | a vulnerable or deprecated package, or a gitleaks hit |
| `coverage-gate` | line coverage dropped more than 0.5pp below `main` |

`scripts/coverage_gate.py` deliberately has three answers rather than two: a
missing baseline, or nothing to measure, is *"we could not look"* and passes - so
the first pull request of a repository can go green - while a measured drop
fails.

---

## Roadmap

M0 and M1 are implemented (tag `v0.1.0`). Everything below the line is specified
and not built. "Gate" is the coverage floor that milestone's pull request must
clear; the ratchet never lets a later milestone lower it.

| # | Milestone | Status | Tag | Gate (line / branch) |
|---|---|---|---|---|
| M0 | Repository and tooling | **done** | - | host-boots test green |
| M1 | Persistent posts and `GET /posts` | **done** | `v0.1.0` | 85% / 75% - measured **100% / 100%** |
| M2 | `POST /posts` with validation and OpenAPI | planned | `v0.2.0` | 88% |
| M3 | `GET /posts/{id}` | planned | `v0.3.0` | 88% |
| M4 | Update and delete | planned | `v0.4.0` | 88% |
| M5 | Pagination (keyset cursor) | planned | `v0.5.0` | 88% |
| M6 | Sorting and Hacker News ranking | planned | `v0.6.0` | 88% |
| M7 | Authentication and authorisation | planned | `v0.7.0` | 90% |
| M8 | Voting | planned | `v0.8.0` | 90% |
| M9 | Comments | planned | `v0.9.0` | 90% |
| M10 | PostgreSQL | planned | `v0.10.0` | 90% |
| M11 | Caching | planned | `v0.11.0` | 90% |
| M12 | Rate limiting | planned | `v0.12.0` | 90% |
| M13 | Observability | planned | `v0.13.0` | 90% |
| M14 | Containerisation | planned | `v0.14.0` | 90% |
| M15 | CI/CD, release and deploy | planned | `v0.15.0` | 90% |
| M16 | Load testing | planned | `v0.16.0` | 90% |
| M17 | Maintenance mode | planned | `v0.17.0` | 90% |
| M18 | Hardening and 1.0 | planned | `v1.0.0` | 90% |

Headlines of what each planned milestone adds: M2 request validation plus a
snapshot-asserted `openapi.v1.verified.json`; M5 a keyset `?cursor=` rather than
`OFFSET`, which degrades with depth; M7 ES256 with JWKS and Argon2id password
hashing; M10 the provider swap behind the same tests; M11 ETags and a feed
`Cache-Control` with `s-maxage` and `stale-if-error`; M12 a limiter partitioned
on the token subject.

---

## Repository conventions

One step, one branch, one commit, one pull request - see `CONTRIBUTING.md`.

* **Conventional Commits**, enforced by `.githooks/commit-msg` locally and
  re-checked over the whole branch in CI. The hook's rules are *tested*
  (`scripts/test-hooks.sh`, 15 vectors) rather than trusted; merge and revert
  subjects, which git composes, are exempt.
* **Every behavioural commit carries its test**, in the same commit.
* **`pre-push`** runs format, build and tests, so CI is not the first place a
  mistake shows up.
* **Style is a build error.** `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`,
  `EnforceCodeStyleInBuild`, and `IDE0005`/`IDE0055` raised to error in
  `.editorconfig`.
* **Reproducible restores.** Central package management
  (`Directory.Packages.props`) plus committed lock files, with CI restoring in
  `--locked-mode`.
* **Branches.** `milestone/m00-repo-and-tooling` and `milestone/m01-list-posts`
  are the two milestone branches merged into `main`; `tooling/hn-crawler` is the
  development tool below.

---

## Filling the store with real data

`GET /posts` is more convincing on real rows than on hand-typed ones, so there is
a crawler for the public Hacker News API - and it lives on the
**`tooling/hn-crawler`** branch, deliberately outside `main` and outside
`v0.1.0`. The assignment asks for persistent storage and `GET /posts` and for
nothing more; a seed tool inside the submitted tree would be exactly that "more".

```bash
git switch tooling/hn-crawler
dotnet run --project tools/MiniHn.Crawler -- --count 30
dotnet run --project src/MiniHn.Api
curl -s http://localhost:5080/posts
```

It reads `v0/topstories.json` and `v0/item/<id>.json` from the official key-less
API - no scraping and no credentials - paces its requests, keeps only live
stories, strips the markup from an Ask HN body, truncates to the schema's 10,000
characters without splitting a surrogate pair, and skips anything already stored.
Details and its own tests: `tools/MiniHn.Crawler/README.md`.

---

## Licence

MIT - see `LICENSE`.
