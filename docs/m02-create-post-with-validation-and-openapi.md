# M02 - `POST /posts` with validation and OpenAPI

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.2.0` |
| Coverage gate | line >= 88% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m02-create-post`. Tag `v0.2.0`. Gate: line >= 88%.

**2.1** `feat(openapi): generate OpenAPI document and Swagger UI with Swashbuckle`
- Changes: `Swashbuckle.AspNetCore`, `AddEndpointsApiExplorer`, `AddSwaggerGen`, UI at `/swagger` in non-production; `.WithName/.Produces<>` metadata on the existing endpoint; first `openapi.v1.verified.json` snapshot, asserted with **Verify** by `OpenApiSnapshotTests` (A.6.7).
- Test: `OpenApi_document_matches_committed_snapshot` (the CI `openapi-snapshot` job becomes live).
- Accept: `/openapi/v1.json` lists `GET /posts` (A.6.5 configures that document path).

**2.2** `feat(api): validate create-post requests with FluentValidation`
- Changes: `FluentValidation` package, `CreatePostRequest(string Content)`, rules: required after trim, 1-10,000 code points (`PostRules.MaxContentLength`), no control characters except `\n`. Endpoint filter `ValidationFilter<T>`.
- Test: `Validator_rejects_{null,empty,whitespace,10001_code_points,control_char}` and `Validator_accepts_10000_code_points_and_multiline` (theory data).
- Accept: unit tests green (validator is pure).

**2.3** `feat(api): add POST /posts returning 201 with Location`
- Changes: `MapPost("/posts")` stores trimmed content, returns `201 Created`, `Location: /posts/{id}`, body `PostResponse`; the `Location` target arrives in M3 (documented in body).
- Test: `Post_creates_row_and_returns_201_with_location_and_body`, `Created_post_appears_in_get_posts`.
- Accept: `curl -i -X POST localhost:5080/posts -H 'Content-Type: application/json' -d '{"content":"hello"}'` > `201`, then `GET /posts` shows it.

**2.4** `feat(api): return RFC 9457 problem details for invalid requests`
- Changes: `AddProblemDetails`, validation failures mapped to `422 application/problem+json` with an `errors` dictionary (A.4.13); malformed JSON → 400; wrong `Content-Type` → 415; `Content-Length` over 64 KiB → 413 (Kestrel `MaxRequestBodySize`, B.4.5).
- Test: `Empty_content_returns_422_problem_with_errors_content`, `Malformed_json_returns_400`, `Text_plain_returns_415`, `Oversized_body_returns_413`.
- Accept: curl each case, compare against the snapshot'd schema.

**2.5** `docs(openapi): document request, response and error schemas`
- Changes: XML docs, `ProducesProblem(400/413/415)`, examples (`SwaggerRequestExample` via `Swashbuckle.AspNetCore.Filters`); snapshot regenerated in the same commit.
- Test: `OpenApi_declares_all_documented_status_codes_for_post_posts`. Accept: Swagger UI "Try it out" works.

---

---

Previous: [M01](m01-persistent-posts-and-get-posts.md) | Next: [M03](m03-get-post-by-id.md) | [All milestones](README.md) | [Repository README](../README.md)
