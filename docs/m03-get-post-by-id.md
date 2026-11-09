# M03 - `GET /posts/{id}`

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.3.0` |
| Coverage gate | line >= 88% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m03-get-post`. Tag `v0.3.0`. Gate: line >= 88%.

**3.1** `feat(api): add GET /posts/{id}`
- Changes: route `{id:long}`, `FindAsync`-style single-row query, 200 with `PostResponse`.
- Test: `Get_existing_post_returns_200_and_body`. Accept: the `Location` header from 2.3 now resolves.

**3.2** `feat(api): return 404 problem details for missing posts`
- Changes: `404 application/problem+json` with `type`, `title`, `detail` ("Post 42 does not exist"); non-numeric id > 404 via route constraint (documented, not 400, to avoid leaking routing detail).
- Test: `Get_missing_post_returns_404_problem`, `Get_non_numeric_id_returns_404`, `Get_negative_id_returns_404`.
- Accept: curl `/posts/9999`.

**3.3** `feat(api): add ETag and conditional GET for a single post`
- Changes: weak ETag from `Id` + a truncated `SHA256` of `content`. The row has no timestamp at M3 — `UpdatedAt` arrives with the update path in M4 and `CreatedAt` only at M5 step 5.0 — and an ETag cannot be derived from a column that does not exist. Step 4.3 switches the source to `UpdatedAt` ticks, which is A.4.7's formula and A.6.3's `W/"4211-638…"` example, in the same commit that creates that column. `If-None-Match` > `304 Not Modified`.
- Test: `Second_get_with_if_none_match_returns_304`, `Etag_changes_after_content_changes` (uses direct DB mutation until M4).
- Accept: `curl -i -H 'If-None-Match: <etag>' ...` > 304.

---

---

Previous: [M02](m02-create-post-with-validation-and-openapi.md) | Next: [M04](m04-update-and-delete.md) | [All milestones](README.md) | [Repository README](../README.md)
