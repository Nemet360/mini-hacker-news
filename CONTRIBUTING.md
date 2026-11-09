# Contributing

## Setup

```bash
git clone <this repo> && cd mini-hackernews
git config core.hooksPath .githooks   # commit-msg + pre-push checks
dotnet tool restore
dotnet restore
```

The SDK is pinned in `global.json` (8.0.404, `rollForward: latestFeature`).

## One step, one branch, one commit, one PR

* Branch from `main`: `milestone/m01-list-posts` for the milestone, `step/m01-5-get-posts` for a step.
* Conventional Commits only - the `commit-msg` hook rejects anything else, and CI
  lints every commit on the branch.
* Every behavioural commit adds the test that proves it, in the same commit.
* `dotnet format` must be clean and `dotnet test` green before you push; the
  `pre-push` hook runs both.

## Commands

```bash
dotnet build
dotnet test
dotnet test --collect:"XPlat Code Coverage"
bash scripts/test-hooks.sh
```
