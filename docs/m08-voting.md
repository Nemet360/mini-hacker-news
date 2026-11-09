# M08 - Voting

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.8.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m08-voting`. Tag `v0.8.0`. Gate: line >= 90%.

**8.1** `feat(data): add Vote entity with one vote per user per post`
- Changes: `Vote(UserId, PostId, CreatedAt)` composite PK, FKs, migration.
- Test: `Second_vote_by_same_user_violates_primary_key`. Accept: migration clean.

**8.2** `feat(votes): add PUT /posts/{id}/vote`
- Changes: authenticated; first vote `200 {"postId","points","voted":true}`, repeat vote returns the same body unchanged (idempotent), missing post `404`.
- Test: `Vote_increments_points_once`, `Repeat_vote_is_idempotent`, `Vote_on_missing_post_returns_404`.
- Accept: curl twice, points +1 only.

**8.3** `feat(votes): forbid self-votes and add DELETE /posts/{id}/vote`
- Changes: author cannot vote their own post (`403`); unvote decrements; unvote returns `200` with `"voted":false`.
- Test: `Author_cannot_vote_own_post`, `Unvote_decrements_once`.
- Accept: curl.

**8.4** `fix(votes): make point counting atomic under concurrency`
- Changes: `ExecuteUpdateAsync(p => p.SetProperty(x => x.Points, x => x.Points + 1))` in the same transaction as the vote insert; triggers `RankScore` recompute (the 6.3 hook).
- Test: `Fifty_parallel_votes_from_fifty_users_yield_51_points` written **first** against the naive version to show it red (commit message body records the red result: 37), then green.
- Accept: test green 20 times in a row (`dotnet test --filter ... -- RunConfiguration.MaxCpuCount=0` loop).

**8.5** `feat(votes): expose points and the caller's vote state in responses`
- Changes: `PostResponse` gains `points`; when authenticated `viewerHasVoted`; computed with one LEFT JOIN, no N+1.
- Test: `List_returns_viewer_vote_state_for_each_post` plus a query-count assertion (`EF` command interceptor counts == 1).
- Accept: log shows one SQL statement per list call.

---

---

Previous: [M07](m07-authentication-and-authorisation.md) | Next: [M09](m09-comments.md) | [All milestones](README.md) | [Repository README](../README.md)
