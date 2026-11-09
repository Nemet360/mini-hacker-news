# M16 - Load testing

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.16.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m16-load`. Tag `v0.16.0`. Targets and the traffic model come from the load/attack section; this milestone only builds the harness.

**16.1** `test(load): add k6 read-heavy baseline script`
- Changes: `load/read.js`: ramp 0 → 200 VUs, 90% `GET /posts?sort=hot`, 10% `GET /posts/{id}`; thresholds `http_req_failed{rate<0.01}`, `http_req_duration{p(95)<200}` (the k6 figure is measured from the load generator and so includes network; the SLO's `p95 < 100 ms` in B.1.5 is measured at the load balancer).
- Test: the thresholds are the assertion. Accept: run against compose locally, summary stored in `docs/load/baseline.md`.

**16.2** `test(load): add mixed write, vote and comment scenario`
- Changes: `load/mixed.js` with `k6/execution` scenarios (setup registers 500 users, tokens cached per VU); 70/20/7/3 split reads/votes/comments/posts.
- Test: thresholds p95 < 250 ms for writes (B.1.5). Accept: no 429s except where the limiter is the target of the test.

**16.3** `ci(load): run the baseline nightly against staging`
- Changes: `nightly-load.yml` (`grafana/setup-k6-action`), results as artifact and job summary, fails on threshold breach; never targets production.
- Test: `workflow_dispatch` dry run. Accept: summary visible in the run.

**16.4** `test(load): add one-hour soak scenario`
- Changes: `load/soak.js` at 30% of peak for 60 min, records memory and connection-pool curves (from the M13 dashboards).
- Test: assertion: RSS growth < 10% over the hour, no error-rate drift. Accept: report committed under `docs/load/`.

**16.5** `perf(data): fix the bottlenecks the load tests found`
- Changes: placeholder step whose real contents are decided by the results (candidates: missing index, N+1, pool size via `MaxPoolSize`, `AsSplitQuery`). One commit per finding, each with a before/after k6 number in the body; this step may expand into several `perf(...)` commits.
- Test: the failing k6 threshold becomes green. Accept: numbers in the PR table.

---

---

Previous: [M15](m15-cicd-release-and-deploy.md) | Next: [M17](m17-maintenance-mode.md) | [All milestones](README.md) | [Repository README](../README.md)
