# M18 - Hardening and 1.0

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v1.0.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m18-hardening`. Tag `v1.0.0`. Gate: line >= 90%, branch >= 80%. Threat detail belongs to the security section; this milestone implements its checklist.

**18.1** `feat(security): add security headers`
- Changes: middleware adding `Strict-Transport-Security: max-age=31536000; includeSubDomains`, `X-Content-Type-Options: nosniff`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`, `Referrer-Policy: no-referrer`, `Cache-Control: no-store` on auth routes; remove `Server`/`X-Powered-By`.
- Test: `Responses_carry_security_headers`, `Server_header_is_absent`. Accept: `curl -I` or securityheaders-style check.

**18.2** `feat(security): restrict CORS to an allow-list`
- Changes: origins from configuration; no wildcard with credentials.
- Test: `Unknown_origin_gets_no_cors_headers`, `Allowed_origin_preflight_succeeds`. Accept: curl with `Origin`.

**18.3** `feat(security): bound request time, size and header limits`
- Changes: Kestrel `MaxRequestHeadersTotalSize`, `RequestHeadersTimeout = 10s`, `MinRequestBodyDataRate`, `AddRequestTimeouts` default 4 s for reads and 6 s for writes (B.5.1), `MaxConcurrentConnections`.
- Test: `Slow_body_is_cut_off`, `Oversized_headers_return_431`. Accept: a slowloris script is dropped by the server.

**18.4** `feat(security): lock accounts after repeated failed logins`
- Changes: 5 failures in 15 min locks the username for 15 min (exponential after repeats), generic `429`/`401` bodies, audit events `auth.login.failure`, `auth.lockout` (the names declared in D.1.14).
- Test: `Fifth_failed_attempt_locks_account`, `Lock_expires`, `Lockout_does_not_reveal_whether_user_exists`. Accept: scripted attempt.

**18.5** `ci(security): scan for secrets and add DAST smoke`
- Changes: `gitleaks` job (required), OWASP ZAP baseline scan (`zaproxy/action-baseline`) against compose in a nightly job.
- Test: plant a fake key in a throwaway branch, the job fails. Accept: required check list updated.

**18.6** `chore(repo): declare API stable and cut 1.0.0`
- Changes: `CHANGELOG.md` regenerated, README final, `docs/` reconciled with the plan, OpenAPI marked `version: 1.0.0`.
- Test: `Openapi_info_version_matches_assembly_version`. Accept: tag `v1.0.0`, Release published, staging and production smoke green.

---

---

Previous: [M17](m17-maintenance-mode.md) | Next: - | [All milestones](README.md) | [Repository README](../README.md)
