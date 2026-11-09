# M15 - CI/CD, release and deploy

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.15.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m15-cicd`. Tag `v0.15.0`. The "test" for workflow steps is `actionlint` plus a dry run on a branch/tag in a fork.

**15.1** `ci: publish coverage baseline and image from main`
- Changes: `main.yml` (push to `main`): test, upload `coverage-baseline` artifact (30-day retention), build and push `ghcr.io/<user>/mini-hacker-news:main` and `:sha-<short>` with SBOM and provenance (`provenance: mode=max`).
- Test: the coverage gate now compares to the baseline. Accept: a PR dropping coverage by 1 pp fails.

**15.2** `ci: add release workflow with generated notes`
- Changes: `release.yml` on `v*.*.*`: versioned image, `git cliff --latest` notes, `gh release create`, attaches `openapi.json` and the SBOM.
- Test: tag `v0.0.0-rc.1` in a scratch fork. Accept: a Release with notes appears.

**15.3** `ci(security): add CodeQL and dependency review workflows`
- Changes: `codeql.yml`, `dependency-review.yml`, secret scanning with push protection confirmed on; `dependabot.yml` for `nuget`, `github-actions`, `docker`.
- Test: a PR adding a package with a known advisory is blocked. Accept: required checks list updated.

**15.4** `ci(deploy): deploy to staging with OIDC after every merge to main`
- Changes: `deploy-staging` job (C.4.4), IAM role with least privilege and trust on the `staging` environment, `scripts/deploy.sh` (ECS `update-service` by digest, `wait services-stable`), `scripts/smoke.sh`.
- Test: smoke script is the test. Accept: staging `GET /posts` returns 200 after a merge.

**15.5** `ci(deploy): add production environment with manual approval and rollback`
- Changes: `deploy-production` job with required reviewer and tag restriction; `docs/runbooks/rollback.md`; rollback = re-run the previous release deploy by digest.
- Test: rehearse a rollback in staging and record time. Accept: rollback < 5 minutes.

---

---

Previous: [M14](m14-containerisation.md) | Next: [M16](m16-load-testing.md) | [All milestones](README.md) | [Repository README](../README.md)
