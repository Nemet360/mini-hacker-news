# Mini Hacker News

[![build](https://github.com/Nemet360/mini-hacker-news/actions/workflows/build.yml/badge.svg)](https://github.com/Nemet360/mini-hacker-news/actions/workflows/build.yml)

A small posts API on .NET 8. Posts are stored persistently in PostgreSQL, and
`GET /posts` returns every stored post as JSON.

That is the whole of the API's scope, on purpose. The tree holds **no** `POST`,
no validation layer, no health endpoint, no authentication and no
pagination: those are milestones M2 onwards in the
[roadmap](#roadmap), specified under [`docs/`](docs/README.md) and written down
rather than built. The full list of absences is
[what this project does not cover](#what-this-project-does-not-cover), and the
choices behind them are the [working assumptions](#working-assumptions). What is
here is built to the standard the rest will be held to - one commit per step, a
test with every behaviour, CI on every push.

---

## Contents

* [Quick start](#quick-start)
* [Build it yourself](#build-it-yourself)
* [Architecture](#architecture)
* [The data model](#the-data-model)
* [Design decisions](#design-decisions)
* [Tests and coverage](#tests-and-coverage)
* [Continuous integration](#continuous-integration)
* [What this project does not cover](#what-this-project-does-not-cover)
* [Working assumptions](#working-assumptions)
* [Roadmap](#roadmap)
* [Per-milestone documents](#per-milestone-documents)
* [Repository conventions](#repository-conventions)
* [Filling the store with real data](#filling-the-store-with-real-data)

---

## Quick start

```bash
git clone https://github.com/Nemet360/mini-hacker-news.git
cd mini-hacker-news
cp .env.example .env                 # then set POSTGRES_PASSWORD in .env
docker compose up -d --wait          # PostgreSQL 17 on 127.0.0.1:5432
dotnet user-secrets set ConnectionStrings:Posts \
  "Host=localhost;Port=5432;Database=minihn;Username=minihn;Password=<the .env password>" \
  --project src/MiniHn.Api
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

The host creates the database if the server lacks it and applies pending
migrations before it serves, so there is no schema step to run by hand. Without
`ConnectionStrings:Posts` it refuses to start, with one line that names the
setting. Any PostgreSQL 17 server works; compose is only the quickest way to get
one. For 100 real stories in the feed, see
[filling the store](#filling-the-store-with-real-data).

---

## Build it yourself

### Prerequisites

| Need | Version | Notes |
|---|---|---|
| .NET SDK | **8.0.404** | pinned in `global.json`, `rollForward: latestFeature` |
| git | any | the commit hooks are plain `bash` |
| Docker | any recent | runs PostgreSQL via `docker-compose.yml`, and the throwaway server the tests start |
| Node | 20 (optional) | only for running `commitlint` locally |

The store is PostgreSQL 17. The tests need no server set up: with
`TEST_PG_CONNECTION` unset they start a `postgres:17-alpine` container through
Testcontainers, and with it set (CI does) they use that server. Each test gets a
database of its own on it.

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

Outside Production the API serves Swagger UI at
<http://localhost:5080/swagger>. In VS Code, F5 builds, starts the API under the
debugger on port 5085 and opens its Swagger page (`.vscode/launch.json`).

### If port 5080 is taken

The development port is 5080 (`src/MiniHn.Api/Properties/launchSettings.json`).
To run elsewhere, bypass the launch profile:

```bash
ASPNETCORE_URLS=http://localhost:5085 dotnet run --no-launch-profile --project src/MiniHn.Api
```

### Database commands

```bash
docker compose up -d --wait                                # PostgreSQL 17, data in a named volume
docker compose exec postgres psql -U minihn -d minihn -c '\d posts'
dotnet ef migrations add <Name> --project src/MiniHn.Api -o Persistence/Migrations
dotnet ef migrations has-pending-model-changes --project src/MiniHn.Api
```

`dotnet ef` builds the model through `DesignTimeDbContextFactory`, so adding a
migration needs no running database and no secret.

### The whole gate, locally

```bash
dotnet format --verify-no-changes                         # style is a build error, not a review comment
bash scripts/test-hooks.sh                                # 15 commit-message vectors
bash scripts/readme-smoke.sh                              # runs this README's own commands on a throwaway database
KEEP_DATA=1 bash scripts/readme-smoke.sh                  # ... against the configured database as it is
bash scripts/compose-smoke.sh                             # docker-compose.yml comes up healthy (isolated project)
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings \
  --results-directory coverage
bash scripts/coverage.sh                                  # the two commands above, plus the report
python3 scripts/coverage_gate.py coverage none 0 --min-line 90 --min-branch 75   # the CI floors
```

`readme-smoke.sh` needs `ConnectionStrings__Posts` in the environment.

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
                        │ EF Core 8, Npgsql provider
                        ▼
        ┌───────────────────────────────┐
        │  Domain                       │   MiniHn.Domain
        │  Post { long Id, Content }    │   no EF attributes, no HTTP types
        └───────────────────────────────┘
                        │
                        ▼
              PostgreSQL 17  (docker-compose.yml locally)
```

```
src/MiniHn.Api/
  Program.cs                                  host: config → DbContext → migrate → MapPosts
  Domain/Post.cs                              the entity, persistence-ignorant
  Persistence/AppDbContext.cs                 the only DbContext
  Persistence/DesignTimeDbContextFactory.cs   lets dotnet ef run without a database
  Persistence/Configurations/
      PostConfiguration.cs                    posts table, pk_posts, identity, char_length CHECK
  Persistence/Migrations/                      InitialCreate + model snapshot (generated)
  Features/Posts/PostsEndpoints.cs            MapGet("/posts")
  Features/Posts/PostResponse.cs              { id, content }
  appsettings.json                            no connection string: user-secrets or ConnectionStrings__Posts

tests/MiniHn.Api.Tests/
  ApiFactory.cs                               the real host over a caller-supplied database
  PostgresServer.cs                           the shared server: TEST_PG_CONNECTION or Testcontainers
  TempPostgresDatabase.cs                     a database of its own per test, dropped on dispose
  HostTests.cs                                an unknown route is 404
  ConfigurationTests.cs                       no connection string → the host refuses to start
  PostTests.cs                                the entity
  PostConfigurationTests.cs                   migration, column types, the CHECK constraint
  MigrationModelTests.cs                      migrations apply and match the model
  StartupMigrationTests.cs                    a missing database is created and migrated at start
  GetPostsTests.cs                            empty, populated, ordering, no-tracking, restart
  OpenApiTests.cs                             Swagger outside Production, none in Production
  DependencyGraphTests.cs                     no SQLite package in any lock file
  DesignTimeDbContextFactoryTests.cs          the dotnet ef factory
```

**The request path, start to finish.** `Program.cs` reads
`ConnectionStrings:Posts`, refuses to start without it, registers
`AppDbContext` on the Npgsql provider, applies pending migrations (creating the
database if the server lacks it), then maps the endpoint group. A `GET /posts` resolves a
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
    id      bigint GENERATED BY DEFAULT AS IDENTITY,
    content text   NOT NULL,
    CONSTRAINT pk_posts PRIMARY KEY (id),
    CONSTRAINT ck_posts_content_length CHECK (char_length(content) BETWEEN 1 AND 10000)
);
```

`id` is 64-bit from the first commit. `BY DEFAULT` rather than `ALWAYS`, so a
test or an import can supply its own ids. `char_length` counts characters, not
bytes, so a post in Hebrew gets as much room as one in English. The `CHECK` is a *schema* bound, not
application validation: there is no write path in this milestone for a validator
to sit on, and a column with no bound is a column that accepts a 2 GB row.

---

## Design decisions

**PostgreSQL, and only PostgreSQL.** M1 shipped on SQLite, so a clone ran with
nothing installed. M10 was pulled forward to come straight after M1: one
provider, used in development, CI and tests alike. There was never a
`Sqlite|Postgres` switch. EF migrations belong to one provider, so a switch means
two migration sets kept in step, and the M1 data was dev data, so nothing was
migrated across. `No_sqlite_package_remains_in_the_dependency_graph` keeps
SQLite from coming back through a transitive reference. Tag `v0.1.0` still
points at the SQLite tree that was submitted.

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

**`long` identity.** Widening an `int` key after rows exist is a table rewrite,
so the key was 64-bit from M1 on, and the PostgreSQL `bigint` identity needed no
change to the entity.

**A response record, not the entity.** `PostResponse` is the wire contract. The
table and the JSON are then free to change independently, and the projection
happens in SQL rather than in memory.

**Ordered by `id`.** An unordered `SELECT` is only accidentally stable. A
reviewer comparing two runs must not see a different array because the planner
chose another plan.

**`AsNoTracking()`.** Nothing on this path writes, so the change tracker is pure
cost - and `Get_posts_does_not_track_entities` pins it.

**The in-memory EF provider is banned**, not merely avoided:
`Directory.Build.props` fails the build if
`Microsoft.EntityFrameworkCore.InMemory` is ever referenced. The requirement
under test is *persistence*, and a provider a second host cannot reopen is unable
to express it. Tests run against PostgreSQL, the engine that ships, with a
**database** of their own per test. A whole database rather than a wiped schema
(Respawn), so identities start at 1 in every test.

---

## Tests and coverage

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings \
  --results-directory coverage
dotnet tool run reportgenerator \
  -reports:'coverage/**/coverage.cobertura.xml' \
  -targetdir:coverage/report -reporttypes:TextSummary
cat coverage/report/Summary.txt
```

Or in one step, which is also what CI runs so the numbers cannot drift from the
documentation:

```bash
bash scripts/coverage.sh
```

Current state of the M10 branch: **47 tests** (22 for the API, 25 for the
crawler) with **line 100% (50 of 50 lines), branch 100% (6 of 6) and method 100%
(10 of 10)** on the product assembly. M1 measured 100% / 100% (40 lines, 2
branches). CI shows these figures in each test job's summary and in a comment
on the pull request, and fails any change that goes under the floors: **line 90%,
branch 75%**. `--results-directory coverage` is not
optional: without it `dotnet test` writes each project's report under
`tests/<project>/TestResults/`, and the reportgenerator pattern above finds
nothing.

`coverlet.runsettings` excludes `Persistence/Migrations/**` and `tools/**`. The
second is the crawler - a development tool, not the product; its pure logic is
unit tested and still fails the build when it breaks, but it does not move the
figure the gate ratchets. The first is deliberate too: a generated `Down()` nobody calls would otherwise
set the coverage number, while the migration's actual effect *is* asserted -
`Migration_creates_posts_with_bigint_identity_text_content_and_the_length_check`
reads `information_schema` and `pg_constraint`, and
`Postgres_migration_applies_and_matches_model` diffs the snapshot against the
model.

What the suite buys, test by test:

| Test | What breaks it |
|---|---|
| `Host_boots_and_unknown_route_returns_404` | an unplanned route appearing |
| `Startup_without_postgres_fails_fast_with_a_named_message` | a host that boots storeless and 500s per request |
| `Post_requires_content_and_defaults_id_to_zero` | the caller assigning identity |
| `Context_round_trips_a_post_through_postgresql` | a broken mapping |
| `Migration_creates_posts_with_bigint_identity_text_content_and_the_length_check` | schema drift from the configuration |
| `Postgres_migration_applies_and_matches_model` | a model change with no migration |
| `Content_outside_the_bound_is_refused_by_the_store` (0 and 10,001) | the length `CHECK` being dropped |
| `Content_at_the_bound_counts_characters_not_bytes` | `char_length` swapped for `octet_length` |
| `Missing_database_is_created_and_migrated_on_host_start` | losing the no-setup-step promise |
| `Get_posts_on_empty_database_returns_200_and_empty_array` | `204`, `null`, or a wrapper object |
| `Get_posts_returns_every_stored_post_with_id_and_content` | a field rename or a casing change |
| `Posts_survive_a_host_restart` | storage that is not actually persistent |
| `Get_posts_does_not_track_entities` | a read path that tracks |
| `Development_serves_the_openapi_document_describing_get_posts` | Swagger lost in development |
| `Production_serves_no_openapi_document_and_no_swagger_ui` | the API surface published in production |
| `No_sqlite_package_remains_in_the_dependency_graph` | SQLite coming back transitively |

---

## Continuous integration

Four workflows, because they answer different questions. Every job that runs
tests gets a `postgres:17-alpine` service container and passes it in through
`TEST_PG_CONNECTION`.

**`build.yml` - the project's build.** On every push to every branch, on `v*`
tags and on demand: the pinned SDK, `dotnet restore --locked-mode`, a Release
build, the migration-drift check, the tests with coverage printed into the job
summary, this README's own commands via `scripts/readme-smoke.sh`, the coverage
floors, a `dotnet publish`, and the published output plus the coverage report and
badges uploaded as artifacts.

**`ci.yml` - the pull-request gate.** Five independent jobs, so a red light names
its own cause:

| Job | Fails when |
|---|---|
| `commitlint` | a commit on the branch is not a Conventional Commit, or a hook vector regresses |
| `build-test` | the build, the migration-drift check, a test, the README smoke or the compose smoke fails |
| `format` | `dotnet format --verify-no-changes` finds anything |
| `vulnerable-packages` | a vulnerable or deprecated package, or a gitleaks hit |
| `coverage-gate` | line under 90%, branch under 75%, or line more than 0.5pp below `main`. It also posts the figures as one pull-request comment, edited in place on each push |

**`main.yml` - the coverage baseline.** On every push to `main` it runs the tests
with coverage and uploads the `coverage-main` artifact. That artifact is what the
pull-request `coverage-gate` job compares against, which is why the baseline is
produced by its own workflow rather than inside the gate that reads it.

**`release.yml` - delivery.** A `v*` tag builds, tests against PostgreSQL,
must clear the same coverage floors, then publishes a GitHub Release with the
zipped API and the coverage table in its notes. The container image is M14 and
the deploy is M15.

`scripts/coverage_gate.py` reads ReportGenerator's merged report. Each test
project writes its own raw file, and the crawler tests also instrument the API
assembly, so no single raw file is the product figure. It deliberately has three
answers rather than two: a missing baseline, or nothing to measure, is *"we could
not look"* and passes, so the first pull request of a repository can go green.
A figure under a floor, or a measured drop, fails.

---

## What this project does not cover

Stated plainly, because an absence a reader has to discover looks like an
oversight - and in a take-home exercise half of these absences are the answer.

**Not built, by instruction.** Nanit asked for a persistent store and
`GET /posts`, and asked explicitly that nothing more be implemented beforehand.
So there is no `POST /posts`, no `GET /posts/{id}`, no update, no delete, no
request-validation layer, no pagination, no
sorting or ranking, no authentication, no authorisation, no voting, no comments,
no health or readiness endpoint, and no `Title` or `CreatedAt` on a
post. Each of those is a milestone from M02 on: specified under `docs/`, not
coded.

**Not built, by scope.** Caching and ETags (M11), rate limiting (M12), structured
logging, metrics and tracing (M13 - the host runs the ASP.NET Core defaults and
nothing more), an API container image (M14 - compose runs only the database),
any deployment beyond a GitHub Release (M15), load tests and
a published latency budget (M16), maintenance mode (M17), and the hardening pass
(M18).

**Deliberately absent even inside the code that does exist.**

* **No application-level validation of `content`.** The only bound is the table's
  `CHECK (char_length(content) BETWEEN 1 AND 10000)`. There is no write path for a
  validator to sit on yet, and a column with no bound accepts a 2 GB row.
* **No concurrency control, no transaction beyond EF's own**, and no uniqueness on
  `content`: this milestone has one reader and no writer.
* **No API versioning, no CORS policy, no response compression, no HTTPS
  redirection, no problem-details handler.** Each belongs to the milestone that
  first needs it; adding them now would be configuration nothing can test.
* **No multi-instance story.** Migrations run at host start, which is right for one
  process and wrong for a rolling deploy - M15 moves that into the release
  pipeline. Now that the store is a shared PostgreSQL server, that move matters.
* **No retry, backoff or authentication in the crawler** beyond a paced loop: it is
  a developer command, run by hand.
* **No performance claim.** Nothing here has been load tested, and `GET /posts`
  returns the whole table - which is exactly what M05's pagination exists to fix.
* **The GitHub-side half of M00 is not applied.** Branch protection, required
  checks, labels, the project board, Milestones and per-step issues need the
  repository owner's account; the local half (hooks, templates, CODEOWNERS,
  dependabot) is in the tree.

## Working assumptions

The exercise is small enough that its assumptions decide the design, so they are
written down rather than implied. Each is something I would ask about in a real
project and have instead chosen, visibly.

1. **"Persistent" means it survives a process restart, not a machine.** A
   PostgreSQL database on a named volume satisfies that, and
   `Posts_survive_a_host_restart` is what holds me to it.
   Replication and backups belong to a deployment story nobody has described.
2. **One reader, no writer, in this milestone.** So the response is the whole
   table, ordered by `id`, with no paging and no cache.
3. **`content` is opaque text** - not markdown, not HTML, not sanitised: nothing in
   this scope renders it. The crawler strips markup *at the source* instead of
   storing it.
4. **The brief's example body is the contract**:
   `[{"id":1,"content":"first post"}]` - camelCase, numeric `id`, a bare array
   rather than an envelope.
   `Get_posts_returns_every_stored_post_with_id_and_content` asserts it character
   for character, so a future envelope has to be a conscious break.
5. **Ids are server-assigned, opaque and monotonic** - not a client-chosen key and
   not a hash of the content.
6. **An empty store is `200` with `[]`**, never `204` and never `404`: an empty
   collection is a successful answer about a collection that is empty.
7. **.NET 8 is fixed by the brief.** Its LTS window closes on 10 November 2026, so
   the target framework sits in one line of `Directory.Build.props`; moving to
   net10 is that line plus package bumps.
8. **No paid dependency and no account.** Hence a local PostgreSQL container over a hosted database, the
   key-less Hacker News API, and FluentAssertions pinned to 6.x - from 8.0 it is
   licensed per developer for commercial use.
9. **The reader is a reviewer, not an operator.** The history, the per-step commits
   and `docs/` are part of the deliverable. A clone must build with nothing but the
   pinned SDK and Docker, and CI runs this README's own commands so that claim
   cannot rot.
10. **English throughout the repository**, whoever wrote it - code, commits,
    comments and documents in one language a future contributor can read.

## Roadmap

M0 and M1 are implemented (tag `v0.1.0`). M10 was pulled forward to come
straight after M1, onto the M1 schema. Its items that need later tables move to
the milestone that adds them. Everything else is specified and not built. "Gate" is the coverage floor that milestone's pull request must
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
| M10 | PostgreSQL (pulled forward, after M1) | **done** | `v0.10.0` | 90% / 75% - measured **100% / 100%** |
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
hashing; M11 ETags and a feed
`Cache-Control` with `s-maxage` and `stale-if-error`; M12 a limiter partitioned
on the token subject.

---

## Per-milestone documents

`docs/` holds one markdown file per milestone, M00 to M18: that milestone's goal,
its ordered steps with the exact commit subject for each, the test every step
adds, the acceptance check that proves it, and the coverage gate its pull request
must clear. The two implemented ones also record **where each step is in the
history** and **where reality departed from the plan** - an SDK installed per-user
because the machine-wide installer wanted elevation, two extra tooling commits, a
development port that was already taken.

Start at [docs/README.md](docs/README.md).

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
* **Branches.** `milestone/m00-repo-and-tooling`, `milestone/m01-list-posts` and
  `milestone/m10-postgres` each point at the last commit of that milestone, so the branch shows the
  milestone as it was delivered rather than as `main` looks today; all are merged
  into `main`. `tooling/hn-crawler` is the branch the development tool below was
  written on. `main` is the one to read. `v0.1.0` tags the SQLite submission and
  `v0.10.0` the PostgreSQL store.

---

## Filling the store with real data

`GET /posts` is more convincing on real rows than on hand-typed ones, so
`tools/MiniHn.Crawler` fills the store from the public Hacker News API.

**It is a development tool, not part of the assignment.** The two requirements are
persistent storage and `GET /posts`; the crawler is how the data behind a demo got
there, and it is in the tree so nobody has to take that data on trust. It is a
separate project, nothing under `src/` references it, and `coverlet.runsettings`
excludes it from the coverage figure the gate ratchets - its own unit tests still
run and still fail the build when they break. `tooling/hn-crawler` remains as the
branch it was developed on.

It writes to the same PostgreSQL store the API reads, through
`ConnectionStrings__Posts` or `--connection`.

```bash
export ConnectionStrings__Posts="Host=localhost;Port=5432;Database=minihn;Username=minihn;Password=<yours>"
bash scripts/seed.sh                                            # start over with 100 stories; fails unless 100 are stored
dotnet run --project tools/MiniHn.Crawler                       # add up to 100 more
dotnet run --project tools/MiniHn.Crawler -- --count 5 --dry-run
dotnet run --project src/MiniHn.Api
curl -s http://localhost:5080/posts
```

`--count` is how many *new* stories to add. A second run without `--clear` goes
further down the top-500 list and adds 100 more. `seed.sh` always clears first,
so it gives exactly 100.

It reads `v0/topstories.json` and `v0/item/<id>.json` from the official key-less
API - no scraping and no credentials - paces its requests, keeps only live
stories, strips the markup from an Ask HN body, truncates to the schema's 10,000
characters without splitting a surrogate pair, and skips anything already stored.
Details and its own tests: `tools/MiniHn.Crawler/README.md`.

---

## Licence

MIT - see `LICENSE`.
