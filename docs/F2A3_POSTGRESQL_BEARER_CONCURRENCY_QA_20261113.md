# F2a-3 PostgreSQL reviewer bearer HTTP + competing EF writers QA

**Status:** SOURCE COMMITTED / NOT WINDOWS-TESTED / DRAFT-HOLD  
**Child branch:** `feat/yangspace-f2a3-pg-bearer-concurrency-qa-20261113`  
**Exact-SHA rule:** resolve the current child branch HEAD immediately before execution and use that exact 40-character SHA for both local HEAD and `refs/remotes/origin/${branch}`. Do not reuse a pre-documentation source SHA.  
**Parent:** F2a-1 branch / PR #17 at `e1a05c30fb2ff8ff967d5e028229b6d2bb8898d5`  
**Tracking:** [Issue #27](https://github.com/jvalkovv/YangSpaceApp/issues/27), parent contract [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20)

## Purpose

Close the remaining bounded QA gap between:
- the real ASP.NET Core Identity bearer HTTP reviewer contract previously tested with disposable SQLite; and
- the real Npgsql/EF PostgreSQL transaction contract previously tested without HTTP or competing writers.

This child adds a separate PostgreSQL-backed TestServer mode and a synchronized two-writer PostgreSQL transaction contract. It is synthetic-data QA only.

## Source changes

- `ClaimReviewPostgreSqlQa:Enabled` is a new explicit opt-in and defaults to `false`.
- The existing `ClaimReview:QaEnabled` SQLite-only guard remains unchanged.
- PostgreSQL reviewer QA requires `YangSpaceQa`, PostgreSQL provider, Preview/Admin/Registration/MerchantRead/SQLite reviewer QA all disabled, the explicit WebApplicationFactory TestServer marker, an exact runner-scoped connection string, a valid per-run scope token and a matching database sentinel. The API verifies the sentinel during startup before mapping the probe.
- The API refuses the PostgreSQL QA mode before host startup if the configured connection differs from `YANGSPACE_F2A3_PG_CONNECTION`, does not match the fixed loopback/disposable database policy, or does not contain the exact `yangspace_f2a3_qa_guard` token.
- The same read-only reviewer authorization probe is mapped only when the new PostgreSQL QA flag is explicitly enabled. No decision/mutation endpoint is added.
- The runtime transaction contract now starts two independent `DirectoryDb` contexts/transactions, makes both read the same Pending claims and Location revision before releasing them, then verifies one winner and full loser rollback.

## HTTP acceptance matrix

The new `--postgres-claim-reviewer-http-only` runtime mode uses `WebApplicationFactory`/TestServer and the real Npgsql-backed `DirectoryDb`. It does not call `EnsureCreated`, `Migrate` or seed demo data.

Required markers cover:
- default-off route and Identity login absence;
- refusal outside `YangSpaceQa` (Production and Development);
- refusal of SQLite provider, overlapping SQLite reviewer QA, missing disposable scope, mismatched scoped connection and mismatched sentinel;
- reviewed PostgreSQL migration presence;
- persisted Identity reviewer role, active-member conflict, self-review conflict, revoked-member behavior, unknown claim and empty subject fail-closed behavior;
- real bearer login and no cookie issuance;
- anonymous/invalid/cookie-only 401; dedicated reviewer 200; PlatformAdmin-only and non-reviewer 403;
- same-token persisted-role revocation immediately returning 403;
- registration and claim-decision route absence;
- no claim/org/location/member/audit side effects from the HTTP probe.

## Competing writer acceptance

The PostgreSQL transaction mode adds two distinct Pending claims from two organizations against one synthetic unclaimed Location. Two independent EF contexts begin separate PostgreSQL transactions and both read revision 0 before either writer is released.

Required markers verify:
- both writers read the same original revision;
- exactly one transaction commits;
- persisted winner state is internally consistent (Approved claim, Active organization, Location ownership and revision increments);
- exactly one active Owner membership exists for the winner;
- exactly four audit rows exist for the winner;
- the losing claim and organization remain Pending/unchanged, with no Owner membership or audit rows.

Each writer keeps one explicit outer transaction across two `SaveChanges` phases: first the concurrency-checked claim/org/location/Owner membership writes, then the four audit rows, followed by commit. The test accepts the expected optimistic concurrency failure or the named approved-location unique-index conflict as the loser outcome. The losing outer transaction must leave no business, membership or audit rows. Any other database exception is a failure.

**Scope qualification:** this is a test-only EF transaction routine against real disposable PostgreSQL. It is not yet a production application decision service or HTTP mutation endpoint. Uncertain-COMMIT/idempotency is explicitly NOT TESTED.

## Isolation

The runner `scripts/Test-F2a3-PostgreSql-Bearer-Concurrency-Local.ps1`:
- pins the exact tested HEAD and the child remote branch ref;
- restores/builds/publishes Release with warnings-as-errors;
- verifies the reviewed PostgreSQL initial SQL SHA-256;
- starts PostgreSQL 17 with `--network none`, tmpfs database storage, no published ports or host binds;
- starts a read-only .NET sidecar sharing only the PostgreSQL network namespace, with `/tmp` as tmpfs;
- transfers the runtime as a hashed tar stream and verifies both the runtime DLL and API `appsettings.json`;
- applies the reviewed SQL and creates a unique per-run sentinel;
- runs the transaction/concurrency mode, then the PostgreSQL reviewer HTTP mode;
- verifies every required marker, clean source tree and forced container cleanup.

No CT164/CT152, live database, GitHub Actions, external egress, production credentials or public-origin traffic is used.

## Operator command

From a clean detached Windows worktree whose `origin/feat/yangspace-f2a3-pg-bearer-concurrency-qa-20261113` ref equals the exact tested SHA:

```powershell
$Sha = '<CURRENT_40_CHARACTER_BRANCH_HEAD>'
.\scripts\Test-F2a3-PostgreSql-Bearer-Concurrency-Local.ps1 -CommitSha $Sha
```

Preserve the evidence directory under `logs/yangspace/f2a3-pg-bearer-concurrency/`, all stage stdout/stderr/command receipts, manifest, hashes, `gate.txt` absence and Docker cleanup proof.

## Remaining independent gates

- PostgreSQL runtime least-privilege application role.
- Application decision service boundary and HTTP mutation design.
- Mutation-boundary fault injection and uncertain-COMMIT/idempotency reconciliation.
- Off-host recovery and deletion-tombstone replay.
- Legal/privacy written clearance under Issue #24 before any real evidence or production claim decision.
- Final parent PR #17 DoD, security review and release authorization.

**No production claim decision, real evidence intake, production storage, live Owner grant or deployment is authorized by this child.**
