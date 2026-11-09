# M06 - Sorting and Hacker News ranking

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.6.0` |
| Coverage gate | line >= 88% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m06-ranking`. Tag `v0.6.0`. Gate: line >= 90%, ranking class 100%. Formula: `score = (points - 1) / (ageHours + 2)^1.8` (the published HN gravity of 1.8).

**6.1** `feat(domain): add pure HackerNews ranking function`
- Changes: `HotRank.Score(int points, TimeSpan age, double gravity = 1.8)` in `Domain/Ranking/HotRank.cs` (A.4.2); no I/O.
- Test: table-driven: 1 point is 0, higher points rank higher at equal age, older ranks lower at equal points, gravity 0 ignores age, age 0 does not divide by zero; reference values computed by hand in the test.
- Accept: green; mutation (`1.8` > `1.0`) fails a test.

**6.2** `feat(data): add Points column defaulting to 1`
- Changes: `Points` int default 1 (author's implicit vote, as on HN); migration. Votes arrive in M8.
- Test: `New_post_has_one_point`. Accept: `select Points from posts` shows 1 for every row; `points` reaches `PostResponse` with the vote endpoints in M8 step 8.5.

**6.3** `feat(data): materialise RankScore on a background refresh`
- Changes: `RankScore` column; `RankRefresherService : BackgroundService` recomputes every 60 s in batches using `ExecuteUpdateAsync`; recomputes immediately for a post whose points change (hook for M8).
- Test: `Refresh_updates_scores_using_time_provider`, `Refresh_is_idempotent`, `Refresh_does_not_run_concurrently_with_itself`.
- Accept: advance `FakeTimeProvider` an hour, order changes as expected.

**6.4** `feat(api): add sort parameter to GET /posts`
- Changes: `?sort=new|top|hot`; `new` = `CreatedAt DESC, Id DESC`, `top` = `Points DESC, Id DESC`, `hot` = `RankScore DESC, Id DESC`; keyset cursor encodes the sort key + id; composite indexes per sort in a `perf(migrations)` file within the same commit.
- Test: `Sort_new_orders_by_created_at_desc`, `Sort_top_orders_by_points`, `Sort_hot_orders_by_score`, `Cursor_works_for_every_sort`, `Unknown_sort_returns_400`.
- Accept: curl each sort with seeded data.

---

---

Previous: [M05](m05-pagination.md) | Next: [M07](m07-authentication-and-authorisation.md) | [All milestones](README.md) | [Repository README](../README.md)
