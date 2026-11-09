# M05 - Pagination

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.5.0` |
| Coverage gate | line >= 88% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m05-pagination`. Tag `v0.5.0`. Gate: line >= 90%. Design: keyset (cursor) pagination exactly as A.6.2 specifies it — the response body stays a bare JSON array so the brief's example shape is not broken, and paging metadata goes in the `Link` header (RFC 8288). The cursor is opaque and carries the sort it was issued for, so it cannot be replayed against a different ordering. Building anything else here would fail A.6.7's OpenAPI snapshot test.

**5.0** `feat(data): add CreatedAt to posts with backfill`
- Changes: `CreatedAt` (UTC, `DateTimeOffset`) set from an injected `TimeProvider`; migration backfills existing rows to the migration time; ADR 0003 states why it was not in M1 (Nanit scope). M10 ran before this milestone, so the column is `timestamptz` and the migration is a PostgreSQL one. The keyset cursor orders on this column, so it has to land before step 5.2.
- Test: `Created_post_has_created_at_from_time_provider` using `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`). Accept: `GET /posts/1` shows `createdAt`.

**5.1** `feat(api): add limit parameter to GET /posts`
- Changes: `?limit=` default 30 (the real HN front page size), max 100; `limit=0`, `limit>100` or a non-numeric `limit` → 400 problem details.
- Test: `Limit_defaults_to_30`, `Limit_above_max_returns_400`, `Limit_non_numeric_returns_400`, `Limit_5_returns_five`.
- Accept: seed 120 rows, curl `?limit=100` returns 100.

**5.2** `feat(data): add keyset cursor over (created_at, id)`
- Changes: opaque `?cursor=` — `base64url(json{ sort, c, i })` per A.6.2, where `sort` is fixed to `new` until M6 step 6.4 introduces the `?sort=` parameter, `c` is the last row's `created_at` and `i` its `id`; query `WHERE (created_at, id) < (@c, @i) ORDER BY created_at DESC, id DESC LIMIT @n+1`, newest-first, fetching one extra row to detect another page without a `COUNT`. In P1 the table has no `created_at`; step 5.0 (`feat(data): add CreatedAt to posts with backfill`, moved here from M6) adds it with its migration and backfill as the first step of this milestone.
- Test: `Cursor_returns_the_next_page_without_overlap`, `Tampered_cursor_returns_400`, `Cursor_issued_for_another_sort_returns_400_invalid_cursor`, `Rows_sharing_a_timestamp_are_ordered_and_paged_by_id`.
- Accept: iterate pages with a shell loop, count equals seeded rows and no id appears twice.

**5.3** `feat(api): expose next page through the Link header`
- Changes: `Link: </posts?sort=new&limit=30&cursor=...>; rel="next"` when more rows exist (A.6.2's exact shape). `sort=new` is emitted from the start even though `?sort=` is not yet *accepted* as input — M6 step 6.4 adds that — so the header shape never changes and the OpenAPI snapshot moves once rather than twice. None on the last page.
- Test: `Link_header_present_until_last_page`, `Link_header_preserves_other_query_parameters`.
- Accept: `curl -i` shows the header.

**5.4** `perf(migrations): add index supporting the cursor query`
- Changes: composite index `ix_posts_created_at_id` on `(created_at DESC, id DESC)` — the index A.5.4 lists for exactly this query. `EXPLAIN` asserts an index scan with no sort step and no temp b-tree.
- Test: `Cursor_query_plan_uses_the_composite_index_and_does_not_sort`. Accept: plan printed in the PR body.

**5.5** `test(data): property-test pagination under concurrent inserts`
- Changes: tests only (`FsCheck.Xunit`): any interleaving of inserts and page walks never returns a duplicate id and never skips an id that existed before the walk started.
- Test: that property, 200 runs. Accept: green, runtime < 20 s.

---

---

Previous: [M04](m04-update-and-delete.md) | Next: [M06](m06-sorting-and-ranking.md) | [All milestones](README.md) | [Repository README](../README.md)
