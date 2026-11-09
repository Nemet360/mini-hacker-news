# M12 - Rate limiting

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.12.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m12-rate-limiting`. Tag `v0.12.0`. Gate: line >= 90%. Uses the built-in `Microsoft.AspNetCore.RateLimiting`.

**12.1** `feat(ratelimit): add global per-IP sliding window limiter`
- Changes: 120 requests/min per client IP, 6 segments, no queue; rejected > `429`.
- Test: `121st_request_in_a_minute_returns_429`, `Window_slides_after_time_advances` (FakeTimeProvider via `RateLimiter` time abstraction or injected clock).
- Accept: `for i in $(seq 130); do curl -s -o /dev/null -w '%{http_code}\n' ...` shows 429s.

**12.2** `feat(ratelimit): add per-user limits on writes`
- Changes: policy `write`: token bucket, 5 tokens burst and 1 token per 12 s (≈ 5/min, B.4.4); comments the same; votes fixed window 60/min (A.4.8); key = `sub` claim.
- Test: `Sixth_post_inside_the_burst_returns_429`, `Limits_are_per_user`.
- Accept: scripted run.

**12.3** `feat(ratelimit): return Retry-After and RateLimit headers on 429`
- Changes: `OnRejected` writes `Retry-After` (seconds), `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset`, problem+json body.
- Test: `Rejected_response_has_retry_after_and_problem_body`. Accept: `curl -i`.

**12.4** `fix(ratelimit): key on the real client address behind a proxy`
- Changes: `ForwardedHeadersOptions` with `KnownProxies`/`KnownNetworks` from configuration; `X-Forwarded-For` honoured only from them (otherwise spoofing bypasses the limiter).
- Test: `Spoofed_forwarded_for_from_untrusted_peer_is_ignored`. Accept: the test.

**12.5** `feat(ratelimit): share counters across instances through Redis`
- Changes: Redis-backed sliding-window limiter (Lua script, single round trip) behind the same policy names; local limiter as fail-open fallback with a warning log.
- Test: `Limit_is_enforced_across_two_instances`, `Redis_outage_falls_back_to_local_limiter`. Accept: two instances, one limit.

---

---

Previous: [M11](m11-caching.md) | Next: [M13](m13-observability.md) | [All milestones](README.md) | [Repository README](../README.md)
