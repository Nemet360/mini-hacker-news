# M01 - Persistent posts and `GET /posts`

| | |
|---|---|
| Status | **Implemented** (tag `v0.1.0`) |
| Tag | `v0.1.0` |
| Coverage gate | line >= 85%, branch >= 75% (measured: 100% / 100%) |
| Boundary | before the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Goal: exactly the two Nanit requirements and nothing else. A persistent store holding posts with `id` and `content`; `GET /posts` returns `200` and a JSON array (empty at this stage). Branch `milestone/m01-list-posts`. Tag `v0.1.0` (= the submission). Gate: line >= 85%, branch >= 75% (a baseline for the ratchet). **Do not add**: POST, request validation, Swagger, auth, paging, health endpoints, Docker, seed data, `CreatedAt`, `Title`. The `CHECK (length(content) BETWEEN 1 AND 10000)` that ships with the table is a *schema* constraint, not application validation — there is no write path in P1 for a validator to sit on, and a column with no bound is a column that accepts a 2 GB row.

**1.1** `feat(domain): add Post entity`
- Changes: `Post { long Id; required string Content }` in `src/MiniHn.Api/Domain/Post.cs` (`long` from day one — A.5.2: widening the key later is a table rewrite).
- Test: `Post_requires_content_and_defaults_id_to_zero`. Accept: `dotnet test` green.

**1.2** `feat(data): add AppDbContext with SQLite provider`
- Changes: packages `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design` (versions in `Directory.Packages.props`), `AppDbContext` with `DbSet<Post> Posts`, `Id` as key with auto-increment, `Content` required (`IsRequired`), connection string `Data Source=data/minihn.db` from configuration (`data/` is git-ignored, per A.4.10).
- Test: `Context_round_trips_a_post_through_a_temp_sqlite_file` against a unique `Data Source=<temp>/minihn-<guid>.db`, deleted on dispose (D.2.3).
- Accept: test green; `data/minihn.db` is git-ignored.

**1.3** `feat(migrations): add initial migration creating the Posts table`
- Changes: `dotnet ef migrations add InitialCreate -o Persistence/Migrations` (the path A.2.3's tree uses); the migration and model snapshot are committed.
- Test: `Migration_applies_to_an_empty_database_and_creates_posts_table` (apply to a temp file DB, query `sqlite_master`). CI adds `dotnet ef migrations has-pending-model-changes` as a step so snapshot drift fails the build.
- Accept: `dotnet ef database update` on a clean clone creates `data/minihn.db` with the table A.5.1 specifies: `posts(id INTEGER PK AUTOINCREMENT, content TEXT NOT NULL, CONSTRAINT ck_posts_content_length CHECK (length(content) BETWEEN 1 AND 10000))`.

**1.4** `feat(data): apply pending migrations at startup`
- Changes: in `Program.cs`, scope → `db.Database.Migrate()` before `app.Run()`. Rationale in body: persistent storage must exist on a fresh clone without a manual step.
- Test: `Fresh_database_file_is_created_and_migrated_on_host_start` (factory with a temp file, assert the file and table exist).
- Accept: delete `data/minihn.db`, `dotnet run`, the file reappears.

**1.5** `feat(api): add GET /posts returning all posts as JSON`
- Changes: `MapGet("/posts", ...)` returns `Results.Ok(posts)` projected to `PostResponse(long Id, string Content)` with `AsNoTracking()`, ordered by `Id` for a deterministic body; camelCase JSON; `application/json`.
- Test: `Get_posts_on_empty_database_returns_200_and_empty_array` (asserts status, content-type, body `[]`).
- Accept: `curl -i localhost:5080/posts` > `HTTP/1.1 200 OK`, `[]`.

**1.6** `test(api): cover populated list and persistence across host restarts`
- Changes: tests only (+ a small `TestHost` helper).
- Tests: `Get_posts_returns_every_stored_post_with_id_and_content` (seed two rows, body equals `[{"id":1,"content":"first post"},{"id":2,"content":"second post"}]` exactly as in the brief), `Posts_survive_a_host_restart` (seed through one factory, dispose, new factory on the same file, GET returns them), `Get_posts_does_not_track_entities`.
- Accept: coverage printed in the PR; line >= 85%.

**1.7** `docs: document how to run, test and measure coverage`
- Changes: `README.md` (prerequisites, `dotnet run`, `curl` example, `dotnet test`, coverage command, design choice paragraph: why SQLite, why int identity). Contains no mention of unbuilt features.
- Test: a CI step runs the README's `dotnet`/`curl` commands in a script (`scripts/readme-smoke.sh`), so docs cannot rot. Accept: a clean clone follows the README and gets `[]`.

Milestone PR M1: demo script is the README; merge with a merge commit; tag `v0.1.0`; this tree is what Nanit sees. Before pushing the tag run the pre-submission checklist: `git log --oneline` reads cleanly, `git ls-files` shows nothing outside the P1 tree in A.2.3 (C.1.2 is the end-state tree and would wave through files that announce unbuilt work), no `TODO`s, `git grep -iE "password|secret|token"` returns nothing.

---

## Where this is in the repository

Branch `milestone/m01-list-posts`, merged into `main`, tag `v0.1.0`. One commit
per step:

| Step | Commit subject |
|---|---|
| 1.1 | `feat(domain): add post entity` |
| 1.2 | `feat(data): add appdbcontext with sqlite provider` |
| 1.3 | `feat(migrations): add initial migration creating the posts table` |
| 1.4 | `feat(data): apply pending migrations at startup` |
| 1.5 | `feat(api): add get posts returning all posts as json` |
| 1.6 | `test(api): cover populated list and persistence across host restarts` |
| 1.7 | `docs: document how to run, test and measure coverage` |

Measured, not estimated: **11 tests, line coverage 100%, branch coverage 100%**
(`Persistence/Migrations/**` excluded - generated code - with the migration's
effect asserted through `sqlite_master` instead).

**Deviations from the plan as written:**

* One extra step, `test(api): measure coverage without the generated migrations`,
  which also turned the missing-connection-string case from an untested `throw`
  into `Host_refuses_to_start_without_a_connection_string`.
* The development port stays 5080 in `launchSettings.json`, but the smoke script
  takes `BASE_URL` because 5080 was occupied on the machine this was built on.


---

Previous: [M00](m00-repository-and-tooling.md) | Next: [M02](m02-create-post-with-validation-and-openapi.md) | [All milestones](README.md) | [Repository README](../README.md)
