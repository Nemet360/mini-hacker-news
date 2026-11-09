# M11 - Caching

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.11.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m11-caching`. Tag `v0.11.0`. Gate: line >= 90%.

**11.1** `feat(cache): add Cache-Control headers to list and detail responses`
- Changes: anonymous reads: B.3.5's full value, `Cache-Control: public, max-age=5, s-maxage=15, stale-while-revalidate=30, stale-if-error=600` — `s-maxage` and `stale-if-error` are what degradation rung 4 (B.5.5) and edge maintenance (B.7.3) rely on; authenticated: `private, no-store`; **`Vary: Accept-Encoding` only** (B.3.5 forbids `Vary: Authorization`: it fragments the shared cache per token and destroys the hit rate the feed exists to get).
- Test: `Anonymous_list_is_publicly_cacheable`, `Authenticated_response_is_not_stored`.
- Accept: `curl -I`.

**11.2** `feat(cache): cache ranked pages with output caching and tag invalidation`
- Changes: `AddOutputCache` policy keyed by `sort,limit,cursor`; writes call `EvictByTagAsync("posts")`; votes deliberately do **not** evict (A.4.7 — 5 s of vote staleness is accepted).
- Test: `Second_request_is_served_from_cache`, `Create_post_invalidates_list_cache`.
- Accept: counters from a test `DbCommandInterceptor` show 1 query for 2 calls.

**11.3** `feat(cache): back the output cache with Redis for multi-instance deployments`
- Changes: `Microsoft.Extensions.Caching.StackExchangeRedis` + custom `IOutputCacheStore`; falls back to in-memory when `Redis:ConnectionString` is unset; Redis added to compose.
- Test: `Two_app_instances_share_cache_and_invalidation` (Testcontainers Redis). Accept: eviction on instance A visible on B.

**11.4** `fix(cache): prevent stampede on cold or evicted keys`
- Changes: per-key single-flight lock so only one request recomputes while others wait up to 2 s.
- Test: `One_hundred_concurrent_requests_cause_one_database_query`. Accept: the test.

**11.5** `feat(obs): expose cache hit and miss counters`
- Changes: `System.Diagnostics.Metrics` counters `minihn.cache.hit/miss` (exported in M13).
- Test: `Counters_increment_on_hit_and_miss` with `MeterListener`. Accept: values observed in test.

---

---

Previous: [M10](m10-postgresql.md) | Next: [M12](m12-rate-limiting.md) | [All milestones](README.md) | [Repository README](../README.md)
