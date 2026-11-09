# M04 - Update and delete

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.4.0` |
| Coverage gate | line >= 88% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m04-update-delete`. Tag `v0.4.0`. Gate: line >= 88%.

**4.1** `feat(api): add PUT /posts/{id} replacing the content`
- Changes: full replace, same validator as create, 200 with new body, 404 when missing.
- Test: `Put_updates_content_and_returns_200`, `Put_missing_returns_404`, `Put_invalid_returns_422`.
- Accept: round-trip with curl.

**4.2** `feat(data): add optimistic concurrency token to posts`
- Changes: a `Version` (uint) shadow token while the store is SQLite, replaced by `xmin` at M10 (A.1.2's switch table), configured with `IsConcurrencyToken()`, incremented on update; migration `AddPostVersion`; `If-Match` is optional (A.6.3): a stale value gives `412 Precondition Failed`, a missing header retries once and then gives `409`.
- Test: `Put_with_stale_if_match_returns_412`, `Two_concurrent_puts_one_wins_one_gets_412`, `Put_without_if_match_retries_once_then_409`.
- Accept: two parallel `curl` PUTs, exactly one 200.

**4.3** `feat(api): add DELETE /posts/{id}`
- Changes: `204 No Content`; missing > 404; delete is idempotent in effect.
- Test: `Delete_returns_204_then_get_returns_404`, `Delete_missing_returns_404`.
- Accept: curl.

**4.4** `feat(data): soft delete posts with a global query filter`
- Changes: `DeletedAt` nullable column + migration, `HasQueryFilter(p => p.DeletedAt == null)`, `DELETE` sets the timestamp; `IgnoreQueryFilters` only in a future admin path. Body explains why (votes/comments will reference posts; hard delete would cascade).
- Test: `Deleted_post_is_hidden_from_list_and_get_but_row_remains`.
- Accept: `sqlite3 data/minihn.db "select id, deleted_at from posts"` shows the row.

**4.5** `test(api): cover create-update-delete lifecycle end to end`
- Changes: tests only. Test: `Post_lifecycle_create_get_update_delete_is_consistent`, `Update_after_delete_returns_404`. Accept: coverage line in PR.

---

---

Previous: [M03](m03-get-post-by-id.md) | Next: [M05](m05-pagination.md) | [All milestones](README.md) | [Repository README](../README.md)
