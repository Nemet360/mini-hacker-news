# M00 - Repository and tooling

| | |
|---|---|
| Status | **Implemented** |
| Tag | none |
| Coverage gate | CI green on a host-boots test |
| Boundary | before the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Goal: an empty, protected, CI-checked repo that builds a host with no endpoints. **No feature code.** Branch `milestone/m00-repo-and-tooling`. Gate: CI green on a host-boots test.

Not a commit but part of M0: create the repo, apply every setting from C.1.1, C.1.3 and C.1.7, create the labels, project board and GitHub Milestones M0-M18 (all steps of M2+ are *not* created as issues yet; only M0 and M1 issues exist before the interview).

**0.1** `chore(repo): initialise repository with gitignore, licence and readme`
- Changes: `dotnet new gitignore`, append local patterns, `LICENSE` (MIT), `README.md` stub, `.gitattributes`.
- Test: none possible (no code). Accept: `git status` clean after `dotnet build` on an empty folder; gitleaks passes on the tree.
- Doc-only/chore step, exempt from the "new test" rule per C.6.

**0.2** `build(sdk): pin .NET SDK with global.json`
- Changes: install the .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`), verify `dotnet --list-sdks` shows 8.0.x next to 6.0.400 and 7.0.306; add `global.json` with `rollForward: latestFeature`.
- **Fallback that works today with only 6.0.400 and 7.0.306:** pin `"version": "7.0.306"` and set the single `TargetFramework` to `net7.0` in `Directory.Build.props` (step 0.3). Because the TFM is defined once, moving to net8.0 later is a one-line `build(sdk): move to net8.0` commit plus package version bumps. Do not claim LTS while on net7.0 (it is out of support); prefer installing 8.
- Accept: `dotnet --version` run in the repo prints the pinned version.

**0.3** `build(deps): add Directory.Build.props and central package management`
- Changes: `Directory.Build.props` (TFM, nullable, analyzers, lock files, MinVer), `Directory.Packages.props` (empty `ItemGroup` ready), `.config/dotnet-tools.json`.
- Test: none yet. Accept: `dotnet restore` on an empty solution succeeds; `dotnet tool restore` installs the manifest tools.

**0.4** `chore(style): add editorconfig with analyser severities`
- Changes: `.editorconfig` from C.1.2; `TreatWarningsAsErrors` already on.
- Accept: introduce a deliberate unused `using` in a scratch file, `dotnet format --verify-no-changes` exits non-zero, remove it, exits 0.

**0.5** `build(solution): scaffold api and test projects`
- Changes: `MiniHn.sln`, `src/MiniHn.Api` (`WebApplication.CreateBuilder`, **no endpoints, no Swagger, no health route**: only what the brief allows), `tests/MiniHn.Api.Tests` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`, FluentAssertions, `coverlet.collector`), `public partial class Program;` for `WebApplicationFactory<Program>`.
- Test: `Host_boots_and_unknown_route_returns_404` (`GET /nope` == 404). Pins that the host starts without leaking an unplanned route.
- Accept: `dotnet test` 1 passed; `dotnet run --project src/MiniHn.Api` listens and `curl -i localhost:5080/anything` returns 404.

**0.6** `ci: add pull request workflow with build, format, tests and coverage gate`
- Changes: `.github/workflows/ci.yml` (jobs `commitlint`, `build-test`, `format`, `coverage-gate`, `vulnerable-packages`), `commitlint.config.mjs`. Coverage gate runs but the regression comparison is skipped when there is no baseline.
- Test: the workflow is the test. Accept: a throwaway PR with a failing assertion turns `build-test` red; with a `bad message` commit turns `commitlint` red; revert both, all green. Run `actionlint` locally.

**0.7** `chore(repo): add commit hooks, pr and issue templates and codeowners`
- Changes: `.githooks/commit-msg` and `pre-push`, `CONTRIBUTING.md` (one page: setup, `git config core.hooksPath .githooks`), templates from C.1.5/C.1.6, `CODEOWNERS`, `dependabot.yml`.
- Test: `scripts/test-hooks.sh` feeds 6 valid and 8 invalid messages to the commit-msg hook and asserts exit codes (run in CI as a step of `commitlint`).
- Accept: `git commit -m "Added stuff"` is rejected locally; the repo's PR page shows the template.

Milestone PR M0: merged with a merge commit; no tag. After merge, switch on branch protection with the **M0 subset** of the required checks (C.1.3); the later ones are added in the milestone that first produces them.

---

## Where this is in the repository

Branch `milestone/m00-repo-and-tooling`, merged into `main`. One commit per step:

| Step | Commit subject |
|---|---|
| 0.1 | `chore(repo): initialise repository with gitignore, licence and readme` |
| 0.2 | `build(sdk): pin .NET SDK with global.json` |
| 0.3 | `build(deps): add Directory.Build.props and central package management` |
| 0.4 | `chore(style): add editorconfig with analyser severities` |
| 0.5 | `build(solution): scaffold api and test projects` |
| 0.6 | `ci: add pull request workflow with build, format, tests and coverage gate` |
| 0.7 | `chore(repo): add commit hooks, pr and issue templates and codeowners` |

Files to look at: `global.json`, `Directory.Build.props`, `Directory.Packages.props`,
`.editorconfig`, `.githooks/commit-msg`, `scripts/test-hooks.sh`,
`.github/workflows/ci.yml`, `.github/workflows/build.yml`.

**Deviations from the plan as written**, all recorded rather than quietly absorbed:

* The SDK was installed per-user (`dotnet-install.sh`) because the machine-wide
  installer needed an elevation prompt. The pin in `global.json` is unaffected.
* `build.yml` was added on top of the planned `ci.yml`: the pull-request gate and
  "does this build on any branch" are different questions.
* Two extra tooling commits were needed and are in the history as themselves -
  `GenerateDocumentationFile` (without it Roslyn refuses to run IDE0005 on build)
  and a repository-wide LF normalisation.
* The GitHub-side half of M0 - branch protection, labels, the project board, the
  Milestones and the per-step issues - is not applied; it needs the repository
  owner's account. The local half (hooks, templates, CODEOWNERS, dependabot) is.


---

Previous: - | Next: [M01](m01-persistent-posts-and-get-posts.md) | [All milestones](README.md) | [Repository README](../README.md)
