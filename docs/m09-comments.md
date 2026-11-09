# M09 - Comments

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.9.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m09-comments`. Tag `v0.9.0`. Gate: line >= 90%.

**9.1** `feat(data): add Comment entity with parent reference`
- Changes: `Comment(Id, PostId, ParentId?, AuthorId, Content, CreatedAt, DeletedAt)`, indexes `(PostId, Id) WHERE DeletedAt IS NULL` and `(ParentId) WHERE ParentId IS NOT NULL` (A.5.4), migration.
- Test: `Comment_with_parent_in_other_post_is_rejected` (service-level invariant). Accept: migration clean.

**9.2** `feat(comments): add POST /posts/{id}/comments`
- Changes: authenticated, same content rules (1-10,000 code points), optional `parentId`, `201`.
- Test: `Create_top_level_comment`, `Create_reply`, `Reply_to_missing_parent_returns_404`.
- Accept: curl.

**9.3** `feat(comments): add GET /posts/{id}/comments returning a flat set in creation order`
- Changes: one query for the flat set, tree assembled in memory, depth capped at 20 (A.5.3's `ck_comments_depth`; a deeper reply is `422`), pagination by top-level comment.
- Test: `Tree_is_nested_in_chronological_order`, `Depth_beyond_20_returns_422`, `Single_query_is_issued`.
- Accept: curl output reviewed for a three-level thread.

**9.4** `feat(comments): allow author to edit and soft-delete own comment`
- Changes: `PUT` and `DELETE /comments/{id}` with the M7 ownership policy; deleted comments keep position and show `[deleted]`.
- Test: `Author_can_edit`, `Other_user_gets_403`, `Deleted_comment_keeps_children`.
- Accept: curl.

**9.5** `feat(data): maintain denormalised comment count on posts`
- Changes: `CommentCount` updated atomically with insert/soft-delete (`ExecuteUpdateAsync`), shown in list responses.
- Test: `Count_tracks_creates_and_deletes_under_concurrency`.
- Accept: counts match `select count(*)` in a consistency check script.

---

---

Previous: [M08](m08-voting.md) | Next: [M10](m10-postgresql.md) | [All milestones](README.md) | [Repository README](../README.md)
