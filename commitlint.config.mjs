export default {
  extends: ['@commitlint/config-conventional'],
  // Dependabot always capitalises its own verb ("build(deps): Bump X from a to b")
  // and cannot be configured out of it, so its exact shape is exempt from the
  // lower-case subject rule rather than the rule being weakened for everyone.
  ignores: [(message) => /^build\(deps\): Bump \S+ from \S+ to \S+/.test(message)],
  rules: {
    'type-enum': [2, 'always', [
      'feat', 'fix', 'refactor', 'perf', 'test', 'docs', 'build', 'ci', 'chore', 'revert'
    ]],
    // Scope is optional in Conventional Commits and optional in the commit-msg
    // hook, so it is optional here too: a tree-wide 'docs:' or 'ci:' has no one
    // scope to name, and this rule failed the first human pull request on eight
    // commits the hook had already accepted.
    'scope-empty': [0],
    'subject-case': [2, 'always', 'lower-case'],
    'subject-full-stop': [2, 'never', '.'],
    'header-max-length': [2, 'always', 72],
    'body-max-line-length': [1, 'always', 100]
  }
};
