# Milestones

One file per milestone, M00 to M18. M00, M01 and M10 are implemented, and
their files also record where each step is in the history and where reality
departed from the plan. M10 was pulled forward to come straight after M01. The
rest are specifications, written before the pair-programming session and not
built.

| # | Milestone | Status | Tag | Coverage gate |
|---|---|---|---|---|
| [M00](m00-repository-and-tooling.md) | Repository and tooling | done | - | CI green on a host-boots test |
| [M01](m01-persistent-posts-and-get-posts.md) | Persistent posts and `GET /posts` | done | `v0.1.0` | line >= 85%, branch >= 75% (measured: 100% / 100%) |
| [M02](m02-create-post-with-validation-and-openapi.md) | `POST /posts` with validation and OpenAPI | planned | `v0.2.0` | line >= 88% |
| [M03](m03-get-post-by-id.md) | `GET /posts/{id}` | planned | `v0.3.0` | line >= 88% |
| [M04](m04-update-and-delete.md) | Update and delete | planned | `v0.4.0` | line >= 88% |
| [M05](m05-pagination.md) | Pagination | planned | `v0.5.0` | line >= 88% |
| [M06](m06-sorting-and-ranking.md) | Sorting and Hacker News ranking | planned | `v0.6.0` | line >= 88% |
| [M07](m07-authentication-and-authorisation.md) | Authentication and authorisation | planned | `v0.7.0` | line >= 90% |
| [M08](m08-voting.md) | Voting | planned | `v0.8.0` | line >= 90% |
| [M09](m09-comments.md) | Comments | planned | `v0.9.0` | line >= 90% |
| [M10](m10-postgresql.md) | PostgreSQL (pulled forward, after M01) | done | `v0.10.0` | line >= 90%, branch >= 75% (measured: 100% / 100%) |
| [M11](m11-caching.md) | Caching | planned | `v0.11.0` | line >= 90% |
| [M12](m12-rate-limiting.md) | Rate limiting | planned | `v0.12.0` | line >= 90% |
| [M13](m13-observability.md) | Observability | planned | `v0.13.0` | line >= 90% |
| [M14](m14-containerisation.md) | Containerisation | planned | `v0.14.0` | line >= 90% |
| [M15](m15-cicd-release-and-deploy.md) | CI/CD, release and deploy | planned | `v0.15.0` | line >= 90% |
| [M16](m16-load-testing.md) | Load testing | planned | `v0.16.0` | line >= 90% |
| [M17](m17-maintenance-mode.md) | Maintenance mode | planned | `v0.17.0` | line >= 90% |
| [M18](m18-hardening-and-1.0.md) | Hardening and 1.0 | planned | `v1.0.0` | line >= 90% |

## Reading order

M00 and M01 are what Nanit asked for and all that is built. The interview
boundary sits between M01 and M02: nothing below it has a branch, an issue or
a line of code, deliberately.

## What is not here

The repository's own scope limits - what the project does **not** cover, and
the assumptions the work rests on - are in the root
[README](../README.md#what-this-project-does-not-cover).
