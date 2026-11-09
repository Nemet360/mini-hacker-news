# M17 - Maintenance mode

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.17.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m17-maintenance`. Tag `v0.17.0`. Gate: line >= 90%. Two modes: **read-only** (reads served, writes `503`) and **full** (everything `503` except health).

**17.1** `feat(maintenance): add read-only mode middleware`
- Changes: `Maintenance:Enabled` and `Maintenance:ReadOnly` (B.7.3's `MaintenanceOptions`); in read-only all non-GET/HEAD/OPTIONS return `503` problem+json with `Retry-After: 120` and a `maintenance` extension field.
- Test: `ReadOnly_allows_get_and_blocks_post_put_delete`, `Retry_after_header_present`.
- Accept: curl in both modes.

**17.2** `feat(maintenance): add full maintenance mode with static response`
- Changes: in `Full`, everything except `/health/*` returns a prebuilt `503` body (no DB access); readiness and liveness both stay `200` (B.7.3) so the load balancer keeps routing and the app itself serves the maintenance body.
- Test: `Full_mode_serves_503_without_touching_the_database` (interceptor asserts zero queries), `Ready_and_live_stay_200_so_the_app_serves_the_maintenance_body`.
- Accept: the ALB target stays **healthy** and the instance itself answers the maintenance body — draining is exactly what B.7.3 avoids, because an empty target group serves a bare 502 instead of the `503 + Retry-After` ProblemDetails the feature exists to send.

**17.3** `feat(maintenance): toggle mode at runtime through an admin endpoint`
- Changes: `POST /admin/maintenance` (role `admin`, audit-logged, D.1.10), state in SSM Parameter Store / AppConfig with a mounted-file fallback, polled every 10-15 s (B.7.3) through `IOptionsMonitor`-style provider so all instances flip within seconds.
- Test: `Non_admin_gets_403`, `Toggle_propagates_to_second_instance`, `Toggle_is_audit_logged`.
- Accept: toggle once, both instances flip.

**17.4** `docs(maintenance): add zero-downtime migration runbook`
- Changes: `docs/runbooks/maintenance.md`: expand/contract migrations (add nullable > backfill in batches > enforce > drop in a later release), when read-only mode is required (never for expand steps), the rehearsal checklist, communication template.
- Test: `scripts/migration-lint.sh` in CI rejects migrations containing `DropColumn`, `RenameColumn` or `AlterColumn` to NOT NULL without an `-- expand-contract-ok` annotation. Accept: lint catches a seeded bad migration.

**17.5** `test(maintenance): rehearse a maintenance window against compose`
- Changes: `scripts/maintenance-drill.sh`: load running (k6 low rate), switch ReadOnly, run a migration, switch Off; asserts zero failed reads throughout and a bounded write failure window.
- Test: the drill's own assertions. Accept: output committed in `docs/load/maintenance-drill.md`.

---

---

Previous: [M16](m16-load-testing.md) | Next: [M18](m18-hardening-and-1.0.md) | [All milestones](README.md) | [Repository README](../README.md)
