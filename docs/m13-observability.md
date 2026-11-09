# M13 - Observability

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.13.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m13-observability`. Tag `v0.13.0`. Gate: line >= 90%.

**13.1** `feat(obs): structured JSON logging with correlation ids`
- Changes: `Serilog.AspNetCore`, `Serilog.Formatting.Compact`, request logging middleware, `X-Correlation-Id` accepted (validated against `^[A-Za-z0-9_-]{8,64}$`), generated when absent, and echoed (A.4.12); log scope with `UserId` (never username or tokens).
- Test: `Response_echoes_correlation_id`, `Logs_do_not_contain_authorization_header_or_password`.
- Accept: `docker compose logs` shows one JSON line per request.

**13.2** `feat(obs): add OpenTelemetry tracing`
- Changes: `OpenTelemetry.Extensions.Hosting`, `...Instrumentation.AspNetCore`, `...Instrumentation.Http`, `Npgsql.OpenTelemetry`, OTLP exporter; 5% head sampling of successful traces in prod with 100% of errors and of requests slower than 500 ms (B.6.2), 100% in dev.
- Test: `Request_produces_server_span_with_db_child_span` (in-memory exporter).
- Accept: trace visible in the compose Tempo/Jaeger UI.

**13.3** `feat(obs): expose Prometheus metrics on an internal port`
- Changes: `OpenTelemetry.Exporter.Prometheus.AspNetCore` on `:9464/metrics` bound to the internal interface only; RED metrics (`http.server.request.duration` histogram), cache/vote/ratelimit counters.
- Test: `Metrics_endpoint_is_not_served_on_public_port`, `Request_duration_histogram_is_recorded`.
- Accept: `curl :9464/metrics | grep http_server`.

**13.4** `feat(obs): add liveness and readiness health checks`
- Changes: `/health/live` (process only), `/health/ready` (DB reachable via `AspNetCore.HealthChecks.NpgSql`, Redis optional); unauthenticated, no detail in the body.
- Test: `Live_is_200_when_db_down`, `Ready_is_503_when_db_down`, `Ready_body_has_no_connection_details`.
- Accept: stop Postgres in compose, observe 503 only on `/ready`.

**13.5** `build(docker): add Prometheus and Grafana to compose with committed dashboards`
- Changes: `observability/` configs, Grafana dashboard JSON (rate, errors, p50/p95/p99, DB pool, cache hit ratio).
- Test: `scripts/obs-smoke.sh` checks Prometheus target `up == 1`. Accept: dashboard panel shows traffic from a curl loop.

**13.6** `docs(obs): add SLOs and alert rules`
- Changes: SLO (99.9% read availability, p95 `GET /posts` < 100 ms, p99 < 300 ms — B.1.5), burn-rate alert rules in `observability/alerts.yml`.
- Test: `promtool check rules` in CI. Accept: promtool passes.

---

---

Previous: [M12](m12-rate-limiting.md) | Next: [M14](m14-containerisation.md) | [All milestones](README.md) | [Repository README](../README.md)
