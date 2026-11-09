# M14 - Containerisation

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.14.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m14-containers`. Tag `v0.14.0`. Gate: line unchanged; the container gate is the new job.

**14.1** `build(docker): add multi-stage Dockerfile with non-root runtime`
- Changes: build on `mcr.microsoft.com/dotnet/sdk:8.0`, run on `mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled` (no shell, runs as `app` user, `APP_UID`), `dotnet publish -c Release --no-restore -p:UseAppHost=false`, `DOTNET_EnableDiagnostics=0` in prod, `ASPNETCORE_HTTP_PORTS=8080`.
- Test: `scripts/container-smoke.sh` builds, runs, polls `/health/ready`, asserts `GET /posts` returns 200. Accept: image under **120 MB**, `docker run --rm minihn id` shows non-root.

**14.2** `build(docker): add dockerignore and layer-cache friendly restore`
- Changes: `.dockerignore`, copy `*.csproj` + lock files first, restore, then source.
- Test: smoke script asserts a no-change rebuild takes < 10 s. Accept: timings in PR.

**14.3** `build(docker): extend compose to run the full stack`
- Changes: `api`, `postgres`, `redis`, `otel-collector`, `prometheus`, `grafana`; `depends_on: condition: service_healthy`; resource limits (`cpus: 1.0`, `mem_limit: 512m` for api).
- Test: compose smoke runs the stack and curls through it. Accept: `docker compose up` from a clean clone works.

**14.4** `feat(config): graceful shutdown and container-friendly settings`
- Changes: `HostOptions.ShutdownTimeout = 30s`, `SIGTERM` drains in-flight requests, readiness flips to 503 on stop signal, config via env vars (`__` separators).
- Test: `Inflight_request_completes_after_shutdown_signal`, `Readiness_fails_while_stopping`.
- Accept: `docker stop` takes < 35 s with no 5xx for in-flight requests.

**14.5** `ci(docker): build image and scan with Trivy in pull requests`
- Changes: `container-scan` job from C.4.2 becomes live; Hadolint step for the Dockerfile.
- Test: introduce a known-vulnerable base tag in a throwaway branch and see the job fail. Accept: the job is a required check.

---

---

Previous: [M13](m13-observability.md) | Next: [M15](m15-cicd-release-and-deploy.md) | [All milestones](README.md) | [Repository README](../README.md)
