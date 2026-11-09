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
    'scope-empty': [2, 'never'],
    'subject-case': [2, 'always', 'lower-case'],
    'subject-full-stop': [2, 'never', '.'],
    'header-max-length': [2, 'always', 72],
    'body-max-line-length': [1, 'always', 100]
  }
};
