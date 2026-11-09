# ADR-018 — No GitHub Actions; operator-run validation and controlled PVE releases

**Date:** 2026-11-09  
**Status:** ACCEPTED — explicit project owner direction. Supersedes all previous YangSpace 2.0 plans that required GitHub Actions or GitHub-hosted CI.

## Decision

- **Never use, enable, run or recommend GitHub Actions for YangSpace.**
- GitHub remains **source control**, pull requests, code review and commit history.
- Validation is **local/operator-run** from an exact 40-character source commit in a clean disposable snapshot, with reproducible commands, logs and PASS/HOLD evidence.
- API integration tests run on a separate loopback-only ASP.NET instance with a disposable SQLite sandbox; they never touch the operator's live API or database.
- Private PVE preview is built manually from an approved allowlisted GitHub ref and exact commit SHA only after source QA and real API integration evidence exists; build never activates.
- PVE activation/backup/restore are separate controlled operator actions and must not be triggered by git push/merge.
- No Jenkins, Azure Pipelines, GitHub Actions, hosted CI, Docker/Kubernetes, IIS or other pipeline substitute is introduced without an explicit new decision.

## Why

The project owner expressly excludes GitHub Actions. Repeated efforts to require green GitHub-hosted workflow runs were invalid execution blockers, not product risks. The desired control is **traceable validation evidence**, not a particular automation provider.

## Replacement gates

1. Source: commit SHA is pinned and validated from a clean Git archive, not an uncommitted working tree.
2. Frontend: tracked lockfile integrity, npm ci, high-severity dependency audit, Angular typecheck/build and 17+ Playwright tests.
3. API: .NET restore/build with warnings-as-errors, real anonymous/nonadmin/admin API flows, expiry, and process-restart persistence, against disposable SQLite only.
4. Evidence: separate per-app logs, exact SHA, commands/exit statuses, owner review and PASS/HOLD decision.
5. Release: exact allowlisted remote ref tip equals approved full SHA; reviewed source and API manifest match; backups, private ACL/TLS and rollback plan verified.

## Implications

- The branch's active workflow file is **deleted**, not disabled or left as optional guidance. The old file remains visible in Git history. The default-branch historical workflow is outside this Draft PR and should disappear when the PR is merged; **no action is requested or performed on it now**.
- Absence of an Actions run must **never** block or delay YangSpace development.
- Tests remain mandatory and reproducible. Manual execution is not the same as skipping QA.
- Repository docs and tracker must use **manual validation** terminology. Historical entries may mention old Actions work as *superseded*, not as current dependencies.

## Evidence

- [S0 manual validation](S0_MANUAL_QA_RUNBOOK.md)
- [PoC Tracker](POC_TRACKER.md)
- [Private PVE runbook](POC_PVE_RELEASE_RUNBOOK.md)

**This ADR is the persistent project-level rule; it must not be reverted without explicit owner direction.**
