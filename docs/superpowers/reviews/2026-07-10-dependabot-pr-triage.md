# Dependabot PR Triage

Date: 2026-07-10

Repository: `zh667/Mes-Agent`

This document records the Dependabot pull request state and decisions observed
after Phase 2 pull request #18 was merged into `main`. CI state is a snapshot
and must be checked again immediately before any merge.

## Interpretation Rules

- A red cross means a CI job failed. It is not a vulnerability severity.
- A green check means the current CI jobs passed. It does not prove a major
  dependency upgrade is compatible with the product architecture.
- Patch updates within the current framework major can usually be merged after
  reviewing the diff and confirming all required checks are green.
- Major framework, runtime, compiler, and Agent SDK updates require a dedicated
  migration task even when CI is green.
- Merge one dependency PR at a time. Wait for `main` CI to pass and for other
  Dependabot branches to rebase before processing the next PR.
- A PR whose checks ran before the latest `main` change must be rebased and
  rechecked.

## Current Triage

| PR | Update | Observed CI | Decision | Reason |
| --- | --- | --- | --- | --- |
| [#19](https://github.com/zh667/Mes-Agent/pull/19) | next-auth 4.24.7 -> 4.24.12 | Backend and Frontend passed on latest `main` | Merge first | Patch update in the current major |
| [#12](https://github.com/zh667/Mes-Agent/pull/12) | Microsoft.AspNetCore.OpenApi 8.0.25 -> 8.0.28 | Backend and Frontend passed on latest `main` | Merge after #19 | .NET 8 patch update |
| [#11](https://github.com/zh667/Mes-Agent/pull/11) | Microsoft.AspNetCore.Mvc.Testing 8.0.0 -> 8.0.28 | Backend and Frontend passed on latest `main` | Merge after #12 | Test dependency patch update |
| [#10](https://github.com/zh667/Mes-Agent/pull/10) | coverlet.collector 6.0.0 -> 10.0.1 | Backend and Frontend passed on latest `main` | Hold for coverage verification | Major test-tool update; verify coverage output before merge |
| [#3](https://github.com/zh667/Mes-Agent/pull/3) | actions/checkout v4 -> v7 | Passed against an older `main` | Rebase, then merge if green | Helps remove the Node.js 20 Actions warning |
| [#4](https://github.com/zh667/Mes-Agent/pull/4) | actions/setup-dotnet v4 -> v5 | Passed against an older `main` | Rebase, then merge if green | Helps remove the Node.js 20 Actions warning |
| [#2](https://github.com/zh667/Mes-Agent/pull/2) | actions/setup-node v4 -> v6 | Passed against an older `main` | Rebase, then merge if green | Workflow-only update |
| [#5](https://github.com/zh667/Mes-Agent/pull/5) | pnpm/action-setup v4 -> v6 | Passed against an older `main` | Rebase, then merge if green | Workflow-only update |
| [#7](https://github.com/zh667/Mes-Agent/pull/7) | postcss lockfile update to 8.5.16 | Frontend dependency installation failed against an older `main` | Rebase and retry once | If install still fails, close and regenerate the lockfile in a dedicated update |
| [#17](https://github.com/zh667/Mes-Agent/pull/17) | Next.js 14.2.35 -> 16.2.10 | Frontend dependency installation failed | Do not merge | Major framework migration |
| [#6](https://github.com/zh667/Mes-Agent/pull/6) | Next.js 14.2.35 -> 15.5.18 | Frontend dependency installation failed | Do not merge | Duplicate major Next.js upgrade path; choose one planned migration later |
| [#16](https://github.com/zh667/Mes-Agent/pull/16) | React 18 -> 19 | Frontend tests failed | Do not merge | Major React migration |
| [#15](https://github.com/zh667/Mes-Agent/pull/15) | TypeScript 5.9.3 -> 7.0.2 | Frontend lint failed | Do not merge | Major compiler/toolchain migration |
| [#14](https://github.com/zh667/Mes-Agent/pull/14) | eslint-config-next 14.2.35 -> 16.2.10 | Frontend dependency installation failed | Do not merge | Version is not aligned with current Next.js 14 |
| [#9](https://github.com/zh667/Mes-Agent/pull/9) | BotSharp 1.x -> 5.2.0 | NuGet restore failed | Do not merge | Major Agent SDK migration with dependency breakage |
| [#13](https://github.com/zh667/Mes-Agent/pull/13) | SignalR Client 8.0.0 -> 10.0.9 | Passed against an older `main` | Do not merge | Project targets .NET 8; keep runtime packages on the same major |

## Recommended Processing Order

1. Merge #19, wait for `main` CI.
2. Merge #12, wait for `main` CI.
3. Merge #11, wait for `main` CI.
4. Rebase #3, #4, #2, and #5; merge them one at a time only after fresh CI.
5. Rebase #7 once. Merge only if dependency installation and all checks pass.
6. Verify coverage output before deciding on #10.
7. Close the major upgrades marked "Do not merge". Use an ignore command when
   the same major update should not be recreated automatically.

## Dependabot Commands

Comment on the relevant Dependabot PR:

```text
@dependabot rebase
```

Use this for an intentionally deferred major version:

```text
@dependabot ignore this major version
```

Use this only when the generated branch or lockfile needs to be regenerated:

```text
@dependabot recreate
```

## Red-Cross Investigation Procedure

1. Open the PR and select `Checks`.
2. Open the failed `Backend` or `Frontend` job.
3. Read the annotation to identify the failed stage: install, restore, build,
   lint, test, or format.
4. Check whether the PR is based on the latest `main`.
5. Rebase stale patch/minor updates and wait for fresh checks.
6. Do not repair a breaking major upgrade inside an automatic Dependabot PR;
   create a planned migration branch with its own tests and review.

## References

- [Repository pull requests](https://github.com/zh667/Mes-Agent/pulls)
- [Dependabot pull request comment commands](https://docs.github.com/en/enterprise-cloud@latest/code-security/reference/supply-chain-security/dependabot-pull-request-comment-commands)
- [GitHub status checks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks)
