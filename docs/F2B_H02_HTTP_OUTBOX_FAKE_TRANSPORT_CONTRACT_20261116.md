# F2b H02 — HTTP/outbox fake-transport execution contract

**Status: DESIGN / DRAFT / NOT RUNTIME TESTED.** Parent: [#33](https://github.com/jvalkovv/YangSpaceApp/issues/33), master register [#32](https://github.com/jvalkovv/YangSpaceApp/issues/32), O3 prerequisite [#36](https://github.com/jvalkovv/YangSpaceApp/issues/36). H01 prerequisite is accepted only at exact SHA `b36f155c37a8f707849100fa12f3b0e82637119f`.

## 1. Accepted inputs and invariants

- O2 schema/migration contract and O3 worker/provider contract remain pinned to their own accepted SHAs and evidence; H02 must not rewrite or relabel those results.
- O3 accepted delivery semantics are **at-least-once**, not exactly-once. A transport ACK can be uncertain; duplicate delivery is possible and must remain explicit.
- Outbox persistence contains only the approved bounded metadata. Never persist raw confirmation/reset tokens, message bodies, passwords, SMTP credentials, or recipient addresses in logs.
- Every HTTP mutation and outbox enqueue must use one transaction and the same `DirectoryDb` unit of work. User creation + confirmation intent must commit atomically; enqueue failure rolls back the Identity user.
- HTTP handlers never call SMTP/transport. They return the generic client contract after durable enqueue; transport failure is asynchronous and cannot turn an already committed request into a synchronous SMTP-dependent 5xx.
- Unknown/existing-account responses remain indistinguishable. Resend/forgot flows must not reveal account existence.
- Public registration and dispatch defaults remain OFF. No CT164, production DB, live SMTP, production secrets, real user data, or GitHub Actions.

## 2. H01 review boundary

H01 proves real Production Program startup against disposable PostgreSQL 17/TLS VerifyFull with public accounts OFF, and proves selected negative startup gates. It does **not** prove enabled Identity HTTP, positive account lifecycle, SMTP delivery, H02–H06, or release readiness.

The current compile-time `DispatcherAvailable=false` is an intentional release gate. H02 must not:
- change it to `true` just to make TestServer pass;
- add a test-only configuration switch that bypasses the production gate;
- weaken `ProductionPostgreSqlPolicy`, `ProductionSchemaGate`, TLS, role, origin, or environment validation;
- mark H01 as proof of positive public-account behavior.

## 3. Required prerequisite: reviewed release-authorization contract

Before positive HTTP lifecycle tests can start, define and review the exact production authorization predicate that permits public Identity endpoints and dispatch. The contract must specify:
- required Production environment and PostgreSQL provider;
- TLS VerifyFull and expected host/origin enforcement;
- schema readiness and migration compatibility;
- non-elevated runtime DB role and least-privilege access;
- durable outbox worker/transport readiness and safe failure behavior;
- explicit configuration ownership/default-OFF behavior;
- fail-closed startup and health/readiness semantics;
- separation between QA fake transport and production SMTP transport;
- no path by which QA configuration can activate public accounts on CT164 or production.

The implementation must have one shared policy used by real Program startup and tests. TestServer may replace only the transport implementation through normal DI composition; it may not replace or skip the authorization policy. If the authorization contract cannot be satisfied in a fully disposable fixture without weakening production policy, stop and revise the architecture rather than introducing a bypass.

## 4. H02 implementation slices

### H02-A — source and DI seam
- Identify the existing transport abstraction and outbox worker registration from the accepted O3 source.
- Ensure the fake transport is injectable through the normal DI registration seam; production registration remains the real configured transport.
- Ensure the fake records only safe test assertions and cannot connect externally.
- Ensure worker start/stop is deterministic in TestServer and can be awaited without sleeps or timing races.
- No real SMTP packages/credentials are needed in the H02 test path.

### H02-B — transactional HTTP enqueue
- Registration: create Identity user + enqueue confirmation intent in one transaction; failure to enqueue rolls back user creation.
- Resend confirmation and forgot-password: enqueue only for eligible accounts while preserving generic response semantics.
- No raw tokens or message bodies are stored; tokens are generated at dispatch from current Identity state.
- Duplicate/repeated requests are idempotent or coalesced according to the existing O2 uniqueness contract and abuse policy.
- SMTP/fake transport exceptions occur in worker execution, never inside the HTTP request.

### H02-C — deterministic fault/recovery matrix
- HTTP request succeeds only after durable transaction commit; verify row and user state from the disposable DB.
- Fake transport success marks the intent sent.
- Transient failure schedules bounded retry/backoff; retry succeeds after worker recovery.
- Permanent/exhausted failure reaches dead-letter with safe error code and no sensitive payload.
- Simulate process/worker restart and uncertain ACK; assert at-least-once behavior and no false exactly-once guarantee.
- Deleted, confirmed, changed-email, staff/ineligible, expired and already-consumed user states are re-read at dispatch and suppressed where required.
- Assert no token, password, message body, or recipient leaks into persisted outbox fields, logs, or test evidence.

## 5. Acceptance gates

- [ ] H01 exact-SHA accepted baseline remains unchanged.
- [ ] Release-authorization contract reviewed and represented by one shared production policy.
- [ ] Production TestServer starts only when the full authorization predicate is satisfied; negative variants fail closed.
- [ ] Fake transport injected via normal DI; no external network or SMTP.
- [ ] Register/resend/forgot HTTP flows prove atomic durable enqueue and generic anti-enumeration responses.
- [ ] Transport failure does not alter already committed HTTP result.
- [ ] Retry, dead-letter, restart, uncertain ACK and current-user-state recheck pass against disposable PG17/TLS VerifyFull.
- [ ] Existing SQLite regression and accepted O3 worker suite remain green.
- [ ] Exact-SHA operator runner, stage receipts, manifest hashes and independent Postflight pass; all temporary resources removed.
- [ ] PublicAccounts/dispatch defaults remain OFF; CT164/production DB/live SMTP untouched.

## 6. Execution decision

Do not begin H02-B positive HTTP tests until H02-A's shared authorization contract is implemented and reviewed. Do not merge this design PR or close #33. The next implementation PR must be a small Draft stacked on H01, with explicit file-level scope, exact SHA, operator runner, negative tests, and rollback notes.

**Current release gate: HOLD.**
