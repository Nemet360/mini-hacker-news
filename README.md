# Mini Hacker News

A small posts API on .NET 8: posts are stored persistently, and `GET /posts`
returns every stored post as JSON.

## Prerequisites

* .NET SDK **8.0.404** (pinned in `global.json`; `rollForward: latestFeature`)
* nothing else - the store is a local SQLite file, created on first run

## Run

```bash
dotnet run --project src/MiniHn.Api
```

The host listens on <http://localhost:5080>, creates `src/MiniHn.Api/data/minihn.db`
if it is missing and applies pending migrations before serving.

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

## Test

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
dotnet tool run reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage/report -reporttypes:TextSummary
```

Every test runs against a unique temporary SQLite **file**, the same provider the
application ships. `Posts_survive_a_host_restart` opens a second host over the
same file, which is the only way a persistence requirement can be proven.

## Design choices

**SQLite.** The requirement is a persistent store. SQLite is a file, so a clone
runs with no service to install and no container to start, and EF Core hides the
provider behind the same API PostgreSQL uses later: the connection string is
configuration, so swapping the provider touches no code.

**A real migration from the first table, never `EnsureCreated`.** The schema is
reviewable in a diff and replayable against a database that already has rows. CI
fails the build when the model and the snapshot drift apart.

**`long` identity.** SQLite stores `INTEGER` either way, and widening the key
after rows exist is a table rewrite.

**A response record, not the entity.** The wire contract and the table are free
to change independently.

**Nothing beyond the two requirements.** There is no `POST`, no validation layer,
no Swagger, no health endpoint, no authentication and no seed data in this tree.
The `CHECK (length(content) BETWEEN 1 AND 10000)` on the table is a *schema*
bound, not application validation: there is no write path here for a validator to
sit on, and a column with no bound accepts a 2 GB row.

## Repository conventions

See `CONTRIBUTING.md`: one step, one branch, one commit, one PR; Conventional
Commits enforced by `.githooks/commit-msg` and re-checked in CI; `dotnet format`
and the tests gate every push.
