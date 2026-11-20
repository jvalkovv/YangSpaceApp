

### H06.1 — QA attempt 3: Angular fix verified; one selector ambiguity (2026-11-18)

- Exact commit tested: `fcb1cb0e4c73426701dec9e76bd9fd358da5d996`.
- `npm ci`: completed successfully; TypeScript typecheck: PASS; Angular production build: PASS.
- Account E2E: **19 PASS / 1 FAIL**. Full E2E: **45 PASS / 1 FAIL**. The only reported failure was `forgot/reset/resend flows`, where `getByLabel('Нова парола')` matched both the account section's accessible name and the actual password input. This is a Playwright selector ambiguity, not evidence of a reset API or UI behavior failure.
- The application zoneless change-detection fix is behaving as intended in the previously failing account flows.
- Test-only correction committed: `f28ef51051dd48de2a61bf0f50c5e84d7403cf4a`, replacing the ambiguous selector with the unique `#auth-password` locator. Assertions and application behavior are unchanged.
- **Evidence integrity caveat:** the operator detected `apps/web/angular.json` modified before checkout. `git checkout --detach` preserved that modification. Therefore the 19/20 and 45/46 results are useful diagnostic evidence but are NOT clean-source acceptance evidence. Re-run from a fresh detached worktree at exact commit `f28ef51051dd48de2a61bf0f50c5e84d7403cf4a`, verify `git status --porcelain` is empty before and after tests, then run typecheck/build, Account E2E and full E2E.
- The operator printed `H06_1_ALL_QA_GATES=PASS` despite prior explicit test failures. That marker is invalid and must not be used as acceptance evidence. PowerShell was run interactively, so a terminating error did not prevent a later separately-entered command from executing.
- PR #59 remains Draft; no merge/deployment. Registration and email dispatch remain OFF.


### H06.1 — QA attempt 2: zoneless account view-update defect (2026-11-18)

- Exact tested SHA: `22f0f454657156ce8c915376a910c3e8b7cec0ed`.
- `npm ci`: PASS; TypeScript typecheck: PASS; Angular production build: PASS.
- Account Playwright: **12 PASS / 8 FAIL**. Failures consistently show the mocked HTTP endpoint was reached (request counters incremented), while the expected feedback alert or `.auth-complete` element never entered the DOM.
- Root cause established from source: Angular 22.2 app has no `zone.js` dependency and bootstraps without Zone-based change detection. `AccountComponent` used mutable class fields (`success`, `feedback`, `isError`, `busy`) updated from asynchronous RxJS subscriptions without notifying the zoneless change-detection scheduler.
- Fix committed: `1cb1587430e3348afbbd6ef8d6cb96ebf7831a55` adds `ChangeDetectorRef.markForCheck()` after async success and through the shared error-feedback path. This fixes application behavior rather than weakening E2E assertions.
- New exact QA target: `1cb1587430e3348afbbd6ef8d6cb96ebf7831a55`.
- **Acceptance remains HOLD pending focused Account E2E, full E2E, typecheck/build and clean-source evidence at the new exact SHA.** No merge/deploy; public registration and email dispatch remain OFF. No backend or infrastructure change.


### H06.1 — QA attempt 1 and remediation (2026-11-18)

- [Issue #58](https://github.com/jvalkovv/YangSpaceApp/issues/58) · [Draft PR #59](https://github.com/jvalkovv/YangSpaceApp/pull/59).
- Tested implementation SHA: `cc71c962f8baaf11fc2e0d3ce586b0852a0695d5`. Result: `npm ci` PASS, TypeScript typecheck PASS, Angular production build PASS, Playwright **37 PASS / 6 FAIL**.
- Two failures were confirmed test-only strict locator ambiguity: the label `Парола` matched both password inputs on registration. Four HTTP-flow failures had no request counters/trace diagnostics, so root cause remains unproven.
- Remediation committed on PR branch; current QA target SHA: `cfc123ec13fd763c7778bfcc1ed4e30ffc990d2d`. Changes include exact label selectors, explicit JSON response mocks/request counters, 403 feedback, and added 403/503/network/rejected-session tests.
- **Acceptance remains HOLD until exact-SHA rerun passes all account and full Playwright tests.** No merge/deploy. Registration and email dispatch remain OFF.
- Reuse the existing QA worktree at `D:\\GitHub\\YangSpaceApp-F2b-H06.1-QA-20261118`; do not attempt to create it again. Verify clean state and checkout the new exact SHA first.


### H06.1 implementation candidate — Draft PR #59

- [PR #59](https://github.com/jvalkovv/YangSpaceApp/pull/59), branch `feat/yangspace-f2b-h06-1-account-browser-e2e-20261118`, exact head `238370a95ecda89d19e1b8a522820d91a7b2cbe6`, targets the F2b feature branch only (not main).
- Changes: account Playwright suite for route/default-off/confirmation/login/bearer/logout/reload/recovery/rate-limit; responsive account widths 320–1440; keyboard focus; accessible error/success announcement; explain memory-only session; clear rejected in-memory bearer after failed session probe.
- **Status: DRAFT / QA NOT RUN.** No build, typecheck, Playwright or browser PASS is claimed. Exact-SHA clean checkout validation is the next operator action. Do not mark ready or merge until all tests and independent postflight pass.
- Mock endpoints only. Registration/dispatch OFF. No real SMTP, production DB/key ring, CT164 or deployment. Issue #24 remains legal/privacy blocker for real evidence processing.


## 2026-11-18 — H06.1 active: Account UX and lifecycle Browser E2E

- H06 umbrella: [Issue #57](https://github.com/jvalkovv/YangSpaceApp/issues/57).
- First bounded implementation slice: [H06.1 Issue #58](https://github.com/jvalkovv/YangSpaceApp/issues/58).
- Scope: deterministic Playwright coverage and account UX fixes for register/confirmation/login/Bearer session/profile/logout/resend/forgot/reset, 400/401/403/404/503/409/429/network failures, generic non-enumerating feedback, memory-only token/no-cookie/no-storage contract, default-off registration/dispatch, account responsive widths 320–1440 and keyboard/focus semantics.
- Existing Angular account UI is present; current gap is lifecycle E2E and exact browser acceptance, not a backend rewrite. H01–H05 remain accepted; do not reopen without a concrete regression.
- Tests use mocked API only. No real SMTP, account activation, production DB/key ring, CT164 or deployment. No localStorage/sessionStorage token persistence.
- Legal/privacy Issue #24 remains OPEN and is the authority for real evidence-processing notice/lawful-basis decisions. Do not invent or publish legal notice content before responsible controller/legal approval.
- Remaining H06 after H06.1: whole-product responsive/browser coverage for directory/detail/admin, complete accessibility acceptance and approved privacy dependencies.
- No Cloudflare/Nginx/proxy/firewall/DNS/ingress work; no GitHub Actions.

# Current application execution focus — 2026-11-18

**Owner direction:** develop YangSpace as a complete product: UX/UI, Angular workflows, .NET business logic, persistence, authorization and real user journeys. The existing reachable application environment is `https://yangspace.gndstore.eu`.

**Explicit scope exclusion:** Cloudflare, Nginx, proxy, firewall, DNS and ingress engineering are not active application tasks. Do not expand the current work into infrastructure troubleshooting.

## 2026-11-18 — F2b H05 FULL ACCEPTANCE PASS; H06 ACTIVE

**H05 is complete.** The exact tested source SHA `a73422d9037df691857c3ff8b6b029532345d60c` passed all nine runtime gates: O4-H01 Production Startup, H02 Identity HTTP, H03 Abuse Protection, H04 Identity Edge, H05 DataProtection, H05b Identity Restart, H05c Operator Summary, H05d Admin HTTP and H05e Recovery/Readiness/Observability. Independent Postflight completed successfully with aggregate `F2B_H05_INDEPENDENT_POSTFLIGHT=PASS` and the H01/H02/H03/H04/H05b/H05c/H05d markers; H05e-specific manifest, marker, checksum, cleanup and clean-source assertions are included in that verifier.

- Evidence: `D:\GitHub\YangSpaceApp-F2b-H05e-QA-20261118-r3\logs\yangspace\f2b-o4-h05e-recovery-observability\a73422d9037d-20261118-002610-a3f3bb75`
- Manifest SHA-256: `6f367b711c8383529fda39ed28faad096db21f4f70c709aedeeaa64a84431f0d`
- H05e [PR #56](https://github.com/jvalkovv/YangSpaceApp/pull/56) merged into `feat/yangspace-f2b-public-identity-foundation-20261113` at merge commit `5df7f5c0519d532a2501bcf3194f64e3e585e770`; exact tested head remains `a73422d9037df691857c3ff8b6b029532345d60c`.
- H05 [Issue #50](https://github.com/jvalkovv/YangSpaceApp/issues/50) and H05e [Issue #55](https://github.com/jvalkovv/YangSpaceApp/issues/55) are CLOSED as completed.
- Parent F2b [PR #29](https://github.com/jvalkovv/YangSpaceApp/pull/29) remains Draft and targets the F2a feature branch. `main` was not changed; no production deployment occurred.
- **Next active milestone: H06 [Issue #57](https://github.com/jvalkovv/YangSpaceApp/issues/57)** — complete Angular UX/UI and user journeys, real-browser acceptance, mobile/tablet/desktop responsiveness, accessibility and privacy. Application work only; Cloudflare/Nginx/proxy/firewall/DNS/ingress are excluded.

Public registration and outbox dispatch remain OFF; fake transport only. No real SMTP, production DB/key ring, CT164, deployment or GitHub Actions. Overall F2b/product release remains HOLD until H06 and remaining MVP gates pass.


## 2026-11-17 — H05e runtime QA blocker: telemetry DI constructor (source correction prepared)

- Exact failed QA source: `3eb99abcea489575cdc3769efc92b659ee984798`.
- Windows Release restore/build passed; the SQLite worker regression stopped at DI container validation before PostgreSQL 17/TLS VerifyFull provisioning.
- Root cause: `IdentityEmailOutboxWorkerTelemetry` is an internal implementation type whose parameterless constructor was also internal. The default Microsoft DI `AddSingleton<T>()` implementation-type registration only selects public constructors, so the hosted worker could not be activated.
- Correction: make the constructor public (the telemetry type itself remains internal), and add an explicit Production worker-contract assertion for singleton DI activation. The SQLite runner and Independent Postflight now require `F2B_H05E_TELEMETRY_DI_ACTIVATION`; the manifest records `H05E_TELEMETRY_DI=PASS_PUBLIC_CONSTRUCTOR_SINGLETON_ACTIVATION`.
- This is a source/test-gate correction only. The failing run did not reach PostgreSQL; no runtime H05e acceptance is claimed. Re-run the complete exact-SHA runner and Independent Postflight after the correction commit. PR #56 remains Draft; Issues #50/#55 remain OPEN; release HOLD.

## 2026-11-17 — H05e SMTP fault-injection evidence correction (runtime QA PENDING)

- Correction baseline: PR #56 head `d845bea20730e9da0611ee06748509c13fdb874b`.
- The prior SMTP readiness probe only toggled the fake transport failure flag while requesting `/health/ready`; it did not exercise a send. The corrected Production TestServer test drives public registration through the real worker, observes the fake transport failure, verifies a persisted Pending outbox retry with at least one attempt, and proves readiness/liveness remain 200 while SMTP is unavailable.
- The fake transport is restored in `finally` before the existing resend/recovery test. No real SMTP is used.
- Runner and Independent Postflight require `F2B_H05E_SMTP_TRANSPORT_FAILURE_OBSERVED`, `F2B_H05E_SMTP_RETRY_PERSISTED`, `F2B_H05E_SMTP_UNAVAILABLE_READINESS_200`, and manifest entry `H05E_SMTP_FAILURE_READINESS=PASS_OBSERVED_FAKE_TRANSPORT_RETRY_AND_HTTP_200`.
- Source correction only; exact-SHA runtime QA remains PENDING. PR #56 remains Draft; Issues #50/#55 remain OPEN. Current PR #56 head is the acceptance SHA to test.

## 2026-11-17 — H05d accepted; H05e source implementation committed (runtime QA PENDING)

- H05d PR #54 merged into the F2b feature branch, not main. Exact tested source 5dc3939a62b0b2766988d21758bc22819f1cb99b; Release + PostgreSQL 17/TLS VerifyFull + full H01–H05c regression + H05d HTTP markers + Independent Postflight PASS. Evidence manifest SHA-256 d70584c89a532775a9e8acee4f6709138a13b74c62efc12bf18fc12524e0f35b. Merge commit 6b411be1295fcff92d1ce5af38aca4c407234bd8.
- H05e branch feat/yangspace-f2b-h05e-recovery-observability-20261117; integrated PR #56 baseline before SMTP fault-injection correction: d845bea20730e9da0611ee06748509c13fdb874b. See the correction entry above; the live PR #56 head is the current exact-SHA target.
- H05e source adds lease/revision fencing for Sent/retry/suppress, separates transport failures from DB finalization errors, preserves at-least-once recovery after Sent-write failure, verifies runtime DB/schema readiness, adds worker metrics/logs and a protected aggregate worker-status endpoint.
- H05e adds PG17 fault tests for stale lease owners and Sent-finalization DB failure, plus Production TestServer DB/schema loss and recovery checks. Runner and independent Postflight now require these H05e markers and use the H05e evidence root.
- **No H05e runtime PASS is claimed yet.** Operator must run exact-SHA Release build, full regression, disposable PG17/TLS VerifyFull/TestServer matrix and independent Postflight on this exact HEAD. H05 #50 and H05e #55 remain OPEN. H06 waits for full H05 acceptance. No main/CT164/production DB/key ring/SMTP/public activation/GitHub Actions changes.

## 2026-11-17 — H05c accepted and H05d protected admin HTTP source candidate

**H05c:** exact-SHA Release/PG17 TestServer + 2 outbox summary no-PII markers + Independent Postflight PASS at `8e2509e93551ac95b7dfaa52909c0b9a86b5037f`, manifest SHA256 `ab702f410da0623d231a1ad26e534dfbcad99a5bd15715cf3df1f7b6e5cdbd5c`. PR #53 Ready for Review, not merged.

**H05d:** branch `feat/yangspace-f2b-h05d-admin-http-replay-20261117`, stacked on H05c accepted source. Tests real Production `Program` with synthetic PlatformAdmin and non-admin bearer, protected gateway, negative access controls, privacy-safe summary/list, concurrent dead-letter HTTP replay and PostgreSQL CAS/idempotence evidence. Source candidate, **exact-SHA runtime QA pending**. Full H05 stays OPEN for any unproven worker fault/observability and readiness recovery contracts.

**PR integration:** PR #47 H03 moved to Ready for Review (bounded exact-SHA PASS); open stacked PRs #43/#46/#47/#49/#51/#52/#53 have not been merged. Formal independent maintainer approval is still absent. Do not describe a Ready PR as merged or close a partially accepted upstream PR. Production switches remain OFF.

## 2026-11-17 — H05b bounded PASS and H05c outbox operator summary source

**H05b:** full exact-SHA QA + independent Postflight PASS at `bdac3b0bc0159bb39a31b5f4f3337160f289d2ae`; nine Identity token restart/recovery markers PASS. Manifest SHA-256 `bc7420998a1de13dee891bb57b9d2e8d9ba17cb78b20d0b53ded6a6d17e33b9f`; PR #52 Ready for Review, not merged.

**H05c:** branch `feat/yangspace-f2b-h05c-outbox-operations-20261117` stacked on H05b accepted SHA. Adds a backend-only privacy-safe operational summary under the existing protected PlatformAdmin/gateway group, with PostgreSQL persisted-count and no-PII tests bound into exact-SHA Release QA/independent Postflight. **Source candidate only; H05c runtime QA pending.** Full H05 remains OPEN until actual Production TestServer HTTP authorization/gateway and replay fault/restart matrices, plus observability, are accepted.

**Focus remains application backend and Angular product functionality; Cloudflare/nginx work excluded.** Production release HOLD, public registration/dispatch OFF, no production database/SMTP/CT164/GitHub Actions.

## 2026-11-17 — H05a bounded QA PASS / H05b source only

**H05a accepted:** exact source SHA `93c9a77403c7820c18365d540e09fd147b13a90c`, full Release/PostgreSQL 17/TLS VerifyFull TestServer regression and 8 DataProtection markers **PASS**, Independent Postflight PASS. Operator manifest SHA-256 `d4792b4cdaac3409a813e18474fc2abc64b467161606e25de1db0dff9d119820`. PR #51 **Ready for Review**, not merged; formal approval remains separate.

**H05b started:** branch `feat/yangspace-f2b-h05b-restart-token-restore-20261117` is pinned on accepted H05a SHA. New real ASP.NET Identity tests generate confirmation/reset tokens from the first Production TestServer, stop it, confirm foreign-ring refusal in a replacement TestServer, simulate offline key-ring copy/restore, and verify the original tokens succeed in a new TestServer using the restored keys. No token or secret is logged. Full H01–H05a regression, H05b marker gates, manifest/hash verification and independent Postflight remain **PENDING** on the H05b head.

**Boundaries:** H05b is not full Issue #50 acceptance. Worker HTTP replay authorization, restart fault matrix, observability and off-host backup recovery remain open. No SMTP/CT164/Production DB/production key-ring/deployment/GitHub Actions. Public activation OFF.

## 2026-11-17 — H05 DataProtection foundation started

**H04 bounded final acceptance:** runtime + Independent Postflight PASS at exact source `8c68de48a64215eff57f2eaa2bd8594541bba2b3`. Operator evidence: `logs/yangspace/f2b-o4-h04-identity-edge/8c68de48a642-20261117-131434-d5921358` in detached operator worktree. Manifest SHA-256 `43cf7fe0cb00101ce628e01a8637a44d46dae73cec16b2aa03ef0f4298b938ad`. PR #49 is Ready for Review; no formal GitHub approval or merge claimed. Public release remains HOLD.

**H05 source candidate, untested:** separate branch `feat/yangspace-f2b-h05-app-recovery-20261117` is stacked on H04's accepted exact source, tracking [Issue #50](https://github.com/jvalkovv/YangSpaceApp/issues/50). Initial production runtime contract requires an operator-provisioned persistent DataProtection key ring with a stable application name, safe directory permissions and decryptable continuity sentinel, plus liveness/readiness checks. The Production TestServer synthetic harness covers key continuity across independently created providers, missing/foreign/corrupt ring cases and readiness loss/recovery; H05 runner/Postflight require exact SHA and full H01–H04 regressions.

**Do not infer PASS:** no H05 source build, PG17 TestServer run or Independent Postflight completed at the H05 branch. This is only a bounded DataProtection foundation; the full H05 worker restart, operator replay, backup/restore and observability acceptance remains open. No CT164, Production DB, live SMTP, production keyring/deployment or GitHub Actions. See `docs/F2B_H05_DATAPROTECTION_RECOVERY_20261117.md`.

## 2026-11-17 — H04 acceptance follow-up

**Status: SOURCE FOLLOW-UP COMMITTED / EXACT-SHA RUNTIME QA PENDING.** PR #49 remains Draft and Issue #48 remains open. The prior H04 runtime result is tied to its own exact source SHA and must not be reused after the follow-up changes below.

The H04 acceptance review identified two missing parts of the written Issue #48 DoD. The follow-up adds:

- Registration HTTP 400 cases for malformed, empty and oversized email; the oversized fixture is first verified as syntactically valid by `EmailAddressAttribute`.
- Empty and weak-password rejection with no Identity account or outbox insertion.
- Stable response-body equality across invalid-email boundaries and an exact before/after disposable outbox row-count assertion for all rejected registration cases.
- Persisted-state assertions that a successfully registered public account has zero Identity role assignments and zero `OrganizationMembers` rows.
- Missing and unknown confirmation-user cases fail closed in addition to malformed/invalid/expired tokens.
- Matching stable markers in the exact-SHA runner and Independent Postflight, plus manifest requirements and an updated H04 operator matrix.

The observation queries use the disposable migrator connection; the application Identity runtime role receives no additional tenant-table privileges. No production code switches, real SMTP, CT164, Production DB or deployment are changed.

**Acceptance remains HOLD** until the current remote PR head passes the complete Release/PostgreSQL 17/TLS VerifyFull runner and Independent Postflight, followed by scoped source review. Keep PR #49 Draft and do not enable public registration or outbox dispatch.

## Current application source audit

Baseline: H03 exact tested source `441c3a7742d7387b3d900d7f2bbee48cdd39001f`.

- Angular currently provides public directory/search/category filters, location detail, offers/brochures/jobs highlights, Identity screens and a PlatformAdmin preview CMS.
- `/admin` is not a business-owner workspace. Merchant API currently has only a QA-gated read endpoint.
- Organization, OrganizationMember, BusinessClaim and ContentAuditEvent persistence foundations exist, but there is no production owner-scoped profile/content mutation API or complete owner-facing CMS.
- Claim/reviewer HTTP remains QA-only. Legal/privacy Issue #24 blocks real evidence intake, live claim decisions and Owner grants.
- Public pages still carry demo-state messaging; the profile experience lacks full owner-managed content, opening-hour exceptions, rich media workflow, directions/map actions and privacy-reviewed public action reporting.
- Payments, checkout, bookings, POS, ATS/CV storage and native apps remain outside the approved MVP.

## Active sequence

1. **H04 — Identity HTTP edge cases:** negative/replay/role matrix on the real Production TestServer, disposable PostgreSQL 17/TLS VerifyFull, fake transport only. [Issue #48](https://github.com/jvalkovv/YangSpaceApp/issues/48).
2. **H05 — application recovery:** [Issue #50](https://github.com/jvalkovv/YangSpaceApp/issues/50) — DataProtection key continuity, outbox retry/re-drive, deterministic failure recovery and readiness behavior. No ingress work.
3. **H06 — Angular/browser/mobile/privacy:** account journeys, responsive navigation/forms, accessible loading/error/empty states, privacy notices and browser acceptance.
4. **M2 — ownership/claim:** synthetic-only workflow until legal/privacy #24; reviewer-only decision and atomic Claim + Organization + Location + Owner membership + audit.
5. **M3 — Merchant Workspace/CMS:** owner/editor dashboard, scoped profile editing, offers/brochures/jobs lifecycle, revision conflicts, audit history, preview/publish and tenant isolation.
6. **M4 — resident discovery:** verified geo, neighborhood filters, useful profile actions, map/list only with verified coordinates, favorites, canonical SEO pages and performance.
7. **M5/M6 — application release and Varna pilot:** privacy, data quality, recovery and product acceptance; Phase A 20–30 verified businesses, Phase B 100 only after Phase A passes.

H03 application-level abuse protection is accepted at `441c3a7742d7387b3d900d7f2bbee48cdd39001f`; evidence and manifest are recorded in Master Issue #32 and child Issue #34. H03 ingress work is excluded from this application roadmap.

---

## H01 launcher destination correction — source fix / runtime pending

The 2026-11-16 operator rerun passed content-root mkdir/copy/verify and launcher-copy, but `chmod` failed because `/run/pgcerts` was not an existing destination directory. The H01 runner now uses `/tmp/o4-h01-run.sh`, the same existing container path family as the O3 launcher: copy, chmod 0600, execute and cleanup all target the exact same path. This is a QA harness correction only; shared stage validation and production gates remain unchanged. New source SHA is required for full rerun; do not accept H01 on source-only evidence.

---

## O4/H01 stage-argument remediation — source candidate / NOT RUN

Observed operator run on `d947804f1733d922f15145d9ad2c98b7e61b844f`: O3 PostgreSQL Worker markers continued to PASS, but H01 stopped **before TestServer startup** at `STAGE_UNSAFE_ARGUMENT=o4-h01-content-root`. Root cause: H01 used `bash -c` with `&&`, shell quoting and redirection in command arguments, which `Invoke-YangSpaceStage.ps1` correctly rejects. The shared stage security boundary is **not** a defect and must not be weakened.

Remediation in the next H01 source commit:
- Replace the compound content-root command with separate safe-argv Docker stages: `o4-h01-root-mkdir`, `o4-h01-root-copy`, `o4-h01-root-verify`.
- Generate the H01 launcher directly from the already-scoped O3 launcher within PowerShell; copy it as a temporary file into the existing disposable container, set restrictive mode, invoke by file path. No shell operators, credentials or script source in the stage's `.runner.cmd` arguments.
- Update independent Postflight receipts for the new stages and require the temporary H01 launcher to be deleted. The existing Docker cleanup/production gate remains unchanged.
- Preserve `DispatcherAvailable=false`, PublicAccounts/Outbox defaults OFF, fake transport only, no CT164, production database, live SMTP or GitHub Actions.

**Observed result:** previous H01 run = HOLD (not H01 PASS). **Remediated SHA: NOT TESTED** until the operator reruns the full runner from a clean exact SHA; this includes O3 regression + disposable PostgreSQL + H01 TestServer + independent Postflight. Do not merge/close PR #43 or H01 without new runtime evidence. Historical O3 acceptance at `5e0546ae4adcc27d6c997f6f4bfa4eb03d2e28cd` is unaffected.

---

## 2026-11-16 — F2b O4/H01 disposable Production TestServer source candidate (NOT RUN)

O3 exact-SHA worker/provider acceptance remains anchored to `5e0546ae4adcc27d6c997f6f4bfa4eb03d2e28cd` (source SHA and evidence unchanged). Work proceeds on the **separate** `feat/yangspace-f2b-o4-h01-pg-http-startup-20261116` branch. This H01 source adds a real `Production` Program TestServer with PostgreSQL17/TLS VerifyFull, verified disposable sentinel/least privilege, `ProductionSchemaGate`, health endpoints, default-off registration 404 and negative startup/security gates. Existing O3 regression runs first on every H01 execution. A separate independent H01 Postflight checks receipts/hashes/cleanup.

**NOT RUN** on the H01 source; H01 remains unchecked and Issue #33 OPEN. The compile-time `DispatcherAvailable=false` is intentionally preserved. Enabled public Identity HTTP is **not testable** until a separately reviewed production release-authorization contract exists; no QA override of the production gate was introduced. The H01 result can validate production startup *with public accounts off*, never live deliverability. H02–H06 and O5/O6 remain OPEN. CT164/production DB/public SMTP untouched. No GitHub Actions.

Runbook: `docs/F2B_O4_H01_POSTGRESQL_STARTUP_QA_20261116.md`.

---

## 2026-11-16 — Expanded O3 PostgreSQL provider QA gates, source only (HOLD)

O3 PostgreSQL worker contract now adds password reset, already-confirmed suppression, uncertain SMTP-style acknowledgement simulation, crash/lease restart recovery, dead-letter and operator replay, retention purge, and absence of tokens/recipients at rest. Its fixture Password policy is explicitly configured, and reset tokens are Base64Url-decoded before verification. Dedicated non-superuser runtime role and narrow Identity reads remain mandatory.

The disposable PostgreSQL runner now performs its **independent Postflight automatically** (same exact source SHA), persists public-CA and Linux binary evidence, validates source/log hashes, and checks cleanup. It supports clean detached worktrees. Runbook: `docs/F2B_O3_POSTGRES_WORKER_QA_20261116.md`.

**No build or actual PostgreSQL execution of these new changes has occurred.** Source is not runtime PASS; O3 / PR #42 are HOLD/Draft. No CT164, live DB, SMTP, public HTTP or GitHub Actions. Next operator action is the single Windows PowerShell runbook block; capture the first failing marker or accepted evidence before progressing.

---

## 2026-11-16 — O3 PostgreSQL 17 worker QA source candidate (RUNTIME HOLD)

This iteration adds `scripts/Test-F2b-Outbox-Worker-PostgreSql-Local.ps1` (exact-SHA disposable QA) and `scripts/Verify-F2b-Outbox-Worker-PostgreSql-Postflight.ps1` (independent evidence verification).

- The PostgreSQL worker contract runs via the real Npgsql/EF Core provider, with a dedicated non-superuser migrator and limited runtime role.
- The runtime role has outbox DML and column-restricted Identity read; negative privilege probes reject credential reads and administrative writes.
- Tests cover confirmation token dispatch, retry/backoff, concurrent claims, expired lease recovery, and the full previously accepted SQLite synthetic regression.
- Disposable PostgreSQL 17 runs on local Docker only: internal network, no published ports, tmpfs-only storage, ephemeral TLS VerifyFull, no image pulls and no CT164/production access.
- The runner captures command receipts, source and log hashes, manifest and public ephemeral CA; secrets, container and network must be removed.
- The independent verifier checks exact local/remote SHA, evidence scope and hashes, required markers and resource absence.

**No new exact-SHA build, SQLite run, PostgreSQL run or independent Postflight has been executed in this source iteration.** Status is `HOLD`; this is not accepted evidence. Production dispatch and public registration remain default OFF. No real SMTP, no production DB, no deployment, no GitHub Actions. PR #42 remains Draft.

Operator from an exact clean branch checkout (or detached worktree after the runner supports it):

```powershell
$Sha = (git rev-parse HEAD).Trim()
& .\scripts\Test-F2b-Outbox-Worker-PostgreSql-Local.ps1 -CommitSha $Sha
# Take EVIDENCE_DIR emitted by the runner, then run independently:
& .\scripts\Verify-F2b-Outbox-Worker-PostgreSql-Postflight.ps1 -CommitSha $Sha -EvidenceDirectory '<EVIDENCE_DIR>'
```

Provider QA is a prerequisite for O3 approval; O4 and CT164 deployment remain blocked.

---

## 2026-11-16 — O3 SQLite synthetic + independent Postflight PASS; PostgreSQL worker QA next

Operator executed exact-SHA O3 worker synthetic at \`750f58ab8ce089161004bec1015cc1a25c760031\`:
- Exact source and all default-OFF guards: PASS.
- Restore and Release build: PASS.
- Complete SQLite synthetic worker contract: PASS.
- DI activation, confirmation/reset dispatch, dispatch-off, current email/confirmation/staff rechecks, expired-intent suppression, retry/backoff, uncertain ACK, concurrent claims, null/expired lease recovery, crash/restart recovery, dead-letter, operator replay, retention purge, no token/recipient at rest: PASS.
- Fake transport only, SQLite shared in-memory only, all stage receipts and clean worktree: PASS.

Evidence directory: \`logs/yangspace/f2b-o3-worker/750f58ab8ce0-20261116-064142-4ce016c0\`
Manifest SHA-256: \`9f59ee1d14a13aaa6f244d831fb94b25a3064b8d01fd98e32e53c8bcc5a90f45\`

The operator then executed \`Verify-F2b-Outbox-Worker-Postflight.ps1\` at the same exact SHA. **Independent Postflight: PASS**, including source/remote identity, clean worktree, evidence scope, manifest, all receipts and worker markers, and default-OFF guards.

**Next gate: disposable PostgreSQL 17 worker/provider QA.** Must exercise the actual Npgsql/EF Core provider path using an isolated PostgreSQL 17 instance, dedicated least-privilege runtime role, fake email transport, real database transactions/atomic claims, concurrent worker claims, lease expiry/recovery and failure/retry behavior. No production DB, CT164, real SMTP, public HTTP, deployment or GitHub Actions. The current O2 disposable PostgreSQL runner is migration-focused and must not be treated as proof of O3 worker/provider behavior.

After PostgreSQL QA: independent PostgreSQL evidence Postflight, provider-specific code review, then O3 acceptance decision. O4 remains gated. PR #42 remains Draft; Issue #36 and MVP remain OPEN.

---

## 2026-11-16 — dispatch-off failure isolated into independent assertions (SOURCE COMMITTED / RE-RUN PENDING)

Operator reran branch HEAD \`933adb6510ff82bcc6ea21b4d64cd9b85b8b24dc\`:
- PostgreSQL review QA default-off: PASS.
- Restore: PASS.
- Release build: PASS.
- DI activation: PASS.
- Synthetic failed at the combined \`F2B_O3_WORKER_DISPATCH_DEFAULT_OFF\` assertion (line 96).

The old compound assertion did not reveal which of its four conditions failed. Source now emits separate markers for runtime switch evaluation, batch blocking, hosted task creation, hosted-loop exit, Pending-state preservation and no fake delivery. The hosted task is awaited with a bounded two-second timeout rather than sampled immediately. This is diagnostic hardening, not a passing result.

Current source-code commit: \`4961d6fe3d53261ec916198b5a3fa2a4c254ebe1\`. Exact-SHA Release build, full synthetic contract and independent Postflight have not yet been rerun at this commit. O3 remains HOLD; PostgreSQL worker QA and O4 remain blocked. Production \`DispatcherAvailable=false\`; both appsettings switches remain false; no SMTP, CT164 DB, deployment or GitHub Actions.

---

## 2026-11-16 — O3 dispatch-off assertion isolated (SOURCE COMMITTED / RE-RUN PENDING)

Operator reran exact branch HEAD \`933adb6510ff82bcc6ea21b4d64cd9b85b8b24dc\`:
- PostgreSQL review QA default-off: PASS.
- Restore: PASS.
- Release build: PASS.
- DI activation: PASS.
- Synthetic failed at the combined \`F2B_O3_WORKER_DISPATCH_DEFAULT_OFF\` assertion (line 96).

The previous assertion combined four independent conditions, so the failing condition was not identifiable. The test now reports each independently:
1. Runtime config switches evaluate disabled.
2. \`ProcessBatchOnceAsync\` claims zero.
3. Hosted task is created and exits successfully within a bounded timeout.
4. Outbox row remains Pending.
5. Fake transport receives no delivery.

This is diagnostic test hardening, not an assertion that the worker already passes. Current source HEAD will be recorded after commit. O3 remains HOLD; do not proceed to PostgreSQL or O4 until exact-SHA synthetic and independent Postflight pass.

---

## 2026-11-16 — O3 production DI activation covered by synthetic contract (SOURCE COMMITTED / RE-RUN PENDING)

The worker contract now mirrors the actual production DI registrations and resolves:
- \`IEmailSender<IdentityUser> -> OutboxIdentityEmailSender\`
- \`SmtpPublicIdentityEmailSender\` without opening an SMTP connection
- \`IHostedService -> IdentityEmailOutboxWorker\`

The synthetic contract uses \`ValidateOnBuild=true\` and resolves the hosted worker and SMTP adapter, so non-public constructor/DI activation failures are caught before PostgreSQL QA. The worker, SMTP adapter and outbox sender now expose DI-discoverable public constructors (their containing types remain internal). New acceptance marker: \`F2B_O3_WORKER_DI_ACTIVATION=PASS\`.

Production dispatch authorization remains compile-time false until O3 acceptance; only the internal synthetic harness supplies the test-only constructor override with fake transport and in-memory switches.

Current branch HEAD: \`7681057020f11d23d9ce16be5aa36e84c12af613\` (latest source-code change: \`28ec89d29c4863dcc77016061eeb15e834d87b07\`; subsequent commit is documentation-only). No rerun has been executed at this HEAD; do not infer acceptance from source changes. PostgreSQL worker/concurrency, Postflight and O4 remain blocked.

---

## 2026-11-16 — production dispatch authorization remains compile-time held (SOURCE COMMITTED / RE-RUN PENDING)

The O3 remediation also restores the explicit production release boundary:
- \`IdentityEmailOutboxDispatchPolicy.DispatcherAvailable=false\` remains in production source until O3 acceptance is formally complete.
- Public Identity startup continues to fail closed if an operator enables PublicAccounts before O3 acceptance.
- The worker's optional synthetic override is an internal constructor argument only. The RuntimeTests harness supplies it with an in-memory SQLite database and fake transport; no configuration key, endpoint or deployment setting can enable it in production.
- Both runtime switches are still required even in the synthetic harness. The dispatch-off regression verifies that the hosted loop exits and the batch method returns without processing.

Current branch HEAD after this release-gate correction: \`de7980008c7109e83f9caf396e480c40ae18714d\`. The earlier exact-SHA build at \`f63149e8908d19945ca3dd5d2345798a92d68e2f\` passed restore/build but failed synthetic; the remediated head has not yet been rerun. PostgreSQL worker QA and O4 remain blocked.

---

## 2026-11-16 — O3 first local run failed; source remediation committed (RE-RUN PENDING / HOLD)

Operator ran exact source \`f63149e8908d19945ca3dd5d2345798a92d68e2f\`:
- Git/source identity: PASS.
- PublicAccounts, Outbox dispatch and PostgreSQL review QA default-off checks: PASS.
- .NET restore: PASS.
- Release build: PASS.
- Worker synthetic contract: FAIL at \`IdentityEmailOutboxWorkerContract.cs:76\`, \`Sequence contains no matching element\` while selecting the expected fake confirmation delivery.
- Independent Postflight: NOT RUN. O3 is NOT accepted.

Root causes and corrections now committed on the same child branch:
1. Enqueue used \`DateTime.UtcNow\` while the worker contract used a frozen \`MutableClock\`; the pending intent could be timestamped a few milliseconds after the worker's frozen time. \`OutboxIdentityEmailSender\` now uses the injected \`TimeProvider\`, and the synthetic contract passes the same clock and asserts timestamp consistency.
2. Candidate selection allowed a leased row with null \`LeasedUntilUtc\`, but the atomic claim predicate did not. Candidate and claim predicates now match, and null-lease recovery remains explicitly tested.
3. Source review found that the hosted worker did not honor \`PublicAccounts:Outbox:DispatchEnabled\`. Both the hosted loop and batch processor now fail closed, with a regression test proving the worker exits and does not dispatch when the switch is false.

- Remediated exact source HEAD: \`1618f2c4fdc7f19c9571523b9cd7721877a1b245\`.
- PR #42 remains OPEN/DRAFT; Issue #36 remains OPEN.
- Next gate: rerun exact-SHA O3 Release build + full SQLite synthetic contract, then independent read-only Postflight.
- PostgreSQL 17 worker/concurrency QA remains NOT RUN.
- PublicAccounts and Outbox dispatch remain false in appsettings. Real SMTP NOT CONNECTED. CT164/Production DB NOT ACCESSED. No deployment and no GitHub Actions.

Do not proceed to PostgreSQL worker acceptance or O4 Identity HTTP until the remediated exact-SHA synthetic contract and independent Postflight both pass.

---

## 2026-11-15 — F2b O2 RUNNER + INITIAL INDEPENDENT POSTFLIGHT PASS

The exact O2 runner and its first independent postflight completed successfully. This supersedes earlier O2 `NOT RUN / HOLD` statements below; those remain historical records of intermediate failed runs.

- Runner/source SHA: `e1944650f48f1ec9acc3161df0527a57c70defe9`
- Initial independent verifier SHA: `3e9697961aa2158fd06f40c0579b239879a42069`
- Evidence: `logs/yangspace/f2b-o2/e1944650f48f-20261115-220828-c3fc043e`
- Manifest SHA-256: `cdb36392e06cba0ee8b2f42e399d3a75562bc57ead88412e50260b41c06af03f`
- Operator result: `F2B_O2_POSTFLIGHT=PASS`

Accepted runner scope: SQLite and PostgreSQL 17 migration apply → rollback → reapply; model-diff and generated SQL review; TLS VerifyFull with hostname/untrusted-CA negatives; non-superuser migrator and Outbox-DML-only runtime role; FK/index/uniqueness/integrity; concurrency/Pending coalescing; Linux runtime contract; evidence hashes; disposable container/network and temporary-secret cleanup.

**Follow-up note:** after the successful postflight above, the current F2b SSOT/roadmap documents were updated to supersede stale NOT RUN/HOLD text. The v2 verifier now explicitly permits only those four status-document changes plus its own verifier and the recovery handoff. A final read-only postflight against the current branch HEAD is pending to verify that approved documentation-only diff. **Do not rerun the O2 migration suite.** The runner SHA, DB evidence and manifest remain unchanged.

Not accepted by O2: worker, SMTP delivery, PostgreSQL-backed Identity HTTP lifecycle, ingress/rate-limit abuse, browser/privacy/legal, CT164 deployment or production migration.

Safety: CT164 and production DB NOT ACCESSED; SMTP NOT CONNECTED; PublicAccounts OFF; dispatcher fail-closed; no GitHub Actions; no deployment.

Next implementation after final read-only postflight: Outbox O3 worker/lease/retry/dead-letter/fake transport in Issue #36; then Issue #33 HTTP integration QA. PR #41 remains OPEN/DRAFT pending source review and merge decision. Master M1c remains OPEN.

---

## 2026-11-15 — O2 storage false-negative root cause confirmed; PowerShell JSON parser corrected (SOURCE COMMITTED / NOT YET RE-RUN / HOLD)

The 2026-11-15 18:00 local diagnostic run at `e35f81bcdd73e58a87f2dc441493bdc8d537ea9e` proved Docker Engine 29.9.1/Linux created the intended storage layout: `HostConfig.Mounts` contains two tmpfs targets; effective `.Mounts` contains exactly two writable tmpfs destinations (`/var/lib/postgresql/data`, `/run/pgcerts`); `HostConfig.Binds=null`. The disposable PostgreSQL container was absent after runner cleanup.

**Confirmed root cause:** PowerShell surfaced the root JSON array from `ConvertFrom-Json` as one array-valued pipeline item. The runner then wrapped that item with `@(...)`, so `$Mounts.Count` became 1 and the two destinations were aggregated in property access. This was a validator false-negative, not a Docker storage-isolation failure.

**Correction:** parse the JSON array inside an object envelope and access its array property; add a two-element synthetic parser contract. Keep all effective tmpfs and no-bind/no-volume hard gates. Diagnostics remain enabled.

Earlier successful gates remain: RuntimeTests restore/build, both provider model diffs, both SQL reviews, SQLite apply → rollback → reapply/cleanup, internal Docker network and PostgreSQL 17 startup. PostgreSQL migrations, TLS VerifyFull, least-privilege migrator, integrity, concurrency and postflight are still pending.

**Next action:** run the full O2 runner against the exact current branch HEAD and then independent postflight. Prior evidence is not acceptance for the new SHA. CT164/production remain untouched; SMTP disconnected; PublicAccounts OFF; dispatcher fail-closed; no GitHub Actions.
## 2026-11-15 — O2 PostgreSQL storage gate: second failure; diagnostics added (SOURCE COMMITTED / NOT RUN / HOLD)

**Runner implementation commit:** `c58716df32c6d0107be36e72feba8852de87c7ea`. At execution time, fetch the branch and pass its exact current HEAD as `-CommitSha`; documentation-only commits may advance the branch without changing the runner implementation.

The rerun at `65d219e561972ee5aa9b496200be111c0717f986` again passed restore/build, SQLite + PostgreSQL model-diff, both SQL review gates, SQLite apply → rollback → reapply, SQLite cleanup, internal network creation and PostgreSQL 17 startup. It then failed at the effective PostgreSQL mount gate. The runner's `finally` cleanup ran; no PostgreSQL migration, TLS, integrity or concurrency test was reached.

**Correction to the prior diagnosis:** switching from Docker `--tmpfs` to `--mount type=tmpfs` did not resolve the observed failure. The earlier assertion that the image-declared `VOLUME` was the confirmed root cause was premature. Docker documents both flags as supported tmpfs mount methods; the effective mount output must decide the cause, not an assumption based on image metadata.

**New diagnostic source:** commit `c58716df32c6d0107be36e72feba8852de87c7ea` writes non-secret `o2-pg-storage-diagnostics.json` while the exact disposable container is alive. It records Docker Server version/OS, image and container `Config.Volumes`, `HostConfig.Tmpfs`, `HostConfig.Mounts`, `HostConfig.Binds`, and effective `.Mounts`. The failure now includes exact mount count/type/destination/writability/bind mismatches. The container is still removed in `finally`.

**Next action:** run the diagnostic-instrumented source once, then inspect the resulting JSON before making another storage-design change. Keep the hard requirement: exactly two writable tmpfs mounts at the PostgreSQL data and TLS paths, no bind/volume mounts. Do not weaken the gate or create additional QA containers/worktrees.

**Acceptance remains NOT RUN / HOLD.** CT164 and production DB remain NOT ACCESSED; SMTP NOT CONNECTED; PublicAccounts OFF; dispatcher fail-closed; no GitHub Actions. Prior evidence is tied to prior SHAs and is not O2 acceptance for this commit.

---

## 2026-11-15 — O2 PostgreSQL storage isolation correction committed (SOURCE COMMITTED / NOT RUN / HOLD)

**Current exact source SHA:** `4836c330794efc45e5afcc270d0096532d8f4dcd` on `fix/yangspace-f2b-o2-race-safe-20261115`.

The first local O2 run reached and passed RuntimeTests restore/build, both provider model-diff checks, both generated-SQL review gates, and the full SQLite apply → rollback → reapply contract. PostgreSQL 17 started, but the runner correctly stopped at the effective mount inspection gate before creating roles or applying PostgreSQL migrations.

**Root cause:** the official `postgres:17` image declares `/var/lib/postgresql/data` in image `Config.Volumes`. The previous runner used the separate Docker `--tmpfs` option and then detected an effective mount set that was not exactly two tmpfs mounts. The container and internal network were removed by the runner's `finally`; the follow-up read-only inspection confirmed both resources were already absent.

**Correction committed:** replace both `--tmpfs` options with explicit `--mount type=tmpfs` entries. The runner now verifies the effective `docker inspect .Mounts` array contains exactly two writable tmpfs mounts at `/var/lib/postgresql/data` and `/run/pgcerts`, checks that the image-declared data volume is overridden, and rejects any volume/bind mount. The independent postflight also requires the corresponding manifest fields. The runbook records the image-volume behavior and acceptance rule.

**Acceptance remains NOT RUN / HOLD.** The correction is source-committed but has not yet been executed on the local Docker Engine. The earlier SQLite PASS remains valid for SHA `b31f7375fdcd35378526357c362a599aa060e2c6`, not for the new exact SHA. Re-run the complete O2 runner against `4836c330794efc45e5afcc270d0096532d8f4dcd`; do not reuse prior evidence as proof for the new commit.

CT164 and production DB remain NOT ACCESSED; SMTP NOT CONNECTED; PublicAccounts OFF; dispatcher fail-closed; no GitHub Actions. CT164 remains gated on a full new-SHA O2 PASS and independent postflight PASS.

---

## 2026-11-15 — F2b O2 dual-provider runtime harness committed (SOURCE COMMITTED / NOT RUN / HOLD)

The six provider migration artifacts were committed and pushed on `fix/yangspace-f2b-o2-race-safe-20261115` at migration commit `b720f930d9cb64f1a6b26a854af37e8293ada3f5`. **This commit adds** opt-in RuntimeTests selectors for the real SQLite and PostgreSQL 17 migration paths, plus an exact-SHA Windows runner and independent postflight.

**O2 scope encoded in source:**
- SQLite and PostgreSQL: pinned baseline → Outbox migration → rollback → reapply.
- Exact schema, FK cascade, ordinary indexes, unique partial Pending index, duplicate/orphan rejection and data integrity.
- Concurrent enqueue, Pending coalescing, expired Pending refresh and revision CAS.
- PostgreSQL TLS VerifyFull with hostname/CA negative tests; migrator is non-superuser; runtime role is DML-only on Outbox.
- SHA256 manifest and cleanup checks.

**Storage boundary:** reuse the existing worktree; no additional clone/worktree. The runner requires an already-present local `postgres:17` image and never pulls one. It creates one disposable container on an internal network, loopback-only port, tmpfs data/cert mounts, and rejects all bind/volume mounts. It removes the container/network and temporary secrets. No Docker prune.

**Current acceptance state: NOT RUN.** The new O2 harness has not yet been compiled or executed. The source is not evidence of a passing test. The exact source SHA is pinned in the PR/commit and must match both local HEAD and origin before running [the O2 validation runbook](F2B_O2_VALIDATION_RUNBOOK_20261115.md).

**Hard boundary:** PublicAccounts OFF; dispatcher fail-closed; SMTP NOT CONNECTED; CT164 and production DB NOT ACCESSED; no deployment, no GitHub Actions. CT164 starts only after O2 PASS and independent postflight PASS.

---

## 2026-11-15 — Outbox parent reconciliation created; exact-SHA QA PENDING

Evidence archive for F2b parent was independently accepted: `YangSpace-F2b-Reconciled-20261115-070631-e24cc3d0.zip`, SHA-256 `8D5F16512503093A2FBB3FEF0452A545968C646A070CA5D4E32095E718102714`; ZIP integrity PASS and 32/32 internal checksums PASS. Parent tested code SHA `8b887dd657323c9e4bae6b7864110f5f61eb04e7`; parent branch docs-only head before this Outbox slice was `1ef4a4fb5645b345f3dfe0dd855bba901598434e`.

**Outbox reconciliation:** created new branch `feat/yangspace-f2b-outbox-reconciled-20261115` and Draft PR #40, with one source reconciliation commit `6a7594e622d436b0be904bcd2e78486fae609845`. Git compare is clean **1 ahead / 0 behind** current parent. It reapplies the 11 Outbox paths from stale PR #37, preserves the F2a SQLite recovery selector + F2b auth-rate selector and adds the Outbox selector exactly once. All three PowerShell branch guards now target the new branch. PR #37 remains open/draft as the historical stale-base proposal; no force-push or close.

**Next:** exact-SHA O1 native PowerShell parser + .NET Release build + disposable SQLite enqueue/coalesce/rollback + independent postflight on the current reconciled branch HEAD. Prior O1 evidence from `4aeeb1c351140f43168aad1eaa62d3bd31ce00ae` is from the old parent and is not inherited. Only after fresh O1 PASS run offline dual-provider EF Core 10 migration scaffold, inspect SQLite/PostgreSQL SQL and model snapshots, then test both migrations on disposable DBs with restore.

**HOLD:** worker/leases/retries/poison/uncertain SMTP ACK, real PostgreSQL HTTP/TLS/least-privilege, SMTP delivery, trusted proxy/ingress, browser/a11y, privacy/legal and production recovery remain open. PublicAccounts OFF; CT164/production untouched; no migration apply, real mail, GitHub Actions or deployment.
 
---

## 2026-11-15 — F2b reconciled evidence archive independently VERIFIED

**Archive:** `YangSpace-F2b-Reconciled-20261115-070631-e24cc3d0.zip`
**Archive SHA-256:** `8D5F16512503093A2FBB3FEF0452A545968C646A070CA5D4E32095E718102714` — exact match.

Independent ZIP review: ZIP integrity PASS (33 entries); internal SHA256SUMS **32/32 PASS** with no missing/unlisted payload files; all stderr logs empty. Bundle and source manifests identify tested code SHA `8b887dd657323c9e4bae6b7864110f5f61eb04e7`.

Observed source/runtime evidence: .NET 10 Release builds PASS (0 warnings/0 errors); Angular npm ci/typecheck/production build PASS; tenant isolation 19 PASS markers; SQLite historic upgrade + data/Identity preservation PASS; 3 fault injection boundaries and 3 restore-recovery paths PASS; auth-rate partition contract 8 PASS markers. Forged XFF ignored. Proxy forwarding/external abuse remain NOT_TESTED.

**Accepted:** exact tested-code source + isolated synthetic runtime QA only. Current PR #29 head `efa51ea7f9f3d591a05729d234bf6a0328a97caa` is docs-only relative to tested code SHA.

**Still HOLD:** PostgreSQL HTTP account lifecycle (VerifyFull + least-privileged role), SMTP delivery/sink, durable Outbox worker/lease/retry/poison/recovery, trusted proxy/ingress abuse, browser/accessibility, privacy/legal, production deployment and backup/restore. PublicAccounts OFF; CT164 NOT ACCESSED; no production DB, real mail, DB migration apply or GitHub Actions.

**Outbox dependency:** PR #37 remains based on stale `a95d242759fcba774eccc1c258b1c253386082cb`; current parent `efa51ea7f9f3d591a05729d234bf6a0328a97caa` vs PR #37 head `f84da94bb7c5a6334a79c607fb4543227e2be69d` is diverged 3 ahead / 11 behind. Reconcile onto current parent on a new branch (no force-push), preserve both F2a/F2b RuntimeTests selectors, update branch guards, rerun exact-SHA O1 QA, then run offline dual-provider migration scaffold. Keep PR #29/#37 Draft/HOLD; no release.

---

## 2026-11-15 — F2b reconciled exact-HEAD source + isolated runtime QA (OPERATOR PASS / ARCHIVE REVIEW PENDING)

**Tested commit:** `8b887dd657323c9e4bae6b7864110f5f61eb04e7` — two-parent F2a/F2b reconciliation.

Operator transcript reports:
- Exact remote SHA and detached clean worktree: PASS.
- F2b source: .NET restore/Release build, Angular npm ci/typecheck/production build, static security invariants and PublicAccounts default-OFF: PASS.
- Integrated RuntimeTests restore and Release build: PASS.
- F2a tenant isolation selector: PASS.
- F2a historic SQLite organization-claim upgrade/recovery selector: PASS, including successful upgrade, data/identity preservation, no Outbox table and all three fault/restore boundaries.
- F2b synthetic auth-rate partition selector: PASS.
- Final source worktree cleanliness: PASS.

Operator evidence:
- Source evidence directory: `logs/yangspace/f2b-public-identity/8b887dd65732-20261115-070556-82f1a70d`.
- Source manifest SHA-256: `D5E90D02DC4ACF1E56626F3BEDEC025E5B13A3850F00599E8A8CDD80198DD19B`.
- Runtime evidence directory: `logs/yangspace/f2b-reconciled-runtime/20261115-070631-e24cc3d0`.
- Archive: `YangSpace-F2b-Reconciled-20261115-070631-e24cc3d0.zip`.
- Archive SHA-256 reported by operator: `8D5F16512503093A2FBB3FEF0452A545968C646A070CA5D4E32095E718102714`.

**Evidence classification:** operator-reported PASS from the submitted terminal transcript. ZIP bytes and internal SHA256SUMS have not yet been independently verified in review. The archive must be supplied and checked before independent evidence acceptance.

**Still NOT tested/accepted:** real PostgreSQL HTTP account lifecycle, Production TLS VerifyFull + least-privilege DB role, SMTP delivery, durable Outbox worker/retry/poison behavior, real proxy forwarding/ingress abuse, browser accessibility, privacy/legal review, production restore/deployment.

**Hard boundaries:** PublicAccounts remains OFF; no real registration/email, CT164/production DB, PVE deployment, Outbox migration or worker, or GitHub Actions. PR #29 remains OPEN/DRAFT/HOLD pending archive review and remaining product gates. PR #37 remains separate Draft; its Outbox work is not authorized to merge ahead of the parent acceptance.

## 2026-11-15 — F2b PR #29 parent reconciliation (SOURCE COMBINED / QA PENDING)

This branch integrates accepted F2a SQLite baseline and historic-data/restore-only QA from parent PR #17 at `37bca4e008d07c5e4748074e82ff9e9d5bd2fcd7` into F2b Public Identity PR #29 (prior HEAD `a95d242759fcba774eccc1c258b1c253386082cb`) by a non-force two-parent merge commit.

- All F2a-only SQLite EF migration, snapshot, historic-upgrade QA and support scripts are preserved as exact parent Git blobs.
- Runtime test CLI now keeps **both** F2a historic SQLite upgrade/recovery selector and F2b synthetic auth-rate partition selector.
- Existing F2b Identity endpoints, Angular account UI, rate limiting and default-OFF security gates remain unchanged.
- Historical documentation from both branches is retained ahead of their common ancestor, preserving source-specific acceptance and HOLD boundaries.
- **Acceptance is NOT inherited** from the older F2b build. Next gate: exact-new-HEAD Windows .NET 10 Release and Angular typecheck/build, plus both runtime selectors and independent evidence review.
- PR #37 remains separate Draft Outbox child. No Outbox migration is generated or applied here, and no CT164, live SMTP, real registration, production DB, PVE deployment or GitHub Actions is authorized.

## 2026-11-15 00:49 Europe/Sofia — F2a-S3 integrated parent evidence review ACCEPTED

Independent review completed for operator archive `YangSpace-F2a-Parent-Integrated-20261115-003349-ece87e11.zip`.

**Archive integrity and identity**
- ZIP integrity test: PASS; 72 entries; 82,901 bytes uncompressed.
- Archive SHA-256: `363AEA8BAA7BE3633A26F6D8DB182CFA6C3D12418820EA28E6C77773CC92F9DF`.
- All 71 entries in `SHA256SUMS.txt` were independently re-hashed: **71/71 PASS**, zero mismatch/missing/malformed.
- Root manifest identifies exact tested source `511a377a5d8436802fd17cc62c13f831c9b51832`, tree `58741d770eb4d5f3ceaa741e03811d3b8b317f7c`.
- 22 runner/stdout/stderr triplets are present; all 22 stderr files are empty; no `gate.txt` is present. Eight empty PostgreSQL stdout logs are expected for copy/dump/restore operations or EF SQL redirected to the reviewed SQL file.

**Tenant scope**
- Exact-SHA tenant manifest PASS; restore/build PASS (Release build 0 warnings/0 errors).
- All 18 tenant contract markers present, including cross-tenant denial, revoked membership, suspended org, legacy unclaimed location, no implicit PlatformAdmin ownership and stale revision denial.
- Evidence: `tenant/` in the reviewed archive.

**PostgreSQL 17 disposable migration**
- Exact-SHA PostgreSQL runner source reviewed: it requires exact HEAD, matching origin tip, clean worktree, local Docker endpoint, no network, no published ports and no bind mounts; PostgreSQL data is on tmpfs.
- Reviewed generated SQL SHA-256 matches the runner's pinned expected value and PG manifest: `2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7`.
- Static SQL inspection: 15 mapped tables + EF history, 17 foreign keys, 24 indexes; no DROP/TRUNCATE/DELETE statements. Approved/Pending partial unique indexes are present.
- Constraints script from the exact tested commit validates schema counts, competing pending claims, duplicate rejection, single approved claim, FK rejection, reapplication after withdrawal, sequential revision CAS and audit rollback; all required markers appear in stdout.
- Synthetic backup has PostgreSQL custom-dump `PGDMP` header; SHA-256 matches manifest: `221ffd3fa0afa8a43b74c7badce069b7770c56bf72355e34546c644ae003187f`. Restore verification checks schema, EF history, Identity users, orgs, locations/revision, claim states, audit and unique indexes. Restore marker PASS.
- PG manifest explicitly retains REAL_BEARER, PARALLEL_WRITERS and APP_ROLE_LEAST_PRIVILEGE as NOT_TESTED.

**Historic SQLite upgrade and recovery**
- Release build PASS (0 warnings/0 errors); all required historic baseline, Location fields, Offers/Brochures/Jobs, Identity users/roles/dependencies, new F2a relationships, migration history, SQLite integrity/FK and no-Outbox markers present.
- Three distinct injected migration interruption boundaries PASS; each recovery uses the verified pre-upgrade backup (restore-only, no blind retry). All three recovery markers PASS.
- SQLite stage command/stdout/stderr triplets and complete runtime output are present. The root bundle manifest binds the integrated run to the exact parent SHA above.

**S3 decision: ACCEPTED.** The evidence package is internally consistent and supports the required disposable-only F2a-S3 acceptance. The operator's root manifest was intentionally written with `RELEASE_GATE=HOLD_PENDING_EVIDENCE_REVIEW`; this review records that evidence gate as accepted without altering the immutable archive.

**Scope boundary remains unchanged:** no production DB/CT164 access, HTTP listener or PVE deployment. This does NOT test PostgreSQL-backed Identity Bearer HTTP, true concurrent application-layer EF writers, runtime app-role least privilege, off-host DR, real claim decision endpoints or real evidence processing. Those remain separate parent/F2b/legal gates. PR #17 remains OPEN / Draft / HOLD.

Issue [#38](https://github.com/jvalkovv/YangSpaceApp/issues/38) — F2a SQLite baseline repair — is complete and may be closed. F2b O2/O3/O4 and PR #29/#37 reconciliation remain follow-on work; no Outbox work is accepted by this review.

## 2026-11-15 00:34 Europe/Sofia — F2a parent integrated runtime regression PASS; evidence review pending

Operator completed the final integrated regression on exact parent source SHA `511a377a5d8436802fd17cc62c13f831c9b51832`, tree `58741d770eb4d5f3ceaa741e03811d3b8b317f7c`.

**Three independent runtime gates PASS:**
- Tenant scope: restore, Release build and isolated tenant-scope contract PASS. Evidence: `logs/yangspace/f2a-tenant-scope/511a377a5d84-20261115-001115-f235b864`.
- PostgreSQL 17: EF tool/project restore, Release build, model diff, reviewed initial migration SQL, disposable container migration, constraints, sequential CAS, transaction rollback, synthetic dump/restore and restored-state verification PASS. Evidence: `logs/yangspace/pg-disposable/511a377a5d84-20261115-002151-53c5a86c`. Container cleanup PASS.
- SQLite: historic baseline, Release build, successful migration, legacy Locations/Offers/Brochures/Jobs and Identity preservation, F2a relationships, integrity/FK checks, no-Outbox assertion, three fault injections and restore-only recovery 3/3 PASS. Evidence: `logs/yangspace/f2a-sqlite-parent-integrated/20261115-003349-ece87e11`.

Combined bundle: `logs/yangspace/f2a-parent-integrated-bundle/20261115-003349-ece87e11`.
Operator ZIP: `YangSpace-F2a-Parent-Integrated-20261115-003349-ece87e11.zip`. The console transcript supplied only a truncated archive SHA-256; the full digest and archive bytes have not yet been supplied for independent byte-level verification.

**Acceptance boundary:** this is a successful isolated runtime regression, not production/release approval. The operator evidence ZIP must be uploaded and independently checked for archive integrity, manifest identity, SHA256SUMS, runner receipts, stdout/stderr, required markers and absence of failure gates. No CT164/production DB, PVE deployment or HTTP listener was accessed. Real PostgreSQL Bearer HTTP and real application-layer parallel writers remain NOT TESTED. Claim decision mutation and real evidence processing remain gated separately by the existing legal/privacy requirements.

**GitHub state at this checkpoint:** PR #17 remains OPEN / Draft / mergeable-clean at the tested SHA; issue #38 remains OPEN until the independent evidence review completes. Open PR inventory: #17, #29, #37 and #1 (four total). PR #29 is currently mergeable-state DIRTY against its older F2a base; #37 is stacked on #29 and must be reconciled only after parent acceptance. PR #1 remains the V2 foundation Draft.

**Next:** upload the exact operator ZIP for independent verification. Keep PR #17 Draft/HOLD and issue #38 open until the archive review and remaining acceptance boundaries are explicitly resolved. No GitHub Actions.

## 2026-11-14 — F2a-S SQLite baseline merged into F2a parent

- Child [PR #39](https://github.com/jvalkovv/YangSpaceApp/pull/39) is **MERGED/CLOSED** into this F2a feature branch by squash commit `da31427d47bc86994f4e3e800900101383ec81d1`; it was not merged to V2 `main` or deployed.
- Exact operator-tested source: `6adf829affbfad9eb69786fe5635e71762d356b9`. The merged commit and tested commit have the identical Git tree `e7b7d03d67a9ab0366a1cc5217a3ddeea2b2eaf6`, so the squash introduced no source-tree drift.
- F2a-S1 offline EF scaffold, SQL/model review, F2a-S2 historic SQLite data-preservation and three fault-injection/backup-restore cases, exact-tree Release build and post-commit evidence review: **PASS**.
- Independent post-commit evidence archive SHA-256: `a05ef6f21815feec41405582f359d38dd73148e777c089eb8dc0629b8bf1786b`. All 10 checksum entries verified; Release build 0 warnings/errors; 17 runtime acceptance markers PASS.
- Migration scope: four F2a domain tables, nullable `Locations.OrganizationId`, `Locations.Revision`, expected indexes/FKs; no Outbox. Legacy Locations, Offers, Brochures, Jobs and Identity fixture data preserved. Interrupted migration recovery is restore-from-verified-pre-upgrade-backup only; do not blindly rerun or use `Down()` as a data-preserving rollback.
- PR #17 was advanced to `da31427d47bc86994f4e3e800900101383ec81d1` by this squash merge. This is a historical merge checkpoint, not a hard-coded current tip; read the live PR #17 branch head before every exact-SHA operator run. **Next gate:** clean exact-HEAD Windows regression on this integrated parent commit: tenant scope, disposable PostgreSQL migration/constraints/backup/restore, and the historic SQLite upgrade/recovery harness. Preserve independent manifests, receipts and cleanup evidence before parent acceptance.
- M2 claim-decision mutations, real evidence intake and production activation remain separate gates; legal/privacy Issue #24 remains OPEN/BLOCKING for real evidence. No CT164/CT152, live DB, PVE deployment, HTTP listener or GitHub Actions.

## 2026-11-14 13:55 Europe/Sofia — F2b integrated exact-source/build + operator postflight PASS (SOURCE GATE ACCEPTED)

**Tested integrated source SHA:** `113c05ff0a171cc3b6d456af7d9ca6b3224e3bca` (reconciled F2b public Identity with F2a reviewer/PostgreSQL QA). Operator used clean detached worktree, matched origin branch, and supplied Windows output:
- `F2B_EXACT_SOURCE=PASS`, `F2B_STATIC_SECURITY_INVARIANTS=PASS`.
- All five source build stages PASS: .NET API Release restore/build, Angular npm ci, typecheck, production build.
- `F2B_PUBLIC_IDENTITY_SOURCE_ONLY=PASS`.
- Subsequent separate independent operator postflight `F2B_INTEGRATED_SOURCE_POSTFLIGHT=PASS`: manifest exact SHA and mandatory PASS/NOT_TESTED/HOLD lines, 5×3 command/stdout/stderr receipts, no failure gate, both `PublicAccounts:Enabled=false` and `ClaimReviewPostgreSqlQa:Enabled=false`, clean worktree.
- **Evidence:** `D:\GitHub\YangSpaceApp-F2b-Integrated-QA-20261114\logs\yangspace\f2b-public-identity\113c05ff0a17-20261114-135340-556e84a0`.
- **Manifest SHA-256:** `5d156889d1e9f54e0923d880427f2d777fe2da244b3c417eb7f8bced87061700`. Logs and manifest bytes remain operator-local; this records verified operator transcript, not direct independent download of local evidence.

**Accepted scope = integrated F2b SOURCE GATE ONLY.** It is 100% for the bounded static security/build/typecheck gate at the tested SHA. The doc-only follow-up to record this milestone must **not** be represented as a new application-code QA run.

**Explicit open blockers:** `PUBLIC_IDENTITY_HTTP=NOT_TESTED`, `PUBLIC_SMTP_DELIVERY=NOT_TESTED`, `OWNER_GRANT=NOT_IMPLEMENTED_NOT_AUTHORIZED`, `CT164_DB=NOT_ACCESSED`, `RELEASE_GATE=HOLD`. Additional P0: PostgreSQL TestServer with non-superuser connection/TLS VerifyFull and reviewed migrations, realistic confirmation/lockout/recovery/role/anti-enumeration matrix, injectable fake email/outbox delivery failure + retry policy, browser privacy/accessibility and production ingress/backups. Issue #24 legal/privacy blocks real business evidence. PR #29 and #17 remain OPEN/DRAFT/HOLD; #1 stays Draft. No runtime deploy, email sends, real registrations or production DB changes.

**Next actionable work:** add scoped disposable F2b PostgreSQL Identity HTTP/email contract and QA runner on PR #29 in a separate code commit, then run new exact-SHA Windows acceptance and operator postflight. Keep default-OFF throughout.

---

## 2026-11-14 — F2b/F2a integration candidate (NOT TESTED)

F2b Identity source from `8846a71ef40e142ac1d00d5e337b4b19bcea314a` reconciled with accepted F2a parent `1f1307250eed0d2e5df24570eb560e5ea16e20ee` (PR #27 QA included). PublicAccounts and ClaimReviewPostgreSqlQa remain disabled by default. Source/build PASS on historic F2b SHA `3ac9df42023ac36bf0fe970d9cf353bc501dba3d` and F2a parent regression PASS `1f1307250eed0d2e5df24570eb560e5ea16e20ee` must not be relabeled as testing this integration. Full exact-head build and subsequent isolated PostgreSQL Identity HTTP/mail QA pending. PR #29 stays DRAFT/HOLD. No CT164, real registrations, claim decisions or production permissions changed.

## 2026-11-13 — F2b exact-SHA Windows operator source QA PASS

**Exact source SHA:** `3ac9df42023ac36bf0fe970d9cf353bc501dba3d`. **Result: SOURCE BUILD/TYPECHECK PASS, NOT HTTP/SMTP/PRODUCTION ACCEPTANCE.**

Operator transcript (17:52 local):
- `F2B_EXACT_SOURCE=PASS` and `F2B_STATIC_SECURITY_INVARIANTS=PASS`.
- `STAGE_PASS=f2b-api-restore`, `STAGE_PASS=f2b-api-build` (Release).
- `STAGE_PASS=f2b-web-npm-ci`, `STAGE_PASS=f2b-web-typecheck`, `STAGE_PASS=f2b-web-build` (Angular production).
- `F2B_PUBLIC_IDENTITY_SOURCE_ONLY=PASS` and independent `F2B_SOURCE_POSTFLIGHT=PASS` (manifest markers and no failure gate).

**Operator-local evidence:** `logs/yangspace/f2b-public-identity/3ac9df42023a-20261113-175209-a7bdefdb`.  
**Manifest SHA-256:** `829E2B8A9BF07FB22FCA9368F89F30D86AD214ED614FFD601F3D5F35062D5122`.

**Independent evidence limit:** Manifest SHA and stage PASS were provided in the operator console transcript; the file bytes/logs remain operator-local and were not independently fetched by the GitHub connector. This PASS is valid for the tested source SHA; a documentation-only follow-up commit must not relabel it as a new runtime test.

**Next required release gates:** disposable PostgreSQL TestServer with fake email transport and a full registration/confirmation/login/reset/role/lockout/anti-enumeration matrix; backend mail delivery failure/outbox durability; approved privacy notice and lawful basis, browser/mobile/ingress, off-host restore and CT164 deployment review.

**PR #29: OPEN/DRAFT/HOLD**, source compile gate achieved; do not merge until all scoped DoD accepted. `PublicAccounts:Enabled=false` remains default, no production deployment. Issue #24 separately blocks real merchant evidence.


## 2026-11-13 — F2b source QA preflight failure and corrected marker

**Operator-tested source:** `f8f71044bb38b93d63574984f3692fc9276076aa`; `F2B_EXACT_SOURCE=PASS`, then **`F2B_STATIC_POLICY_MISSING=users.GetRolesAsync(user)`** before any .NET restore/build, npm CI, Angular typecheck or production build. **No build PASS and no live operation.**

**Root cause:** static QA string literal used `users.GetRolesAsync(user)`, while the actual guarded function `IsStaffAsync(UserManager<IdentityUser> manager, IdentityUser user)` correctly invokes `manager.GetRolesAsync(user)`. This is a harness-only false-negative, not evidence of a role authorization bypass.

**Fix code commit:** `0f1d4313f11ca45ecdb3c3b634a1951708d36ee0`, one-line source QA marker correction; no API/Angular behavior changed. Independent source comparison after fix confirms **10/10 public-account backend policy markers, 3/3 startup/program gates, 6/6 Angular auth routes, and default OFF**. This verification is static-only and cannot establish successful compilation.

**Next:** rerun `scripts/Test-F2b-Public-Identity-Source-Local.ps1` from a clean detached worktree at the **current exact F2b branch HEAD**, with origin ref parity; require .NET Release build, npm-ci, Angular typecheck and build, manifest and evidence hash. **PR #29 stays OPEN/DRAFT/HOLD**, no CT164, no SMTP, no signup enablement, and no Owner grants.


## 2026-11-13 — F2b public Identity implementation in Draft PR #29

**Branch:** `feat/yangspace-f2b-public-identity-foundation-20261113` stacked on F2a-1 PR #17. **Runtime code SHA:** `c0dca5074733cbfab8158f8e78d9a5ddbd188308`.  
**[PR #29](https://github.com/jvalkovv/YangSpaceApp/pull/29): OPEN / DRAFT / SOURCE NOT WINDOWS-TESTED / HOLD.** [Issue #28](https://github.com/jvalkovv/YangSpaceApp/issues/28): OPEN/P0.

- API: dedicated `/api/public-account` registration, canonical email confirmation/resend, confirmed-only login, password reset and bearer session. Reuses ASP.NET Core Identity and existing EF user store; no Microsoft Entra, no new migration.
- Security: default-off flag, Production+PostgreSQL+explicit host+canonical origin+STARTTLS SMTP preflight, rate-limit, memory-only bearer token, generic duplicate signup outcome, prior password validation, deny public login/reset/confirm for **any** Identity staff role, no Owner/membership grants, no bypass of admin `/api/auth` gateway.
- Angular: responsive `/login`, `/register`, `/recover`, `/reset`, `/resend-confirmation`, `/account` with keyboard-aware forms, complete mobile dropdown auth navigation and error/disabled messages.
- Evidence scaffolding: `scripts/Test-F2b-Public-Identity-Source-Local.ps1` exact-SHA Windows .NET Release build + Angular typecheck/production build; `docs/F2B_PUBLIC_ACCOUNTS_FOUNDATION_20261113.md`.
- **No QA PASS yet.** Pending Windows exact-SHA compile/typecheck, disposable PostgreSQL TestServer/SMTP sink integration, privacy notice and lawful-basis approval, mail retries/outbox decision, ingress/real-browser/CT164 recovery acceptance. No production flag change, no real registrations and no deployment. Issue #24 remains blocking for real business evidence and claim decisions.


## 2026-11-14 — F2a-3 QA scoped merge accepted

PR #27 MERGED/CLOSED into F2a parent at `1b59aa94cee410212df954223693153faa1c7d1d`. Exact tested source `ede8a3f2064b8f93cdc2e1757278890574ea4458` passed disposable PostgreSQL real bearer/reviewer role revocation and synchronized competing EF writers, reported independent postflight PASS; manifest SHA `A278A5C4A31655F305F16384DBF4607A0352303BFC0A6C7287CB6FC24D0C0EAA`. Logs remain operator-local. This acceptance is synthetic QA only, NOT current-parent runtime test, production claim decision/Owner grant, legal approval or CT164 deployment. PR #17 and #29 stay DRAFT/HOLD; Issue #24 blocks real evidence.

## 2026-11-14 — Owner-approved strategy and reviewed growth expansion (DOCS ONLY)

**Scope:** Six base decisions are owner-approved; additional resident/merchant ideas reviewed and prioritized as candidates, NOT implementation/release authorization. Product plan: [YangSpace growth and pilot direction](PRODUCT_GROWTH_PILOT_20261113.md). This new documentation is based on the active YangSpace V2 feature branch; legacy-main Draft PR #30 is a historical source proposal and must NOT be merged directly into main.

**Positioning:** verified hyperlocal discovery plus measurable, privacy-safe customer actions, rather than a phone directory or checkout marketplace. **2026-11-14 Bulgarian-market clarification:** international design examples are not proof of direct local competition; the presence of Google Business Profile in Bulgaria and partial local offer/brochure alternatives is not proof that YangSpace cannot acquire users. Neither demand nor willingness-to-pay is established by comparison alone: validate Varna resident engagement, active local supply, repeat merchant publishing and acquisition/support economics during the pilot. P0: verified listings/owner controls, freshness/publishing, anonymous search, basic contact/route actions, SEO. P1: map, QR, optional favorites, scoped inquiries, aggregated business analytics. P2: events, opted-in notifications, clearly labeled sponsored posts and area/category campaigns. EUR 1-5 merchant subscription is an unvalidated pricing hypothesis; no billing/checkout.

**Execution:** M0 strategy/ADRs; M1 #27 final source review -> #17 parent reconciliation -> #29 full Identity HTTP and SMTP evidence; M2 claim decision/Owner lifecycle and Issue #24 legal; M3 business CMS; M4 geo + discovery/SEO + action counts; M5 CT164 recovery and protected release; M6 Varna pilot 20-30 then 100; M7 growth validation. Engineering gates keep historical names G0-G4/G-PREVIEW/G-PILOT (no duplicate G1/G2/G3 definitions).

**Exact code-state boundary:** Parent V2 HEAD at branch creation was `92f248dc88fcc888f93c2829727e22db0eb65312`; F2a parent #17 remains Draft at `e1a05c30fb2ff8ff967d5e028229b6d2bb8898d5`; #27 Draft tested runtime `ede8a3f2064b8f93cdc2e1757278890574ea4458` (postflight PASS), #29 Draft tested build `3ac9df42023ac36bf0fe970d9cf353bc501dba3d` (HTTP/mail NOT TESTED). No claim mutation, live registration, release or legal/privacy clearance is inferred. #1/#17/#27/#29 still Draft/HOLD, #24 still OPEN.

**Policy:** one modular monolith + PostgreSQL target, manual clean exact-SHA QA/no GitHub Actions, CT164 only permanent app runtime, off-host recovery/observability. Session handling is ADR-pending; source F2b uses memory-only bearer. Privacy/DSA applicability and marketing consent require review before additional lead, analytics and promotional features.

## 2026-11-13 — Public registration gap / ASP.NET Core Identity clarification

- **Architecture:** ASP.NET Core Identity in the .NET API is the credential, role and bearer-token implementation. There is **no Microsoft Entra ID / Microsoft social login integration** in the current V2 implementation.
- **Frontend gap:** Angular routes currently include `/`, `/obekt/:slug`, `/admin`. Public `/register`/`/login` views and calls-to-action are **NOT IMPLEMENTED** in this reviewed source. A live deployed frontend inspection was not possible here; do not equate GitHub branch source with the public server's exact build.
- **Backend boundary:** `Preview:AllowRegistration=false` by default; self-registration is explicitly rejected in Production CMS mode; reviewer QA endpoints use isolated bearer auth but do **not** enable production registrations.
- **Backlog:** [Issue #28 — Public Identity signup/login UI and guarded business onboarding](https://github.com/jvalkovv/YangSpaceApp/issues/28). Distinguish ordinary verified user signup from merchant claim review and Owner membership; neither public account creation nor Owner auto-grants are authorized by PR #27.
- **Release note:** public read-only directory may remain anonymous. User signup and merchant onboarding are explicit product/release gates; no enabling without verified-email/anti-abuse, privacy, operational and recovery acceptance. Evidence/claim decision remain blocked by [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24).


## 2026-11-13 — F2a-3 exact-SHA Windows acceptance: PASS (synthetic-only)

Operator ran the dedicated `scripts/Test-F2a3-PostgreSql-Bearer-Concurrency-Local.ps1` on clean detached worktree at exact SHA `ede8a3f2064b8f93cdc2e1757278890574ea4458`. The operator transcript reports:

- Git exact source, local Docker image identity, .NET 10 restore/Release publish, EF migration model diff and pinned reviewed PostgreSQL SQL hash: **PASS**.
- PostgreSQL 17 `--network none`, read-only .NET sidecar, actual mount flags, native Linux SQLite SHA-256 and `appsettings.json` transfer verification: **PASS**.
- `STAGE_PASS=pgapp-tenant-scope-regression`: **PASS**.
- `STAGE_PASS=pgapp-real-ef-transaction`: **PASS**; the script requires all transaction, parallel-writer, one-winner, Owner membership and loser-rollback markers before advancing.
- `STAGE_PASS=pgapp-reviewer-identity-http`: **PASS**; the runner requires real PostgreSQL-backed Identity bearer HTTP, WAF metadata/content-root, same-token reviewer role revocation, 401/403 and no business side effects.
- `F2A3_PG_BEARER_CONCURRENCY_ISOLATED_ONLY=PASS` and independent operator postflight `F2A3_INDEPENDENT_POSTFLIGHT=PASS`: required manifest markers, no `gate.txt`, and no leftover matching Docker QA containers.

**Operator-local evidence:** `logs/yangspace/f2a3-pg-bearer-concurrency/ede8a3f2064b-20261113-143712-f50743ec`  
**Manifest SHA-256:** `A278A5C4A31655F305F16384DBF4607A0352303BFC0A6C7287CB6FC24D0C0EAA`

This is acceptance of the **bounded synthetic-only QA on the exact tested source SHA**. Full stage/evidence bytes reside on the operator workstation; only console transcript and manifest hash are available in this GitHub checkpoint. Do not claim independent download/hash verification.

**NOT release authorization:** no public registration/onboarding UI, production claim decision endpoint, real evidence intake, production storage, PostgreSQL least-privilege app role, uncertain-COMMIT/idempotency/fault injection, or off-host restore acceptance. CT164 and CT152 untouched by this QA. Legal/privacy Issue #24 remains OPEN for real business evidence.

Child PR #27 is mergeable/clean but remains **OPEN/DRAFT** pending final scoped code review and merge decision. Parent PR #17 and V2 PR #1 remain OPEN/DRAFT/HOLD. No deployment, no GitHub Actions.


## 2026-11-13 — F2a-3 PR #27 second Windows QA checkpoint (PG PASS / HTTP HOLD)

- Tested source `fa481beb2b589e2392094bb0ddad695a62fc3d3c`: exact source, Release publish, reviewed PostgreSQL migration SQL, isolated Docker server/sidecar, `/qa` exec mount, Linux SQLite hash, tenant scope and Npgsql application transaction/concurrent writer stages **PASS**. The runner validates all competing-writer one-winner/loser-rollback markers before recording stage PASS.
- PostgreSQL reviewer Identity HTTP stage **FAIL `EXIT=134`**. Root cause: ASP.NET Core 10 `WebApplicationFactory` tried to locate a source `.sln` under `/qa`, because it sets content root before the existing `WithWebHostBuilder` override. The printed `libgssapi_krb5.so.2` warning was Npgsql's nonfatal fallback, not the crash.
- New source correction: `WebApplicationFactoryContentRoot` assembly metadata for `/qa/appsettings.json`, deterministic `GetTestAssemblies()`, metadata marker preflight, QA-only `GSS Encryption Mode=Disable`. **Fix NOT WINDOWS-TESTED yet.**
- Preserve operator stage logs as historical exact-SHA evidence. Keep [child PR #27](https://github.com/jvalkovv/YangSpaceApp/pull/27), [parent PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) **OPEN/DRAFT/HOLD**. No production claim mutation, real evidence, CT164/CT152, GitHub Actions or deployment.
- Next: clean detached exact-current-HEAD rerun, require full PostgreSQL Identity bearer HTTP matrix, independent manifest, no failure gate and verified Docker cleanup. See [F2A3 dedicated QA contract](F2A3_POSTGRESQL_BEARER_CONCURRENCY_QA_20261113.md).


## 2026-11-13 — F2a-3 PostgreSQL Bearer/Concurrency operator failure and scoped fix (DRAFT/HOLD)

- Child [PR #27](https://github.com/jvalkovv/YangSpaceApp/pull/27) failed Windows exact-SHA runtime QA at `560e027b53d8124710f574eff1e809bcf2ad7406`: after source/EF build/reviewed SQL and disposable PG setup PASS, `pgapp-tenant-scope-regression` exited 134. The Linux native SQLite library at `/tmp/qa/runtimes/linux-x64/native/libe_sqlite3.so` failed executable memory mapping; likely `noexec` tmpfs. No transaction/concurrent-writer or PostgreSQL Identity HTTP QA was executed. Cleanup postflight must be checked separately.
- Child-source correction: binary-safe streamed runtime relocated to dedicated `/qa:rw,exec,nosuid,nodev` tmpfs; `/tmp:rw,noexec,nosuid,nodev` retained. Runner checks actual `/proc/mounts`, read-only rootfs/no binds/no ports and native `libe_sqlite3.so` SHA-256 before running the unchanged tenant, PG transaction/concurrent-writers and Identity bearer HTTP gates.
- **State:** source fix committed, exact-new-SHA Windows QA **NOT RUN**. No retroactive PASS and no parent acceptance. Full incident and runner contract: [F2A3_POSTGRESQL_BEARER_CONCURRENCY_QA_20261113.md](F2A3_POSTGRESQL_BEARER_CONCURRENCY_QA_20261113.md).
- Keep [PR #27](https://github.com/jvalkovv/YangSpaceApp/pull/27) and parent [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) **OPEN/DRAFT/HOLD**. No CT164/CT152, production DB/evidence, claim mutation route or deployment; legal/privacy [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains blocking for real evidence.


# YangSpace 2.0 — Single Source of Truth (SSOT)

**Status:** ACTIVE planning/status authority on the YangSpace V2 feature branch; introduced by merged PR #9. This document does **not** authorize deployment, database mutation, exposure, or feature-to-`main` merge.  
**Baseline date:** 2026-11-11 (Europe/Sofia).  
**Previous accepted F2a-2 code QA:** tested code SHA `f579c70db9cc0986d2f1df8047158587f0e87eef` (Windows restore/build/tenant regression/real Identity bearer TestServer PASS; operator evidence postflight PASS), documentation-only follow-up `bc0a77ea6b6bc794a2d9d6aefcbe3c12936a329e`, squash-merged by PR #19 as `17d08530db7fed0bbe7ed4444577a7b0e6b73c4d` into the F2a-1 feature branch. This is **not merged into the V2 feature branch or `main`**. Earlier F1b source QA remains historical and valid for its own SHA. Historical CT164/CT152 deployment remains `a4802805421f10e834ad35bbf2f259db4e4eab84`. [Parent PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) stays Draft. `main` remains the legacy application.  
**Scope of this update:** GitHub branch/document reconciliation and operator-provided exact-SHA Windows QA + read-only evidence postflight; the V2/F2a merge commit and documentation updates introduced no runtime source changes. No new build, PostgreSQL runtime, real-browser, PVE, or public-origin execution was performed for this document.

## 2026-11-13 — F2a-1 synchronized with current V2 base

**Integration merge commit:** `eb75e0022634499de489578f6f1003be2706a1f1` (F2a-1 first parent `93809f4f8c6cab18d261123195ec518df4ca9ba5`; current V2 second parent `92f248dc88fcc888f93c2829727e22db0eb65312`). The merge tree is unchanged from the F2a-1 pre-integration tree; application, EF migration, SQL, test and runner source were not altered by the merge.

GitHub compare now reports **83 commits ahead / 0 behind**, with merge base equal to V2 HEAD. PR #17 reports **mergeable=true / clean**, but remains **OPEN / DRAFT / HOLD**; it has not been merged into the V2 branch or `main`.

The four documentation conflicts were reconciled without discarding either side's accepted history. The F2a security contract now reflects owner-approved technical and evidence/retention policies while preserving Issue #24 as the independent legal/privacy blocker. The previous dirty-mergeability observation in the 07:53 checkpoint is historical and resolved by this integration commit.

**Next operator gate:** run the exact current PR #17 head from a clean detached Windows worktree using `Test-F2a-Tenant-Scope-Local.ps1` and `Test-F2a-PostgreSql-Disposable-Local.ps1`. These F2a-1 runners require the remote branch ref to equal the tested SHA. Historical PR #22/#23/#26 child PASS results remain attached to their own exact tested SHAs and are not relabelled as tests of this integration head.

> **Authoritative summary:** strong design and functional *preview foundation*; real multi-business/owner workflows, public-pilot content governance, SEO, ingress protection, observability and off-host recovery are not accepted. **G0, G1, G2, G3, G4, G-PREVIEW and G-PILOT remain HOLD at this reviewed source baseline.** Do not infer percentages or production readiness from code, screenshots, HTTP 200 or historical PASS markers. F2a-3 reviewer/ownership/concurrency policy and the synthetic-only evidence policy foundation are owner-approved and accepted in their bounded scopes; real evidence intake/storage/retention operations and all real claim mutations remain HOLD.

## 2026-11-13 07:53 Europe/Sofia — F2a-3 synthetic evidence policy accepted

**[PR #26](https://github.com/jvalkovv/YangSpaceApp/pull/26) MERGED/CLOSED** by squash commit `7114427be24f0d50a682f1b67834336473c0f594` into the F2a-1 feature branch, not V2 or `main`.

- Exact Windows-tested source SHA: `519809875658e7e27232d6a49536f2d71933ea69`.
- Native PowerShell runner syntax/source identity, .NET 10 restore, Release build with warnings-as-errors, synthetic runtime contract and independent evidence postflight: **PASS**.
- Operator markers: `STAGE_PASS=f2a3-evidence-restore`, `STAGE_PASS=f2a3-evidence-build`, `STAGE_PASS=f2a3-evidence-policy`, `F2A3_EVIDENCE_POLICY_SYNTHETIC_ONLY=PASS`, `F2A3_EVIDENCE_POSTFLIGHT=PASS`.
- Manifest SHA-256: `e53fd7ec1e2de2acec19d90f174bc211eb156b938b7a744e56ce1018010142ec`.
- Operator-local evidence: `logs/yangspace/f2a3-evidence-policy/519809875658-20261113-072609-f43b3a39`. Full logs and `postflight.txt` remain operator-local; no claim of cross-host artifact-byte verification.
- Accepted scope: pure PDF/JPEG/PNG validation contract, deterministic retention calculations, UTC-anchor rejection, fail-closed scanner guard and test-assembly-only synthetic in-memory store.
- Source review: five changed files only; no HTTP intake/download route, EF entity/migration, API-host storage registration, production storage, claim mutation or deployment path.
- Explicit limitations: magic-byte checks are not full format parsing; PDF `/Encrypt` detection is heuristic; no scanner implementation, deletion worker, legal-hold processor or backup tombstone replay exists. These are future gates before any real intake.
- [Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25) is **CLOSED — completed for this bounded synthetic-only DoD**. [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains **OPEN/BLOCKING** for legal/privacy clearance.
- Parent [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) remains OPEN/DRAFT/HOLD; V2 [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) remains OPEN/DRAFT/HOLD.
- **Historical PR #17 mergeability RCA (resolved):** GitHub reported `mergeable_state=dirty` before the branch synchronization. The four overlapping governance/security documents were reconciled by integration commit `eb75e0022634499de489578f6f1003be2706a1f1`; the current compare is 83 ahead / 0 behind and PR #17 is mergeable=true / clean. The parent PR remains Draft/HOLD for technical acceptance, not documentation conflicts.
- No CT164/CT152, live DB, real evidence, GitHub Actions or production deployment.

## Historical checkpoint — 2026-11-12 F2a-3 technical policy and reviewer authorization child PR #23

Owner approved the technical reviewer/ownership/concurrency policy recorded in [F2a-3 security contract](F2A3_CLAIM_REVIEW_SECURITY_CONTRACT_20261112.md) and [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20). **This is not approval of evidence categories, evidence checklist, private evidence handling, retention/erasure or legal basis**; those remain a mandatory separate gate before any real claim decision mutation.

[Draft PR #23](https://github.com/jvalkovv/YangSpaceApp/pull/23) is stacked on [F2a-1 PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) at parent source SHA `7f3e8d5b37f591c3c0344996b850ad23a3155b0a`. Its bounded scope is dedicated `MerchantClaimReviewer` bearer authorization, persistent live-role check (same-token revocation), self-review/active-member conflict policy and disposable SQLite Identity TestServer tests. A minimal QA access probe is the only new route; claim decision POST, ownership changes, Owner grants, evidence storage, audit writes and migrations are not included.

**Historical status at that checkpoint: DRAFT / SOURCE-ONLY / OPERATOR WINDOWS QA NOT RUN / HOLD.** Next: fetch exact PR #23 head into a clean detached Windows worktree; native PowerShell parser, .NET restore, Release build with warnings-as-errors, exact-SHA HTTP contract runner, then independent manifest/stage/evidence postflight and scoped review. The test is SQLite bearer HTTP, not PostgreSQL bearer HTTP. PR #22's already accepted disposable PostgreSQL transaction QA must not be repeated for this authorization-only change.

Historical PR inventory at that checkpoint: #23 OPEN/DRAFT/HOLD; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD. PRs #19/#21/#22 remain MERGED/CLOSED. No GitHub Actions, CT164/CT152, live DB, public merchant activation or deployment. Continue reporting exact SHA, evidence path, PASS/PARTIAL/HOLD, blockers and next action at each meaningful checkpoint; close only a scoped PR with 100% accepted DoD.

## 2026-11-12 18:23 Europe/Sofia — PR #22 real PostgreSQL EF transaction QA MERGED/CLOSED

**[PR #22](https://github.com/jvalkovv/YangSpaceApp/pull/22) MERGED/CLOSED via squash `4bf7d92f5e31988d6f5490f9288c9488ac4a65f3` into the F2a-1 feature branch ([Draft PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17)), NOT V2 or `main`.** Scoped source code and operator evidence acceptance complete; PR #17 now contains the test-only child but remains OPEN/DRAFT/HOLD, as does V2 PR #1.

- **Exact Windows-tested QA source SHA** `32bbe66e8934fd99bcdc4093ec5eeafa955d68fe`. Native PowerShell syntax for both scripts, .NET Release restore/build/publish, EF model diff and pinned reviewed SQL SHA, isolated disposable PG17 migration and private network, read-only sidecar with tmpfs, streamed runtime TAR/verified DLL digest, real Npgsql EF *synthetic* multi-entity transaction and PostgreSQL second-SaveChanges unique audit failure with full outer rollback all PASS.
- **Operator postflight** `F2A3_PG_EVIDENCE_POSTFLIGHT=PASS`: manifest, 17 stage triplets, nine PostgreSQL transaction markers, SQL/TAR/DLL SHA checks and no remaining local QA containers. Operator evidence dir `D:\GitHub\YangSpaceApp-F2a3-PG-AppTx-QA-R3\logs\yangspace\f2a3-pg-apptx\32bbe66e8934-20261112-181451-778ff609`. Local artifacts have not been uploaded for cross-host byte verification.
- **SHA-256 receipts:** reviewed SQL `2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7`; TAR `0e6829ffd9ba00b59e09e2f4bfe477da4d7445fac886ef79d6f0b3587b004c0e`; DLL `1d358f9137b190d8ff63a59d1f5fc7bf6221710281021dc19b98d8461902246d`; manifest `91b379b9adf6da8a4cbecfb38728d1b75bb4abc393cb61f9cdacf2a3acfe0954`; transaction stdout `d9346f2fc01b8a19f571a06c4b111b1d4d8dc5e3931bfec6636652984bb1286a`. A docs-only evidence commit `abb727ad0e959aaace02b7e99aa23e0905332d4a` followed tested code: one Markdown file only, no test/SQL change.
- **Only accepted scope:** real PostgreSQL **synthetic EF transaction feasibility and rollback** in isolated test containers. Still **NOT ACCEPTED:** actual claim-review Identity bearer HTTP, dedicated reviewer role/revocation, self-approval/evidence/ownership-transfer policy, parallel application writers and claim races, uncertain COMMIT/idempotency, least-privilege production DB role, off-host restore and site/pilot release.
- **Current PR hygiene:** #19 and #21 and now #22 MERGED/CLOSED with scoped 100%-acceptance; #17 OPEN/DRAFT/HOLD, #1 OPEN/DRAFT/HOLD. [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) remains OPEN and PROPOSED, requiring security policy sign-off before implementation of actual mutation routes. No Actions, CT164/CT152/live DB writes, merchant activation or production deployment.
- Standing progress reporting / exact-SHA evidence / closure-only-at-100% governance remains mandatory.

## 2026-11-12 18:14 Europe/Sofia — PR #22 R3 real Npgsql app transaction runtime PASS

- [Draft PR #22](https://github.com/jvalkovv/YangSpaceApp/pull/22), exact tested/current SHA `32bbe66e8934fd99bcdc4093ec5eeafa955d68fe`: operator source/worktree identity, native PowerShell parser of main + TAR helper, preflight no-old-QA-containers, local Docker image/context, EF tool/restore/Release build/model-diff/pinned SQL hash, .NET publish, disposable PG17 migration/sentinel, two isolated containers, binary tar stream to read-only `/tmp` sidecar, verified transferred DLL SHA-256, and **real Npgsql EF transaction contract** all `PASS`. Final marker `F2A3_PG_APP_TRANSACTION_ISOLATED_ONLY=PASS`.
- Evidence dir: `D:\GitHub\YangSpaceApp-F2a3-PG-AppTx-QA-R3\logs\yangspace\f2a3-pg-apptx\32bbe66e8934-20261112-181451-778ff609`. The runner completed cleanup before printing final PASS; **independent postflight is still pending:** check manifest, every runner/stdout/stderr stage file, 9 transaction markers, reviewed SQL/TAR/DLL SHA-256 and absence of matching temporary QA containers. Retain prior R2 failing evidence; do not rerun passed stages merely for docs changes.
- **Accepted current boundary: runtime operator PASS, not yet scoped PR acceptance.** This is synthetic disposable Npgsql transaction feasibility (initial Owner/audit persistence and full outer rollback after deliberately failed second SaveChanges), NOT reviewer Identity bearer HTTP, concurrent writers, unknown COMMIT/idempotency, least-privilege DB role, real data or public claim writes.
- PR state: #22 OPEN/DRAFT/HOLD until operator postflight and scoped review; #17 and #1 OPEN/DRAFT/HOLD; #19/#21 MERGED/CLOSED. Issue #20 policy PROPOSED/NOT APPROVED. No GitHub Actions, CT164/CT152, live DB, merchant activation or deployment. Standing requirement for progress checkpoints and 100%-scoped PR closure unchanged.

## 2026-11-12 17:43 Europe/Sofia — PR #22 R2 real PostgreSQL QA PARTIAL; read-only runtime transfer fixed in R3 candidate

- **Operator R2 exact SHA `3fd89d55b337590452974544967f13a017375ed5`:** native PowerShell parser, Git/worktree identity, local ASP.NET 10 runtime image, EF restore/build/model diff, pinned reviewed SQL generation, .NET runtime publish, disposable PostgreSQL17 server/DB/migration/sentinel and two isolated containers **PASS**. First failure at `STAGE_FAILED=pgapp-copy-runtime EXIT=1` with Docker `container rootfs is marked read-only`.
- **RCA:** Docker daemon `docker cp` refuses transfer into a running container using `--read-only`, even when `/tmp` is writable tmpfs. **Real Npgsql EF transaction was never executed.** The `io: read/write on closed pipe` message belongs to the failed Docker tar copy. Operator evidence path: `D:\GitHub\YangSpaceApp-F2a3-PG-AppTx-QA-R2\logs\yangspace\f2a3-pg-apptx\3fd89d55b337-20261112-174329-33a19746` (FAIL/PARTIAL; not an accepted success manifest). Container cleanup must be checked before next run.
- **Corrected source candidate:** [Draft PR #22](https://github.com/jvalkovv/YangSpaceApp/pull/22) exact `32bbe66e8934fd99bcdc4093ec5eeafa955d68fe`. Changes remain QA-only: new binary-stream helper `Invoke-YangSpaceTarStreamStage.ps1`, adjusted local Windows runner with host tar packaging and SHA-256 verified extraction into `/tmp` via `docker exec -i`, updated proof-boundary doc. Runtime container keeps `--read-only`, `/tmp` tmpfs and `--network container:<pg>`; no host binds/published ports or production source changes.
- **Next gate:** operator R3 must use clean exact SHA in a new detached worktree, check both native PowerShell parser results and no leftover local `yangspace-f2a3-` containers; then run reviewed-migration/Npgsql app transaction/second-save audit-failure rollback tests and evidence/cleanup postflight. **No PASS on R3 yet**; do not transfer results from R2 to new SHA.
- **PR policy:** #22 OPEN/DRAFT/HOLD; #17 and #1 OPEN/DRAFT/HOLD; #19/#21 MERGED/CLOSED. Reviewer/claim security policy in Issue #20 still PROPOSED; real Identity reviewer HTTP, parallel application writers, uncertain COMMIT and production remain unaccepted. No GitHub Actions, CT164/CT152 or live database mutations. Report progress at source/test/review/merge checkpoints and close only scoped 100%-accepted PRs.

## 2026-11-12 17:02 Europe/Sofia — PR #22 PowerShell parser failure and exact-SHA fix

- [Draft PR #22](https://github.com/jvalkovv/YangSpaceApp/pull/22) failed first operator Windows preflight on SHA `3b65d0ce0177f90359801e664f6883aa3211ad76`: `F2A3_PG_WORKTREE=PASS` then **`F2A3_PG_RUNNER_SYNTAX_FAILED`**, prior to all `.NET`, Docker and PostgreSQL stages. No runtime PASS is claimed.
- **Root cause:** source patch corrupted `AssertContainerIsolation` (unterminated regex) and appended a second copy of the script after its final output. **Fixed in the same PR #22** at new exact HEAD `3fd89d55b337590452974544967f13a017375ed5`. GitHub refetch: 305 lines, one isolation function and one cleanup body; compare: **one changed PowerShell runner file** (+3/−196 lines) vs failed SHA. Lightweight delimiter check balanced, not a substitute for native Windows parser.
- **Next exact gate:** fetch newest SHA into a fresh clean detached R2 worktree; native `[System.Management.Automation.Language.Parser]::ParseFile` PASS required *before* running `Test-F2a3-PostgreSql-AppTx-Local.ps1`. If parser passes, execute isolated real Npgsql disposable PostgreSQL transaction QA and inspect evidence/cleanup. No need to repeat already accepted historical SQLite/F2a schema/restore QA.
- **PR status:** #22 OPEN/DRAFT/HOLD (Windows parser and all runtime gates not yet passed); #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD; #19 and #21 MERGED/CLOSED. Issue #20 reviewer policy remains PROPOSED. No GitHub Actions, CT164/CT152, production DB, merchant activation or deployment. Continue visible progress at each checkpoint and close only 100%-accepted scoped PRs.

## 2026-11-12 16:22 Europe/Sofia — F2a-3 real Npgsql app transaction test slice in progress

- **Next scoped child:** [Draft PR #22 — disposable real PostgreSQL EF app transaction QA](https://github.com/jvalkovv/YangSpaceApp/pull/22), exact source-only candidate `3b65d0ce0177f90359801e664f6883aa3211ad76`, stacked on [Draft PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) base `615fa2f7b597a9cdcd4f0163a26b679e67b8c96d`. Four changed test/docs files, **NO production API route/config/EF model/migration mutation**.
- Builds on [merged PR #21](https://github.com/jvalkovv/YangSpaceApp/pull/21), which accepted only disposable SQLite claim transaction QA. New test targets **actual Npgsql/PG17** (synthetic claim/owner+membership+4 audit commit, injected second-SaveChanges PG unique failure, full outer transaction rollback). Test process requires exact loopback-only database, per-run sentinel and SHA-256 pinned migration SQL.
- **Local-only topology:** Docker PG17 `--network none --rm --tmpfs`, second temporary .NET 10 ASP.NET runtime container `--network container:<pg> --read-only --tmpfs`; no published ports, no host mounts; explicit local named-pipe Docker context and image-ID evidence; mandatory two-container cleanup before PASS. This is disposable operator QA, not a Docker deployment requirement.
- **Current acceptance:** SOURCE-ONLY / NOT BUILD-TESTED / NOT RUNTIME-EXECUTED. Need operator exact-SHA parser/restore/publish/PG apply/real EF transaction markers, evidence hashes and cleanup postflight. **No new PR accepted for closure**: #22 OPEN/DRAFT/HOLD; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD; previous #21 and #19 MERGED/CLOSED.
- **Unapproved and out of scope:** Issue [#20](https://github.com/jvalkovv/YangSpaceApp/issues/20) reviewer role, claimant evidence, self-approval conflict/ownership transfer/re-grant, real reviewer Identity bearer HTTP, parallel approvals, uncertain COMMIT, DB least privilege, production rollout. No GitHub Actions, CT164/CT152 access, live DB, merchant activation or deployment.
- Standing roadmap governance remains mandatory: report progress **during the active conversation** at each source/test/review/merge checkpoint, exact SHA, PASS/PARTIAL/HOLD, remaining blocker and next action. Merge/close only when PR's *own bounded DoD* is 100%-accepted with exact-SHA receipts and owner authorization; no dependence on an hourly watcher.

## 2026-11-12 15:31 Europe/Sofia — F2a-3 isolated transaction QA accepted and PR #21 MERGED

**[PR #21](https://github.com/jvalkovv/YangSpaceApp/pull/21) MERGED/CLOSED at `62ecc089ec245703dc7e428cd4430d64fd12e94d` into F2a-1 child branch, not V2 or `main`.** The parent [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) is now at this merged head and remains **OPEN/DRAFT/HOLD**; V2 [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) remains **OPEN/DRAFT/HOLD**.

- Exact tested source of #21: `91f34f17a52e11dd107645ebb44ea0e578e4fa73` — Windows clean SHA/runner syntax, .NET restore, Release build and isolated `--claim-transaction-only` **PASS**. Operator read-only evidence postflight: `F2A3_MANIFEST=PASS`, `F2A3_STAGE_FILES=PASS`, `F2A3_TRANSACTION_MARKERS=PASS`, `F2A3_EVIDENCE_POSTFLIGHT=PASS`.
- Evidence dir: `D:\GitHub\YangSpaceApp-F2a3-ClaimTx-QA\logs\yangspace\f2a3-claim-tx\91f34f17a52e-20261112-151758-9c434338`; manifest SHA-256 `ED0069DB82C9339D0C98290715C1BA1B655741B5D5D723D8DD5D0152DFFB7442`, transaction stdout SHA-256 `0812D68FC76DD152AE63F7CC6F5F3EBA1D58058A03D06DDD7A7A8258E75127F4`. Full ten-file SHA inventory in [F2a-3 QA report](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-f2a1-domain-foundation-20261111/docs/F2A3_CLAIM_TRANSACTION_PREFLIGHT_20261112.md). Operator-local evidence not uploaded for independent cross-host byte verification.
- Tested runtime/test source did not change after acceptance: one documentation-only follow-up commit `0fad914dbcaad4197fbcbc8efc427e4046fa6198` modified one Markdown file before squash. Scoped static review was completed. **Accepted only:** synthetic EF SQLite transaction across claim+org+previously unclaimed location+Owner membership+4 audit events; an injected duplicate audit insert on the second SaveChanges forced complete outer rollback.
- **Explicit remaining HOLD:** actual reviewer claim approval HTTP route, live Identity reviewer role and revocation, real PostgreSQL transaction under concurrent clients, uncertain-COMMIT idempotency, atomic production application audit, least-privilege PG DB role, off-host recovery/DR and pilot release. [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) security policy is still PROPOSED/NOT APPROVED.
- **Current PR inventory:** #21 MERGED/CLOSED; #19 MERGED/CLOSED; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD. The owner's standing roadmap governance (milestone-by-milestone progress, 100%-scoped evidence-gated PR closure, no Actions) continues to apply. No CT164/CT152, live DB, merchant activation or deployment.

## 2026-11-12 15:18 Europe/Sofia — F2a-3 isolated transaction candidate operator PASS

- [Draft PR #21](https://github.com/jvalkovv/YangSpaceApp/pull/21), exact source `91f34f17a52e11dd107645ebb44ea0e578e4fa73`: operator source/worktree and PowerShell syntax PASS; `STAGE_PASS=f2a3-restore`, `STAGE_PASS=f2a3-build`, `STAGE_PASS=f2a3-transaction`; `F2A3_CLAIM_TX_ISOLATED_ONLY=PASS`. The scoped Windows runner verifies six transaction contract markers before emitting final PASS.
- Evidence dir: `D:\GitHub\YangSpaceApp-F2a3-ClaimTx-QA\logs\yangspace\f2a3-claim-tx\91f34f17a52e-20261112-151758-9c434338`. **Read-only manifest and stage file/hash postflight PENDING; no merge decision yet.**
- Accepted runtime scope is synthetic SQLite EF in-memory transaction success + rollback after deliberately failed second SaveChanges on audit UNIQUE index. This is **not** live reviewer authorization, real PostgreSQL app transaction, concurrent client or actual claim approval.
- Current PR inventory: #21 OPEN/DRAFT/HOLD (evidence postflight pending), #17 OPEN/DRAFT/HOLD (larger F2a security/PG gates), #1 OPEN/DRAFT/HOLD (release/pilot); #19 MERGED/CLOSED. No open PR qualifies for closure until its scoped DoD completes. No Actions, CT164/CT152, production DB, claim activation or deployment.

## 2026-11-12 14:48 Europe/Sofia — next F2a-3 QA slice and PR hygiene

- The required **in-conversation progress at every meaningful source/test/review/merge milestone** and **100%-scoped PR closure only after QA/review/owner approval** are reaffirmed in [MVP roadmap](MVP_ROADMAP.md#2026-11-12-1448-europesofia--execution-governance-reaffirmed-visible-progress-and-pr-closure). Monitoring is optional, not a substitute for stage reports.
- [Draft PR #21](https://github.com/jvalkovv/YangSpaceApp/pull/21) is now open, stacked on #17, **SOURCE-ONLY / NOT BUILD TESTED**. Its allowlist is one test-only `ClaimTransactionContract` class, one isolated test CLI mode, one Windows exact-SHA runner and a proof-boundary doc. Synthetic SQLite EF transaction test succeeds or rolls back in one outer transaction across two SaveChanges; no real reviewer/HTTP/PG interaction or production behavior changes. **All runtime PASS is pending operator execution**.
- [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) still requires explicit policy acceptance (dedicated reviewer, no self-approval, verified evidence, no silent transfer, member regrant/revocation, audit/idempotency). No new claim approval endpoint was exposed.
- PR inventory: #21 OPEN/DRAFT/HOLD (source-only); #17 OPEN/DRAFT/HOLD (larger security/PG app/recovery); #1 OPEN/DRAFT/HOLD (V2 pilot/release); #19 MERGED/CLOSED (isolated bearer read QA). **No open PR is 100%-eligible for closure at this checkpoint.**
- Earlier tested combined F2a application SHA `445db4d9a5d28b798054c426afeba9b9be2c771b` retains its two Windows disposable PG/tenant PASS and operator evidence postflight PASS. No CT164/CT152, live DB, provider migration, claim activation, GitHub Actions or deployment.

## 2026-11-12 13:57 — F2a-3 review contract proposed (not approved)

Created [F2a-3 reviewer-controlled claim and Owner membership design](F2A3_CLAIM_REVIEW_SECURITY_CONTRACT_20261112.md), linked from [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20). It is **PROPOSED / HOLD**: dedicated live-checked reviewer authority, no self-approval, claim decision state machine, first-time only location ownership, atomic claim+organization+location+Owner membership+audit transaction, concurrency/uncertain-COMMIT protection and PostgreSQL HTTP negative/fault-injection matrix. Existing `BusinessClaim`/membership/audit indexes are accounted for.

**Important test-topology dependency:** the present disposable PG runner uses `--network none` and cannot be directly reached by a host `WebApplicationFactory`. A separate approved isolated PostgreSQL bearer HTTP test topology is needed. No implementation or public feature enabled yet; role, evidence handling, re-grant and transfer policy require explicit owner acceptance.

## 2026-11-12 13:57 Europe/Sofia — F2a combined exact-SHA evidence postflight PASS

**Operator read-only evidence verification: `F2A_COMBINED_EVIDENCE_POSTFLIGHT=PASS`** on the tested application SHA `445db4d9a5d28b798054c426afeba9b9be2c771b` ([PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17)). Two earlier complete tenant + disposable PostgreSQL regression runs passed at the identical SHA. The operator recomputed file hashes, verified both manifests and checked local disposable-container cleanup; the underlying local artifact bytes were not uploaded for an additional remote verification.

- `COMBINED_MANIFESTS=PASS`, `PG_SQL_INTEGRITY=PASS`, `PG_BACKUP_INTEGRITY=PASS`, `PG_EVIDENCE_MARKERS=PASS`, `PG_CONTAINER_CLEANUP=PASS`.
- Reviewed migration SQL SHA-256: `2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7` (pinned review checksum).
- Synthetic PostgreSQL backup SHA-256: `94c7c2cb1a2fcb93633c23efcb642764baea04f866e464e0728a22e06bdd664e` (matched to PG manifest).
- Tenant manifest SHA-256: `F544C05CE7DC829692072291660A60DAD40DCA00A68DD262B0EE125BF656BEE3`; PG manifest SHA-256: `738D715F7EA4D73A02DF09DBFEFA69530F6DBFD17BC2673DABFEB3DC5D1BA54B`.
- Latest tenant evidence directory: `D:\GitHub\YangSpaceApp-F2a1-Combined-QA-20261112-0822\logs\yangspace\f2a-tenant-scope\445db4d9a5d2-20261112-134120-cc109012`.
- Latest PostgreSQL directory: `D:\GitHub\YangSpaceApp-F2a1-Combined-QA-20261112-0822\logs\yangspace\pg-disposable\445db4d9a5d2-20261112-134126-299426c5`.
- **Accepted bounded gate:** combined source/tenant regression and disposable PG initial schema, constraints, sequential CAS, DB audit uniqueness rollback, synthetic backup/restore/integrity and isolated cleanup. No further retry of this same QA needed for Markdown-only changes.
- **Still HOLD:** actual Identity bearer authorization over PostgreSQL (existing F2a-2 real bearer TestServer acceptance is SQLite-only), parallel EF application writers, reviewer-controlled claim decision and ownership/membership grant/revoke, atomic application-level business+audit failure injection, PG least-privilege role, production migration/backfill/off-host recovery and release. No CT164/CT152, live DB, merchant activation or GitHub Actions.
- **PRs:** #19 MERGED/CLOSED; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD. Next work is [F2a-3 Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) security design decisions before implementation. Tested code SHA remains `445db4d…`; any newer docs-only PR head must not be labeled as separately runtime-tested.

## 2026-11-12 13:41 Europe/Sofia — latest combined-head F2a regression

- **Source:** [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) exact tested HEAD `445db4d9a5d28b798054c426afeba9b9be2c771b` — two successful operator-run Windows combined tests. Both `F2A_TENANT_SCOPE_ISOLATED_ONLY=PASS`, `F2A_PG_DISPOSABLE_RUNTIME=PASS`, `COMBINED_HEAD_REGRESSION=PASS`. PostgreSQL migration, model-diff, reviewed SQL, filtered unique/FK constraints, sequential CAS, DB audit rollback, synthetic `pg_dump`/second-DB `pg_restore` PASS in disposable PG17; source/tenant regression PASS.
- **Latest evidence:** tenant `logs/yangspace/f2a-tenant-scope/445db4d9a5d2-20261112-134120-cc109012`; PG `logs/yangspace/pg-disposable/445db4d9a5d2-20261112-134126-299426c5` in the operator-local combined QA worktree. Underlying file bytes and postflight hashes have not been received.
- **F2a-2 read-only Identity bearer:** PASS on isolated SQLite/TestServer exact source SHA `f579c70db9cc0986d2f1df8047158587f0e87eef`, merged via #19 into #17. **PostgreSQL bearer HTTP, parallel EF writes, reviewer claim approval, atomic application audit, least-privilege DB role, off-host recovery and release remain HOLD**.
- **PRs:** #19 MERGED/CLOSED; #17 OPEN/DRAFT/HOLD, GitHub reported mergeable at tested HEAD; #1 OPEN/DRAFT/HOLD. Latest parent-only documentation commits do not constitute source-code runtime validation.
- **Next dependency:** local evidence postflight without rerunning PG; [F2a-3 security contract Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) proposes a dedicated reviewer role and transactional claim/member ownership with failure injection; **not approved and not implemented yet**. No Actions, CT164/CT152 or live DB changes.

## 2026-11-12 08:15 Europe/Sofia — F2a-2 merged; F2a-1 parent remains HOLD

- [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) is **MERGED/CLOSED** by squash, merge SHA \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\`, into the F2a-1 feature branch. It is not merged into V2 or \`main\`.
- Exact tested code SHA: \`f579c70db9cc0986d2f1df8047158587f0e87eef\`; docs-only evidence follow-up: \`bc0a77ea6b6bc794a2d9d6aefcbe3c12936a329e\`. Compare verified one Markdown file only between tested code and docs follow-up; no code/test changes after the accepted source SHA.
- Operator exact-SHA restore, Release build warnings-as-errors, tenant regression and real Identity bearer TestServer PASS; read-only evidence postflight PASS. Manifest SHA-256: \`336D8A9DCB4C69483981A2A5124A5123E8AD9044C14A1619199439E2E121F366\`. Full receipts and operator-local evidence path are in [the merged F2a-2 QA report](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-f2a1-domain-foundation-20261111/docs/F2A2_MERCHANT_BEARER_READ_QA_20261112.md).
- Scope is disposable SQLite/TestServer with real Identity bearer tokens only. No PostgreSQL bearer HTTP, parallel EF writes, reviewer claim mutation, atomic application audit, production role/ingress, off-host recovery or deployment is implied.
- [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) now has combined head \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` and remains OPEN/DRAFT/HOLD. Its earlier PostgreSQL migration/constraints/synthetic backup/restore PASS is attached to prior exact SHA \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`; it must not be silently transferred to the combined head. Re-run exact-SHA source/build + disposable PostgreSQL regression on the combined head before further parent acceptance.
- [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) remains OPEN/DRAFT/HOLD for V2 release/pilot. Current PR inventory: #19 MERGED/CLOSED; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD.
- Next: combined-head regression, then [F2a-3 Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) for reviewer-only claim decision + membership grant/revoke + atomic audit/fault injection before implementation. No Actions, CT164/CT152 access, live DB, migration, feature activation or deployment.

## 2026-11-12 08:02 Europe/Sofia — F2a-2 bearer HTTP acceptance

- Exact tested child SHA: \`f579c70db9cc0986d2f1df8047158587f0e87eef\`, [Draft PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19), stacked on F2a-1 \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`.
- Operator Windows output: restore PASS, Release build warnings-as-errors PASS, tenant regression PASS, actual Identity bearer HTTP TestServer PASS; final \`F2A2_MERCHANT_BEARER_HTTP=PASS\`.
- Evidence path: \`D:\\GitHub\\YangSpaceApp-F2a2-Bearer-QA\\logs\\yangspace\\f2a2-merchant-http\\f579c70db9cc-20261112-075949-ea648860\`. Runner source confirms that it checks the required marker list and writes \`manifest.txt\` only after all stages/assertions pass. **Actual local manifest/log bytes have not yet been independently reviewed.**
- Scope: disposable SQLite/TestServer only; real Identity-issued bearer, 401/404, cross-tenant isolation, membership revocation and org suspension with same token, default-off/QA environment fail-closed, no registration/cookies/mutations, safe DTO/no side effects. No PostgreSQL HTTP claim.
- Source review of the bounded #19 diff found no blocking defect in the QA gate, Bearer-only policy, live membership JOIN, DTO allowlist, fixture setup or exact-SHA runner. Independent reviewer and evidence postflight remain outstanding.
- #19 remains Draft/HOLD pending evidence postflight and review; #17 remains Draft/HOLD pending real PostgreSQL bearer/parallel writer/audited mutation and least-privilege gates; #1 remains Draft/HOLD for program release gates. **0 open PRs eligible for closure.**
- Next: read-only manifest/log verification; then owner-approved merge decision for #19. After that, implement reviewer-only claim verification and membership grant/revoke with atomic audit and DB fault-injection as a separate scoped gate. No Actions, CT164/CT152 access, live DB, migration or deployment.

## 2026-11-12 07:49 Europe/Sofia — superseding F2a-2 HTTP QA status

- Windows operator exact HEAD `ceea4723ccc5d2fc7612e63ec3ff4239b37ea0b8` verified **PowerShell syntax, .NET restore, Release build and prior tenant regression PASS**, then HTTP fixture startup **FAIL** at API `Program.cs:46`: no `ConnectionStrings:Main` during early `WebApplication.CreateBuilder`. The fixture's late `ConfigureAppConfiguration` injection missed the startup guard. No real bearer HTTP tests completed.
- New corrective test-only [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) HEAD `f579c70db9cc0986d2f1df8047158587f0e87eef`: process-scoped synthetic SQLite + explicit QA mode **before host creation**, with restoration of process configuration and preserved fail-closed Production/Development checks. **New exact-SHA runtime QA NOT RUN**.
- [Roadmap](MVP_ROADMAP.md#2026-11-12-0749-europesofia--f2a-2-http-testserver-startup-recovery) owns current recovery gate. Open PRs #19, #17, #1: all Draft/HOLD, none 100%-accepted for merge. F2a-1 PostgreSQL synthetic recovery PASS stays scoped to `1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001`. No production database, CT164/CT152 or preview activation changed.

## 2026-11-12 07:39 Europe/Sofia — latest F2a progress and PR inventory

**Canonical milestone source:** [MVP roadmap progress/PR-closure checkpoint](MVP_ROADMAP.md#2026-11-12-0739-europesofia--f2a-2-build-failure--recovery-and-pr-closure-review). Earlier F2a "proposed/not implemented" paragraphs below are historical snapshots, not the current implementation status.

- **F2a-1 PostgreSQL disposable QA:** PASS at `1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001`; schema, constraints, sequential CAS, DB audit rollback, backup/restore and evidence integrity/cleanup verified from operator console. Not a production rollout.
- **F2a-2 real merchant Identity bearer HTTP:** [Draft PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) at `ceea4723ccc5d2fc7612e63ec3ff4239b37ea0b8`. Prior HEAD `62505864c934df1536194eb3bdc07eab29af648e` passed runner syntax/restore but failed Release build with test-only `CS0136` duplicate `owner` and `editor` locals. New commit renames fixture locals only. **Exact-new-HEAD build, tenant regression and actual bearer HTTP QA remain NOT RUN / HOLD.**
- **Open PR inventory:** #19 Draft/HOLD (HTTP QA), #17 Draft/HOLD (reviewed claims/real PG runtime/auth and release security), #1 Draft/HOLD (full program/release gates). **No open PR accepted 100% for merge/closure** at this checkpoint.
- **Standing governance:** meaningful in-conversation progress reports with full SHA and real evidence; review all open PRs at closure checkpoints; only owner-approved 100%-accepted scoped PRs may be merged/closed. Policy in merged PR #18 and [MVP roadmap](MVP_ROADMAP.md#delivery-governance--mandatory-progress-reporting--pr-closure-accepted-2026-11-11).
- **No exposure:** no YangSpaceQa authorization enabled on CT164, no active DB changes, no migrations applied to CT164, no production/preview activation or GitHub Actions. Next is fresh operator Windows exact-SHA QA and evidence review.

## F2a architecture work — 2026-11-11 (proposed, review pending)

**Status: SOURCE REVIEW / DESIGN PROPOSAL ONLY**, implementation gates remain HOLD. The current API has only global `PlatformAdmin`; Organization/Member/Claim rows and actor+revision audit are absent. [Issue #4](https://github.com/jvalkovv/YangSpaceApp/issues/4) owns tenant authorization, [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11) owns the full freshness/audit requirements. Proposed detailed contract: [F2a ownership and audit](F2A_TENANT_OWNERSHIP_AND_AUDIT_CONTRACT_20261111.md). **ADR-019 accepts PostgreSQL as the production target, but provider migration/activation is HOLD**; no new EF migration or owner endpoint enabled. F1a/F1b remain accepted for local code QA only, public demo unchanged.

## 2026-11-11 18:22 Europe/Sofia — current accepted F1 freshness milestone

This is the **current source/PR status**, superseding dated historical QA snapshots below. The V2 source baseline following F1b approval is `403b281abad2fb0c8048c2763a606c3435f3e5dd`. Additional documentation-only commits may advance Git HEAD without changing the tested application source.

| Workstream | Scoped source / QA status | Release boundary |
| --- | --- | --- |
| [F1a — public freshness (#13)](https://github.com/jvalkovv/YangSpaceApp/pull/13) | **MERGED/CLOSED**; candidate `9656518bb3d795a1e8cfaa7c3004678ee27fa6d4`, `SOURCE_VALIDATION=PASS`, `REAL_API_INTEGRATION=PASS`; squash `54b47576ab8206d599f837cd99340c008d49f0d6` | Only public read-only detail rendering and Bulgarian timezone/validity, **not deployed** |
| [F1b — publication timestamps (#14)](https://github.com/jvalkovv/YangSpaceApp/pull/14) | **MERGED/CLOSED**; candidate `2634014f64fcff237b29564d0572643f2cba034a`, `SOURCE_VALIDATION=PASS`, `REAL_API_INTEGRATION=PASS`, `SQLITE_RUNTIME_INTEGRITY=PASS`; squash `403b281abad2fb0c8048c2763a606c3435f3e5dd` | 9 nullable migration columns, lifecycle timestamps and EF initial→F1b disposable DB upgrade/recovery proved locally; **not applied to CT164** |
| [Issue #11 — F2 actor/field audit](https://github.com/jvalkovv/YangSpaceApp/issues/11) | **NOT STARTED** — needs authenticated organization/member scope, atomic audit and optimistic revision | Do not allow owner writes with PlatformAdmin as proxy for real organization ownership |
| Issue #11 — F3 owner confirmation/moderation | **NOT STARTED**, depends on F2 | No verified merchant freshness badge or unmanaged self-service |
| [V2 → legacy main parent PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) | **DRAFT/OPEN**, only open application PR after #14 | Full architecture, PVE security, off-host restore, release and pilot gates remain **HOLD** |

**F1b exact candidate acceptance evidence (operator-local, not copied into GitHub):**
- `D:\GitHub\YangSpaceApp-F1b-Scaffold\logs\yangspace\verify\2634014f64fc-20261111-181828-f2898981`
- `D:\GitHub\YangSpaceApp-F1b-Scaffold\logs\yangspace\api-integration\2634014f64fc-20261111-181923-5dbe2df0`
- `D:\GitHub\YangSpaceApp-F1b-Scaffold\logs\yangspace\sqlite-runtime-integrity\2634014f64fc-20261111-181244-81cbfb42`

**Current operations:** no release, live database apply, CT164/CT152 change or tenant authorization acceptance from PRs #13/#14. Existing public demo remains the earlier source release `a480280...` per previous operator evidence. Source QA is not production gate acceptance. Use [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11) for the sequenced F2/F3 work contract. Historical status notes below remain archival.

## 2026-11-11 runtime status addendum — operator-provided evidence

**Why this addendum exists:** the source snapshot at `a4802805` and the historical CT164 provisioning log were written **before** the owner activated the application and public reverse proxy. The older "application not installed / CT152 unchanged" statements remain historically correct **at their timestamps**, but must not be used as the current live-state assessment.

**Evidence provenance:** console output pasted by the operator into the YangSpace conversation from `root@pve` on **2026-11-10**, plus the independent **read-only** Cloud Browser audit reported on 2026-11-11. This is **not** an independent SSH read or a persisted, signed release attestation. No credentials, API writes, database contents, or infrastructure mutations were used to prepare this documentation.

| Layer | Updated observation | Acceptance boundary |
| --- | --- | --- |
| CT164 application release | Operator's guarded shell verified `/srv/yangspace/current` resolves to `/srv/yangspace/releases/a4802805421f10e834ad35bbf2f259db4e4eab84`; `yangspace-preview.service` active; demo SQLite present. | **Operator-reported runtime PASS**, not cryptographic binary-to-source attestation. |
| CT164 internal web/API | Local Nginx `/ = 200`, `/api/v1/homepage = 200`; local CT-IP request denied `403`; registration negative probe `404`. | Scoped preview/read-only smoke; broader direct-origin ACL and auth negative matrix remain unverified. |
| CT152 proxy and HTTPS | Operator output: `nginx -t` PASS, YangSpace virtual host present and enabled, `/ = 200`, `/api/v1/homepage = 200`; admin/auth/health paths `404`, public API POST `403`. CT152 service journal records successful reload. | Public **read-only demo** route active, not production merchant access. |
| Public Cloudflare endpoint | Operator's external `curl https://yangspace.gndstore.eu/` reported `200` and `/admin` reported `404`; Work Cloud Browser reported 6 detail pages, search, filters, retry and 375–1920 px responsive smoke. | Live browser smoke only. No signed client build fingerprint, backend auth with credentials, cross-browser proof or load/WCAG certification. |
| Operational/release readiness | PVE thin pool reported `84.76%` during activation; effective firewall, off-host encrypted backup + restore, monitoring and rollback evidence still incomplete. | **G-PREVIEW remains HOLD** as a comprehensive operational gate; **G-PILOT remains HOLD**. Browser reachability is not approval for real merchant content or writes. |

**Current interpretation:** the apparent *live-versus-provisioning mismatch* is explained by the historical log's age, not a demonstrated alternative origin. The **remaining release identity gap** is to verify the exact running binary/assets against immutable release SHA and publish a safe build fingerprint. Preserve the recorded operator console evidence and close this gap with independent, read-only verification before a later deployment. **Do not repeat activation or mutate CT152/CT164 to update documentation.**

## 2026-11-11 — source QA, communication and PR closure addendum

**Exact-SHA manual acceptance:** operator ran `Verify-Source-Local.ps1` and `Verify-Api-Local.ps1` from Windows on `2baeb797b4d9ca0f5ce9d6c3d311a7b8092be7dc`. Console transcript reported `SOURCE_VALIDATION=PASS`, `REAL_API_INTEGRATION=PASS`, `BOOTSTRAP_PRIVILEGE_ESCALATION_DENIED=PASS`, `BOOTSTRAP_NONADMIN_ROLE_RETAINED=PASS`. Source runner passed lock/install/audit/typecheck/build/Playwright/API restore/build; API runner passed disposable database prepare/restart/anti-escalation/recovery checks. No live DB writes. Local evidence, not independently read by this GitHub-only update:

- `D:\GitHub\YangSpaceApp\logs\yangspace\verify\2baeb797b4d9-20261111-074951-04b1acd1`
- `D:\GitHub\YangSpaceApp\logs\yangspace\api-integration\2baeb797b4d9-20261111-075201-77648186`

**PR reconciliation:** [#9](https://github.com/jvalkovv/YangSpaceApp/pull/9) MERGED/CLOSED at `ea959a104275599212156dc4d9f1ce76adcf13f8`; [#10](https://github.com/jvalkovv/YangSpaceApp/pull/10) MERGED/CLOSED at `2baeb797b4d9ca0f5ce9d6c3d311a7b8092be7dc`. At this status checkpoint, only **[#1](https://github.com/jvalkovv/YangSpaceApp/pull/1)** remains Draft/Open; it is **not 100% complete** because merchant ownership, real data, recovery and production gates are not accepted. New doc-only PRs are handled as isolated work and reconciled after merge.

**Mandatory progress reporting:** report meaningful GitHub commits, tests, PR transitions, release gates and blockers promptly in the active conversation; the existing hourly YangSpace GitHub check notifies only on substantive change. Always cite actual source SHA and test/evidence scope. No promise of continuous independent execution. **PR hygiene:** merge/close fully accepted, owner-approved scoped PRs; keep incomplete PRs open/draft; do not close unmerged merely to clear the list. See [MVP roadmap](MVP_ROADMAP.md#delivery-governance--mandatory-progress-reporting--pr-closure-accepted-2026-11-11).

**Gate verdict:** current engineering source QA **PASS for `2ba...`**; comprehensive G0/G-PREVIEW/G-PILOT **remain HOLD** for outstanding security/operational/product requirements. No deployment has occurred from this candidate; CT164 public demo is still reported at `a480...`.

## 1. Purpose, precedence and change control

This file is the **single entrypoint** for *current state, gate status, sequencing and unresolved decisions*. Detailed execution and evidence remain in the linked specialist records below. Do not maintain another conflicting top-level status list.

| Authority / purpose | Source | Rule |
| --- | --- | --- |
| Accepted owner decisions | [Decision log](DECISIONS.md), especially [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md) | An accepted ADR controls policy until explicitly superseded by another approved ADR. |
| **Current cross-project status and execution order** | **This SSOT** | Must be updated after validated milestone/gate changes, linking exact evidence. |
| Detailed work items and implementation state | [POC tracker](POC_TRACKER.md) | Task IDs, dependencies, assigned responsibility, evidence location; align with this SSOT. |
| Objective PASS/FAIL criteria | [Acceptance matrix](POC_ACCEPTANCE_MATRIX.md) | A source change or mocked test cannot substitute for required real-runtime evidence. |
| Product vision and scope | [Product](PRODUCT.md), [Master Plan](POC_MASTER_PLAN.md), [Domain Model](DOMAIN_MODEL.md) | Design targets are not evidence of implementation. Historical “current” summaries are superseded by this file. |
| Operator evidence / procedures | [CT164 log](CT164_PROVISIONING_20261110.md), [release runbook](POC_PVE_RELEASE_RUNBOOK.md), [SQLite integrity report](S2_SQLITE_RUNTIME_INTEGRITY_20261110.md) | Dated observations remain historical facts; older “no CT exists” or TODO inventories must not override later accepted evidence. |
| Code reality | Reviewed Git commit SHA | Actual behavior must be checked in source **and** in the deployed release; a PR description is not runtime proof. |

**Evidence hierarchy:** direct exact-SHA runtime logs + reviewer acceptance > validated operator-provided test records > source inspection > browser screenshots > proposal/narrative. A stale document is not evidence that an implemented feature has disappeared; conversely a code commit is not evidence that it runs correctly. Mark unknown facts **UNVERIFIED**, unmet gates **HOLD**, and confirmed failures **FAIL/BLOCKED**. Separate *source-implemented*, *tested locally*, *verified on CT164*, and *accepted for public release*.

**Policy:** No GitHub Actions/hosted CI, webhook auto-deploy, blind `git pull` activation, unreviewed PVE writes or silent merge. Use clean exact-commit, manual/operator-run QA and retained per-app logs per [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md). Work in isolated feature/docs branches; reconcile to the active feature HEAD before merging. **Never modify the active CT or database merely to synchronize documentation.**

## 2. Accepted product boundaries

- **YangSpace** = hyperlocal directory of **businesses, offers, brochures and local jobs**, starting with one Varna neighborhood, then Varna, then other cities once pilot gates pass.
- Visitors browse without mandatory registration. Verified business owners/editors manage only their own organizations/locations after a controlled claim/approval flow. Moderators manage reviews/reports/audit.
- Target stack: Angular 22 + ASP.NET Core 10/EF Core; modular monolith; controlled PVE single application LXC initially, Nginx/systemd and separated persistent data/release/config.
- **Out of MVP:** booking, integrated payments, paid subscriptions as a predetermined tariff, native iOS/Android apps, CV database/ATS, AI recommendations and bulk uncontrolled imports.
- **No invented data:** demo entries, offers, time schedules and badges must be clearly identified; never claim “verified”, “open now”, actual proximity or authentic promotions without evidence.
- **Content freshness and accountable edits:** [Issue #11 — business freshness, brochure validity, actor/field-level audit and owner confirmation](https://github.com/jvalkovv/YangSpaceApp/issues/11) is the accepted requirement contract. Public expired-brochure filtering and location-level `UpdatedAtUtc` exist, but they do **not** prove owner verification or field-level publication history. Implement per-entity timestamps, server-attributed audit, ownership and approval in reviewed slices **before enabling merchant CMS writes**.
- Initial 10–20 merchant invite/claim pilot is a **proposal**, not an approved target. Agree with product owner before outreach.

## 3. Historical source-review snapshot (pre-activation; superseded by the runtime addendum above)

| Layer | Verified/implemented evidence | Qualification |
| --- | --- | --- |
| Repository | `main` = legacy; Draft PR #1 targets `main` from `feat/yangspace-v2-foundation-20261108`; reviewed HEAD `a4802805`. | This docs branch is isolated. Feature branch may advance independently. |
| Frontend foundation | Home/Detail/Admin Angular components, category/search, pagination, skeleton/error/retry states; green/cream visual language. | Historical 17/17 Playwright PASS on `20aa30c` with **mocked API**. Not proof for reviewed HEAD. |
| Real local engineering | Operator-reported clean source/API integration PASS at `b30c1c8`; committed npm lockfile and .NET build records. | QA does **not** automatically transfer to later HEAD; repeat exact-SHA validation. |
| Persistence | Reviewed EF Core initial migration committed; FK runtime checks and isolated DB backup/restore local regression reported PASS at `2069eb9` / `1ce3fa3`. | **Disposable Windows DB only**; not public database cutover or off-host operational restore. |
| Public directory | Published locations, facets, search, active offers/brochures/jobs, detail routes; six fictional seeded examples. | City/geo coordinates, distance search, owner verification, rich opening hours, real photos and indexable SEO not complete. |
| Admin CMS | PlatformAdmin bearer policy; source APIs for create/edit/hide/restore locations and publications; offline admin provisioning and production startup guards. | Owner portal, cross-tenant permissions, moderation/audit and exact-HEAD real CMS browser/DB acceptance are missing. |
| CT164 guest | Operator reported Debian 13.6 unprivileged LXC; systemd running; .NET 10 runtime; dedicated `yangspace`/`yangdeploy` identities and actual positive/negative directory ACL PASS; DHCP .110, gateway/DNS .102 persisted after reboot. | DHCP **server identifier .107 unexplained**; PVE firewall effective protection, storage headroom, independent backups, application release, CT152 ingress and full recovery are not accepted. |
| Public URL screenshots | User-supplied iPhone screenshots of `yangspace.gndstore.eu` show Hero, menu, search, category select, cards and detail. | **Origin, environment, deployed SHA, DB provenance, effective access control and exact viewport are UNVERIFIED.** A visible URL/HTTP 200 is not a CT164 deployment acceptance. |

**Important drift reconciliation:** Older [PVE runbook](POC_PVE_RELEASE_RUNBOOK.md) opening text (“no CT provisioned”) predates the confirmed CT164 guest work and is **SUPERSEDED for infrastructure inventory status** by the later [CT164 evidence](CT164_PROVISIONING_20261110.md). The runbook's security/release procedure remains applicable. The [production CMS contract](PRODUCTION_CMS_SECURITY_20261109.md) also includes a historical “no migration committed” statement superseded by the actual [migration evidence](S2_INITIAL_MIGRATION_20261110.md); **runtime production schema adoption remains HOLD**.

## 4. Product/architecture findings requiring resolution

### P0 — release/architecture boundaries

1. **Database ADR conflict:** [Master Plan](POC_MASTER_PLAN.md) / [DECISIONS](DECISIONS.md) leave PostgreSQL vs SQL Server open (ADR-007/013); current [Models.cs](../apps/api/Models.cs), [Program.cs](../apps/api/Program.cs), migration and [ProductionSqlitePolicy.cs](../apps/api/ProductionSqlitePolicy.cs) explicitly implement SQLite even for guarded Production startup. **Decision required before G2:** accept SQLite as a bounded single-instance pilot with explicit concurrency/backup/data growth limits, or adopt a reviewed PostgreSQL/SQL Server strategy. No silent provider flip and no real DB conversion as a side effect.
2. **Missing multi-tenant domain:** `DirectoryDb` contains Identity, BusinessLocation, Offer, Brochure, Job, but no Organization/OrganizationMember/BusinessClaim, scoped ownership or cross-tenant API policies. The existing admin is a global PlatformAdmin, **not** a merchant CMS.
3. **Content governance missing:** public boolean publication is a start, not a full draft → review → publish → expire/archive/reject/suspend lifecycle; no persisted actor/reasoned audit, business reports or vetted upload pipeline.
4. **Infrastructure safety:** CT164 guest preparation PASS does not establish protected CT152→CT164 route, direct-origin denial, PVE firewall, trusted DHCP server, storage resilience, off-host backup/restore or safe activation. No service/DB/proxy activation on the strength of this SSOT.
5. **Release identity gap:** the screenshots of a reachable site and the CT164 “no active app release” operator report may refer to different hosting states. Determine actual DNS, edge origin, active release marker, runtime configuration and DB **read-only** before making any release claim.

### P1 — public quality and scale

6. **Geo semantics:** search filters neighborhood/category/string only; no stored coordinates, city filter in public query or distance ranking. Do not label results “nearest”.
7. **Real business presentation:** home cards ignore available `imageUrl` in favor of category decoration; detailed public profile shows a hard-coded demo disclaimer. Introduce contextual demo/verified status, real media and accurate opening-hours presentation.
8. **SEO:** Angular CSR preview and `noindex` are intentional for demo, but not an indexable public pilot. Decide SSR/prerender and build canonical metadata/schema/sitemap before G4.
9. **Error messaging:** [directory-api.service.ts](../apps/web/src/app/directory-api.service.ts) shows development-specific `127.0.0.1:5090`/Angular proxy instructions to end users. Production errors must be actionable without exposing internal operations.

### Mobile UX review (visual observations, **not verified defects**)

- **Keep:** green/cream brand, strong hero type, clearly identifiable CTAs, native form controls and accessible semantic structure.
- **Improve:** reduce first-screen hero and excess vertical spacing; make mobile discovery/filter flow more compact; prioritize useful business information over decorative category covers; ensure menu state, focus and scroll behavior.
- iOS dark select popovers in screenshots are **native browser UI**, not a broken dropdown. Replace only if a custom implementation passes touch, keyboard, VoiceOver and accessibility checks.
- Verify on **real Safari/iPhone and Chromium**; screenshot width alone is not proof of CSS viewport. Test 320/375/390/768/1024/1440 CSS px, dynamic font/zoom, focus, overflow, keyboard, reduced motion and Core Web Vitals.

## 5. Canonical execution sequence and acceptance

| Order | Package / tracker mapping | Must exist before work is accepted | Exit evidence |
| --- | --- | --- | --- |
| **0** | **Reconcile Work → GitHub → CT164/runtime** (cross-cutting) | Exact latest Work HEAD, changed paths, PR, active-release identity; avoid concurrent writes | Read-only reconciliation record, current source SHA, runtime/source drift, revised tracker; HOLD if unknown |
| **1** | **Engineering & infrastructure P0** — YS-S0-02..06, YS-S5-01..06 | Re-run manual exact-SHA QA; CT ingress/auth, effective firewall, storage risk, independent backup/restore, monitored systemd/API and rollback | G0 + G-PREVIEW evidence separately; private only until gate PASS |
| **2** | **Domain & data architecture P0** — YS-S2-01..06 | Database ADR approved; migration compatibility; normalized city/neighborhood/category; organizations, location ownership, claims/memberships, audit | G2 real DB and cross-tenant negative tests |
| **3** | **Accounts, CMS & moderation P0** — YS-S1-01..07, YS-S3-01/02/05 | Scoped business signup/login/verification/claim with reviewer authorization; owner/editor only own resources; publish controls | G1/G3 unmocked end-to-end scenarios; persistence after restart |
| **4** | **Real content/features P1** — YS-S3-03/04, YS-S4-01..03 | Photo/media with type/size/rights/quarantine controls; profiles with address/hours/geo; offers/brochures/jobs expiry and discovery | Real approved content, upload-negative tests, complete public journeys |
| **5** | **Mobile design, SEO & quality P1** — YS-S1-04, YS-S3-06, YS-S4-04/05 | Mobile layout refinement without brand reset; reusable component patterns; SSR/prerender ADR; A11y/performance | Browser visual approvals, keyboard/screen reader, Core Web Vitals, SEO/crawler evidence |
| **6** | **Limited public pilot P0 gate** — YS-S6-01..05 | Independently verified business ownership/content, terms/privacy/reporting, no demo leaks, security/recovery/performance completed | G-PILOT=PASS, operator sign-off, first real KPI baseline |

Dependencies matter more than calendar dates. Work on mobile *design specs* may occur in parallel, but avoid conflicting commits to the same branch, and do not open owner workflows publicly before G2/G3 authorization.

## 6. Release gates — snapshot and blockers

| Gate | Current verdict | Main blocker / proof required |
| --- | --- | --- |
| G0 Engineering | **HOLD** | Re-run source, NuGet/npm, Playwright, real API/security checks against **current candidate SHA**; audit findings and approved manifests. |
| G1 Real local CMS | **HOLD / PARTIAL SOURCE** | Full admin create→edit→publish→public read→hide→restore→restart with real DB; unauthenticated/nonadmin denials. |
| G2 Production domain | **HOLD** | DB ADR; migrations; Organization/claims/membership; per-object tenant authorization and audit. |
| G3 Content/moderation | **HOLD** | Full reviewed publication lifecycle, safe media, abuse reporting and actor/reason audit. |
| G4 Public discovery | **HOLD** | Geo correctness, verified profiles, SEO/indexing, accessibility and measured performance. |
| G-PREVIEW protected PVE | **HOLD** | Verified source↔runtime SHA; private TLS/ACL, reverse-proxy negative probes, recovery and monitoring. CT164 OS-level readiness alone insufficient. |
| G-PILOT public | **HOLD** | All public prerequisites plus real consented merchant data, legal/privacy and operational sign-off. |

**NO-GO conditions:** unverified deployment source, reachable preview identity/registration/bootstrap on public origin, external DB writes outside approved maintenance, cross-tenant write, unauthorized publishing, absent verified off-host restore for real data, missing rollback, or unsolved P0 security finding. The application should remain private/demo-restricted while these are open.

## 7. Immediate ordered queue (next operator/Work actions)

1. Obtain latest Work result, PR URL, exact HEAD, local test evidence and files changed; record whether it supersedes the reviewed `a4802805` baseline. Do **not** merge competing commits blindly.
2. Perform read-only live-origin reconciliation: DNS→CT152→upstream; live artifact commit marker; CT164 service state and configs (redacted); public/preview identity endpoint accessibility; DB schema/identity; no secrets in evidence. Mark unknown results as HOLD.
3. Re-run exact-SHA manual source + real API integration; distinguish mocks from unmocked backend/DB tests. File evidence under separate web/api/deploy/verify/restore logs and reference immutable SHA.
4. Address CT164 risk register **without unsafe speculative changes**: source of DHCP `.107`, effective firewall/ACL, `pve/data` thin allocation/headroom, off-host backup policy and recovery rehearsal.
5. Close/approve DB provider ADR before introducing real organization/claim tables. Implement a focused vertical slice: **Organization → BusinessClaim → reviewed membership → scoped Owner CMS** with strict server-side permission tests.
6. Freeze a short mobile UX specification from actual iPhone/Safari observations; implement in an independent scoped PR only after the Work source merge boundary is known.
7. Update this SSOT + tracker + acceptance matrix + operator runbooks in the **same reviewed change** when any gate moves. Require proof, never percentage-based status.

## 8. Operational risk register

| Risk | Severity | State / owner role | Closure criterion |
| --- | --- | --- | --- |
| Live artifact identity not independently attested | P1 | OPEN — release operator | Operator reported CT164 release symlink, CT152 HTTPS and public edge probes; still require immutable built-asset/API fingerprint and independently retained verification. |
| Thin pool high allocation, no VG free space | P0 | OPEN — PVE operator | Audited capacity/headroom thresholds; backup/restore before storage actions; alerts. |
| No accepted off-host database + media recovery | P0 | OPEN — operator | Encrypted/controlled copy, retention, restore into scratch, integrity check, measured RPO/RTO. |
| PVE firewall / edge admin exposure | P0 | OPEN — network/security operator | Effective ACL and direct-origin denial proven; no client-supplied gateway-header bypass. |
| DHCP responder `.107` unidentified | P1 security | OPEN — network operator | Responder provenance and DHCP trust risk assessed; route/DNS persistence already guest-tested. |
| SQLite production provider vs DB ADR | P0 architecture | OPEN — engineering + owner decision | Accepted documented ADR with limits, migrations, concurrency, migration/restore strategy. |
| Missing multi-tenant auth, audit and uploads | P0 product/security | NOT IMPLEMENTED — backend/product | Owner A/B isolation, moderation, safe media and end-to-end negative tests. |
| Mobile density, useful listing data, SEO | P1 product | NEEDS DESIGN/RUNTIME QA — web/product | Approved real-device usability, a11y, crawler and performance checks. |

Role labels above indicate **required reviewer functions**, not claims that a specific individual has accepted responsibility.

## 9. Update / evidence contract

Every milestone or gate update MUST include:

~~~text
TASK_ID / GATE:
SOURCE_REF / FULL_COMMIT_SHA:
DEPLOYED_COMMIT_SHA: <exact value or UNVERIFIED>
FILES_CHANGED:
TEST_SCOPE: SOURCE | MOCK_BROWSER | REAL_API_DB | PVE_RUNTIME | PUBLIC_ORIGIN
COMMANDS / EXIT_CODES:
HOST / ENVIRONMENT / FIXTURE:
EVIDENCE_PATHS (web, api, deploy, verify, restore):
NEGATIVE_SECURITY_TESTS:
VISUAL_DEVICE / VIEWPORT EVIDENCE:
BACKUP_REF / RESTORE_RESULT / RPO / RTO:
REGRESSIONS / OPEN_RISKS:
REVIEWER / DATE:
VERDICT: PASS | PARTIAL | HOLD | FAIL | UNVERIFIED
NEXT_DEPENDENCY / OWNER_ROLE:
~~~

**Status rules:** `DONE` only after that task's complete acceptance contract; `PARTIAL` means implementation or limited evidence exists; `HOLD` means a prerequisite/approval is missing; `UNVERIFIED` means no trustworthy observation; `FAIL` requires RCA and regression evidence. Historic test PASS always remains attached to its own SHA and environment. Never paste real credentials, private keys, bearer tokens or unredacted DB contents into issues/docs/logs.

**Change protocol:** an accepted architectural direction requires a dated ADR in [DECISIONS](DECISIONS.md), then updates here, in [POC_TRACKER](POC_TRACKER.md) and in [POC_ACCEPTANCE_MATRIX](POC_ACCEPTANCE_MATRIX.md). When details disagree, explicitly record the conflict and the newest evidence; do not silently “fix” the historical record. The latest approved gate report controls release eligibility, not the most recently edited Markdown header.


## 2026-11-12 — disposable PostgreSQL QA runner (historical pre-execution snapshot)

Windows operator confirmed local Docker Desktop Linux Engine v29.9.1. PR #17 F2a code/migration HEAD `e1287058...` passed Windows exact-SHA build, tenant regression and committed EF snapshot-diff gates. Added a new source-only, fail-closed Docker PostgreSQL 17 runner with SQL for initial migration/partial UNIQUE/FK/revision CAS/database rollback and synthetic backup→restore. **Runner has not run; no real PostgreSQL PASS may be claimed yet.** Independent review and operator exact-new-SHA runtime proof are next. CT164/CT152 unchanged; PR #17 and master PR #1 stay Draft/Open. [Details](F2A1_SOURCE_FOUNDATION_20261111.md).











## 2026-11-11 22:09 Europe/Sofia — F2a PostgreSQL schema review findings / source fix

**Latest F2a progress, not yet QA accepted:** PostgreSQL initial EF C#/Designer/snapshot/SQL for source `ccd2acb...` was independently inspected: ZIP SHA-256 and four internal checksums match, 15 tables/22 indexes, Designer↔snapshot model equal and no destructive `Up` statements. **HOLD on commit:** no DB-level approved/pending claim deduplication and no `Locations.Revision` in the generated schema, contrary to F2a contract. Original ZIP is preserved as rejected review evidence. Draft PR #17 now has a model correction for filtered unique `Approved` by Location, filtered unique `Pending` by (Location, Organization), Location revision concurrency token and new isolated regression tests. **Current source/EF scaffold/actual PostgreSQL QA remain PENDING**. See [F2a source foundation](F2A1_SOURCE_FOUNDATION_20261111.md), [claim policy](F2A_TENANT_OWNERSHIP_AND_AUDIT_CONTRACT_20261111.md) and PR #17. No CT164/CT152 changes or approved owner endpoint.











## 2026-11-11 F2a provider NuGet blocker (historical; later exact-SHA restore/PG QA supersedes)

**Current truth:** operator checked out `aa4fec12873b5319072087317a6b64eb6a8a5162` with a clean exact remote SHA and `Test-F2a-Tenant-Scope-Local.ps1` failed at `f2a-restore` (NU1902/NU1903) before any build/tenant regression. New PostgreSQL migrations project resolved vulnerable transitive `System.Security.Cryptography.Pkcs 9.0.0` and `System.Security.Cryptography.Xml 9.0.0`. Keeping NuGet Audit and TreatWarningsAsErrors is required. Source remediation `8d2511308ff8e14b6d9a25ba6ec5539c8f2de4aa` pins both to patched `10.0.14` in the affected migrations csproj, and handles empty Git branch stdout in the detached worktree scaffold. **Fix has not been run on Windows; no current source QA PASS is claimed.** Local failure evidence: `D:\GitHub\YangSpaceApp-F2a1-PG-QA\logs\yangspace\f2a-tenant-scope\aa4fec12873b-20261111-210730-fb959c92`.

**Next sequence:** re-fetch exact PR #17 HEAD, verify clean detached worktree/PowerShell parse, rerun `Test-F2a-Tenant-Scope-Local.ps1` with source+NuGet Audit PASS, then run guarded PostgreSQL EF migration scaffold and independent review. No database connection, active CT164 migration or public owner write routes. Issues #4/#11 are the implementation acceptance ledger; PR #17 remains Draft/HOLD.



## 2026-11-13 — F2a-3 reviewer authorization operator QA failure and correction

Operator exact-SHA run on `7ed7ab3d0e00da41fb3d11afeb6ed45c69c2fa1b`: PowerShell parser, .NET restore and Release build **PASS**; isolated Identity bearer HTTP **FAIL** during reviewer-role revocation. EF Core reported duplicate tracked `IdentityUser`: the test reused the reviewer entity created in an earlier DI scope inside a later `UserManager` scope. This is a test-harness entity-lifetime defect; the HTTP acceptance is not PASS.

Failure evidence (operator machine): `D:\\GitHub\\YangSpaceApp-F2a3-ReviewerAuth-QA\\logs\\yangspace\\f2a3-reviewer-auth\\7ed7ab3d0e00-20261113-060137-7782b371`. Runner wrote `gate.txt`; no passing manifest was produced.

Correction committed in PR #23: the revocation test reloads reviewer by ID through the active scoped `UserManager`, verifies the role is present, then removes it using that same-scope entity. Runner now requires markers for both the reload and pre-revocation role check. Current corrected source candidate at the time of this entry: `9ced228838c11b4584229160fe1b40565aff3c7f` (the documentation commit that follows may advance the SHA).

**Current state: SOURCE CORRECTION COMMITTED / CORRECTED SHA NOT YET TESTED / PR #23 DRAFT-HOLD.** Next: clean detached worktree at the latest PR head; native parser; restore; Release build with warnings-as-errors; isolated SQLite Identity bearer HTTP; independent stage/manifest/hash postflight and scoped review. No PostgreSQL, CT164/CT152, live DB, claim decision mutation or deployment. Evidence/retention approval remains a separate prerequisite for any future real decision endpoint.


## 2026-11-13 — F2a-3 bounded reviewer authorization child completed

PR #23 was merged into the F2a-1 feature branch: `c54c6cc4f4cd58b184c5d6b0dc7ec6a7c3507808`. Tested source SHA: `e4ba6bbd697eeaeec4bf6fafe81ab78e30d3f5c4`.

Windows exact-SHA QA and independent postflight passed: parser, restore, Release build, isolated ASP.NET Core Identity bearer HTTP, exact source/clean worktree, manifest, three stage receipts, command validation and **27/27 HTTP markers**. No failure gate. Evidence path: `D:\GitHub\YangSpaceApp-F2a3-ReviewerAuth-QA-v2\logs\yangspace\f2a3-reviewer-auth\e4ba6bbd697e-20261113-061414-30f957ed`. Manifest SHA-256: `7c3c3894c229f9020c9892f8b618d0220b79dd4561e64b8eea26b8a0385aa9d0`.

**Bounded result only:** reviewer role is checked against persisted Identity state on every request; same-token revocation denies access; conflict helper and default-OFF QA restrictions pass. Test uses disposable SQLite TestServer. PostgreSQL bearer HTTP, real claim mutation, ownership grant, evidence handling and production remain NOT ACCEPTED.

Issue #20 remains OPEN for the separate evidence/retention/legal approval and subsequent claim-decision implementation. Parent PR #17 remains Draft/HOLD for broader F2a-1 acceptance. No CT164/CT152, live DB, Actions or deployment.


## 2026-11-13 — F2a-3 Evidence & Retention Policy OWNER-APPROVED

Canonical policy v1: [F2A3_EVIDENCE_AND_RETENTION_POLICY_20261113.md](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-f2a1-domain-foundation-20261111/docs/F2A3_EVIDENCE_AND_RETENTION_POLICY_20261113.md).

**Owner approval: RECORDED 2026-11-13. Legal/privacy clearance: BLOCKED on [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24).** The approved operational baseline includes evidence categories/proof thresholds, data minimisation/redaction, private claim-scoped access/storage, file controls, retention/deletion, audit and backup tombstone replay.

No real evidence collection/processing, production upload/storage or live claim-decision activation before written legal/privacy clearance. Isolated non-production QA with synthetic data only may proceed if it is technically incapable of accepting real evidence and is not wired into production.

F2a-3 remains **RELEASE HOLD**. [Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25) tracks the synthetic-data-only QA foundation; [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains the hard legal/privacy gate for any real evidence.


## 2026-11-13 — F2a-3 synthetic evidence policy code candidate

[Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25) is now implemented as [Draft PR #26](https://github.com/jvalkovv/YangSpaceApp/pull/26), exact candidate `519809875658e7e27232d6a49536f2d71933ea69`.

Source-only scope: pure evidence file validation, deterministic retention calculation, UTC timestamp validation, fail-closed malware scanner configuration guard and synthetic-only runtime tests. No HTTP route, evidence schema/migration, production storage or real evidence intake.

**State: NOT WINDOWS-TESTED / DRAFT / HOLD.** Exact-SHA restore, Release build, runtime execution and independent evidence postflight are still required. [Legal/privacy Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains the hard gate before any real evidence collection or production activation.

## 2026-11-13 08:24 Europe/Sofia — combined-head regression PASS

Operator ran both required Windows exact-SHA runners from a clean detached worktree at `cf32e641c9fdf45bcdf981b5ebd0a922fb2467ec`. Console evidence supplied in the project conversation:

- Tenant runner: restore, Release build and tenant-scope contract all PASS; `F2A_TENANT_SCOPE_ISOLATED_ONLY=PASS`.
- PostgreSQL runner: EF tool restore, migrations restore/build, pending-model-diff, reviewed SQL hash, isolated PostgreSQL 17 container, initial migration, constraints, synthetic dump, restore to second DB and restored-state verification all PASS; `F2A_PG_DISPOSABLE_RUNTIME=PASS`.
- Independent postflight found both manifests, required markers, no `gate.txt`, and no leftover `yangspace-f2a-pgqa-` containers.
- Tenant evidence directory: `logs/yangspace/f2a-tenant-scope/cf32e641c9fd-20261113-081601-7ad450a6`; manifest SHA-256: `0197FA2ECE84FEB71B9C411CB6772355FB39FAF12665AB93407F3C1B524A686E`.
- PostgreSQL evidence directory: `logs/yangspace/pg-disposable/cf32e641c9fd-20261113-081618-7632fff2`; manifest SHA-256: `0BDFC76E5C6764A1ED8DFCFED741D08F9FEE1994E4F4429D12AD356EB7FC08D6`.
- Both manifests retain `CT164_DB=NOT_ACCESSED` and `RELEASE_GATE=HOLD`. PG runner scope explicitly leaves real bearer and parallel writers NOT TESTED.

**Acceptance:** combined source/build + tenant regression + disposable PostgreSQL schema/constraints/CAS/rollback/backup-restore gate is PASS on exact head `cf32e641c9fdf45bcdf981b5ebd0a922fb2467ec`. The evidence directories and full raw stage logs remain operator-local; only the console transcript and manifest hashes were supplied here. Do not claim independent byte-level re-verification of local evidence files.

**Still NOT TESTED:** PostgreSQL-backed ASP.NET Core Identity bearer HTTP; real application-layer concurrent EF writers; atomic claim/organization/location/Owner membership/audit transaction under competing writers; fault injection across mutation boundaries; least-privilege PostgreSQL app role; off-host recovery. No real claim decision endpoint, evidence intake, production storage, CT164/CT152 access, deployment or release approval.

**Next:** a separate default-OFF, synthetic-only PostgreSQL bearer HTTP + competing EF writer child slice. Keep parent PR #17 OPEN/DRAFT/HOLD until all parent DoD and independent release gates pass.



## 2026-11-17 — H05e PostgreSQL worker log assertion false positive; structured PII gate corrected

- Failed exact-SHA runtime: `323a121dbacdc39d3dac3b0aaa13570b3ef83f30`. SQLite regression and PostgreSQL 17 provisioning passed; the PG worker contract reached H05e lease fencing, DB-finalization recovery, telemetry snapshot and metric-cardinality markers before failing `F2B_H05E_WORKER_LOGS_NO_PII`.
- Root cause is in the test oracle, not evidence of a worker data leak: the old assertion rejected any rendered log message containing the substring `Email`, while the required stable event IDs themselves contain `IDENTITY_EMAIL_OUTBOX_...`. The assertion therefore rejected the expected safe event names.
- Correction: capture each worker log's rendered message, structured property names/values and exception text. The gate now rejects PII-bearing property names, labelled sensitive fields, actual synthetic user IDs/emails, generated delivery payloads/tokens, scoped DB connection strings and release scope token. It still requires both transport-failure and worker-cycle-failure event IDs.
- This is a test-observability correction; production worker log templates and behavior are unchanged. No weakening or deletion of the no-PII acceptance gate.
- Corrected exact-SHA runtime and Independent Postflight are pending. Do not claim H05e PASS until the complete runner, cleanup and Independent Postflight pass at one exact SHA. PR #56 remains Draft; Issues #50/#55 remain OPEN; release HOLD.


## 2026-11-17 — O4-H01 readiness failed on feature-disabled abuse storage; corrected

Operator runtime reached O4-H01 Production TestServer after O3 PostgreSQL 17, TLS VerifyFull, non-elevated roles and fake-transport worker receipts passed. H01 then failed at `F2B_O4_H01_PRODUCTION_HTTP_READY` with SQLSTATE `42501 permission denied for table IdentityAbuseCounters`.

RCA: H01 intentionally runs the default-off public-account configuration as `ys_o3_runtime`, which has no account-abuse table grant. `/health/ready` called `ValidateRuntimeReadinessAsync`, which unconditionally selected from `IdentityAbuseCounters`. The table's full CRUD privilege gate is already separately enforced at startup only when `PublicAccounts:Enabled=true`. Therefore the readiness query imposed a disabled feature's permission requirement on the generic runtime role.

Source correction:
- Runtime readiness now receives the explicit `publicAccounts` switch.
- Abuse-counter schema/data probe runs only when public accounts are enabled.
- H01 test explicitly proves the runtime role has no SELECT privilege on `IdentityAbuseCounters` while the feature is off, then requires `/health/ready=200`.
- Runner and Independent Postflight require the new marker and manifest entry.
- H03's full SELECT/INSERT/UPDATE/DELETE requirement remains unchanged for the enabled public-account identity runtime role.

No broad grants, role expansion, real DB access or production changes. Exact-SHA rerun and Independent Postflight are pending; PR #56 Draft, Issues #50/#55 OPEN, release HOLD.


## 2026-11-17 — H02 H05e DB-outage probe RCA and correction

The operator rerun advanced through O3 and O4-H01 to `h02-production-http`, then failed at `F2B_H05E_DB_UNAVAILABLE_READINESS_503_LIVE_200`. Prior H05 DataProtection sentinel-loss/corruption recovery markers in the same H02 contract had passed.

**Root cause is in the fault-injection test, not the Production readiness endpoint:** the fixture revoked `CONNECT` from `ys_h02_identity`, but PostgreSQL does not terminate existing sessions when CONNECT is revoked. Npgsql pooling could reuse an authenticated session, so the simulated outage did not guarantee that the API had to open a new connection.

**Test-harness correction committed:**
- The TestServer factory supplies an explicitly owned `NpgsqlDataSource` to EF Core `DirectoryDb`, using the same TLS VerifyFull connection string and runtime identity.
- Revoke CONNECT and call `NpgsqlDataSource.Clear()` on the exact EF-owned pool; do not rely on `NpgsqlConnection.ClearAllPools()` for a separately managed EF data source.
- Wait for zero remaining `ys_h02_identity` sessions in `pg_stat_activity`.
- Prove a fresh non-pooled identity connection is denied with SQLSTATE `42501`.
- Then require `/health/ready=503` while `/health/live=200`.
- Restore CONNECT, clear the same data source, and require `/health/ready=200`.
- Runner and Independent Postflight require the EF-data-source-cleared, session-drained and new-connection-denied markers plus manifest `H05E_DB_UNAVAILABLE_PROBE=PASS_CONNECT_REVOKED_EF_DATASOURCE_CLEARED_SESSIONS_DRAINED`.

Superseding source/test contract commit: `e15ad690d2519879f769124b6b28cdc67466ef9e`. Runner marker/manifest commit: `77cacf580e03630b62a808926b7f5ecbd6ae2c7d`; Independent Postflight commit: `0ec6637acb94d4b30613b48c7f93e66fc0f2f6cd`.

**Current acceptance boundary:** H01 stage was passed far enough for the ordered runner to start H02; H02 remains runtime HOLD until a clean exact-current-HEAD full run and Independent Postflight pass. PR #56 remains Draft/Open; Issues #50/#55 remain Open; release HOLD. No production DB, CT164, real SMTP, deployment or GitHub Actions.


## 2026-11-18 — H02 H05e CONNECT revocation grantor RCA

The latest operator rerun passed O3 PostgreSQL worker and O4-H01 Production TestServer. H02 passed DataProtection recovery, cleared the exact EF-owned Npgsql data source and drained all identity-role sessions, but failed the fresh connection denial marker.

**Root cause:** the O3 setup granted CONNECT to `ys_h02_identity` as PostgreSQL superuser `postgres`, while H02 later attempted to revoke it as `ys_o3_migrator`. The migrator was not the grantor or database owner and did not hold CONNECT WITH GRANT OPTION. PostgreSQL therefore left the original superuser grant effective. The fresh non-pooled connection correctly succeeded. This is a disposable test ACL/grantor mismatch, not a production readiness failure.

**Correction committed in PR #56:**
- Grant CONNECT WITH GRANT OPTION to the disposable migrator and ordinary CONNECT to the O3 runtime role; do not grant identity CONNECT from postgres.
- H02 grants identity CONNECT through the migrator before validating the identity connection, so the migrator owns the grant and can later revoke it.
- After REVOKE, query `has_database_privilege(..., 'CONNECT')` and require false before clearing EF's exact `NpgsqlDataSource`, draining sessions, and asserting a fresh non-pooled connection is rejected with SQLSTATE `42501`.
- Runner and Independent Postflight require `F2B_H05E_DB_UNAVAILABLE_CONNECT_PRIVILEGE_REVOKED` and manifest `H05E_DB_UNAVAILABLE_PROBE=PASS_CONNECT_REVOKED_ACL_VERIFIED_EF_DATASOURCE_CLEARED_SESSIONS_DRAINED`.

Source/test correction: `ed45175a108ff41bee9201d40a4467ac329d1c4b`. Runner: `c6be324e3d458e4b159607f7ca1ba7ba57b9d0b7`. Independent Postflight: `f1e8e0739ffed25bad90886406a24e24f4d448ff`.

**Acceptance boundary:** H02 remains HOLD until a clean full exact-SHA runner, cleanup and Independent Postflight pass. PR #56 remains Draft/Open; Issues #50/#55 remain Open; release HOLD. No production DB, CT164, real SMTP, deployment or GitHub Actions.


## 2026-11-18 — F2b H06.1 clean-source browser acceptance PASS

Exact application/test source accepted: `65e9a64f0aa2a1d227f9cc790ffd87d0318e3a3f` on `feat/yangspace-f2b-h06-1-account-browser-e2e-20261118`.

Operator evidence from a newly-created detached worktree:
- Exact SHA checkout: PASS; initial `git status --porcelain --untracked-files=all` empty.
- `npm ci --no-audit --no-fund`: PASS (243 packages). npm emitted an `install-scripts` policy warning for four packages whose lifecycle scripts are not covered by `allowScripts`; installation completed and no package script approval was added as part of this QA.
- TypeScript `npm run typecheck`: PASS.
- Angular production `npm run build`: PASS; initial bundle 470.67 kB raw / 109.72 kB estimated transfer.
- Focused Account Playwright: **20/20 PASS**.
- Full Playwright suite: **46/46 PASS**.
- Final `git status --porcelain --untracked-files=all`: empty.
- Evidence directory: `D:\\GitHub\\YangSpaceApp-QA-Evidence\\H06.1\\65e9a64f0aa2a1d227f9cc790ffd87d0318e3a3f`.

The previous dirty worktree's pre-existing `apps/web/angular.json` change was preserved separately as a patch; it was not carried into the clean QA worktree. The earlier 19/20 and 45/46 diagnostic run is superseded by this exact clean-source 20/20 and 46/46 result.

**H06.1 frontend QA acceptance: PASS.** PR #59 is moved to Ready for Review. This is not a production release authorization: public registration and real email dispatch remain OFF; no deployment, production DB, CT164 or real SMTP access occurred. Issue #58 remains open until PR review and merge are complete.

Operator supplied console evidence in the project conversation. Raw log files remain on the operator workstation; a consolidated SHA-256 evidence manifest has not yet been supplied to GitHub and must be retained with the local evidence directory.


## 2026-11-19 — H06.2-B public object detail source implementation (QA pending)

- H06.2 umbrella: [Issue #60](https://github.com/jvalkovv/YangSpaceApp/issues/60); implementation: [Draft PR #61](https://github.com/jvalkovv/YangSpaceApp/pull/61), branch `feat/yangspace-f2b-h06-2-public-product-ux-20261118`, base `feat/yangspace-f2b-public-identity-foundation-20261113`.
- H06.2-A homepage highlights reliability is committed at `e928b6c998ceec7e349c9fde48fb2588bca097cf`; prior local exact-SHA QA was reported PASS (targeted 6/6, full Playwright 52/52, typecheck/build and clean worktree).
- H06.2-B object detail implementation is committed on the branch: explicit loading/success/not-found/unavailable states; 404 distinct from transient 5xx/network errors; current-slug retry; stale-route request cancellation via `switchMap`; image-aware hero and category fallback; verified/unverified profile status; offer/brochure/job empty and populated sections; contact actions; responsive detail layout and reduced-motion behavior.
- Added `apps/web/e2e/detail.spec.ts` for populated/empty content and stale route-response protection. `apps/web/e2e/detail-navigation.spec.ts` covers loading, 503 retry without stale payload leakage, network retry, 404 distinction, navigation cancellation and mobile Escape/focus restoration. `apps/web/e2e/product-responsive.spec.ts` covers detail and PlatformAdmin preview/editor at all ten required widths.
- Implementation note: `docs/H06_2_OBJECT_DETAIL_20261119.md`.
- **Current implementation head before this SSOT update:** `3bbbf2dae8d9f747a6c8927d6d90ce031c2d99c7`. Source is committed; exact-SHA typecheck/build/Playwright and owner browser review are **PENDING**. Do not claim H06.2 runtime acceptance yet.
- No API/backend contract, H01–H05, migrations, production DB/key-ring, CT164, SMTP, registration/dispatch switches, Owner grant, merchant evidence intake, deployment, ingress or GitHub Actions changes. Public registration and email dispatch remain OFF.
## 2026-11-19 — H06.2-C public directory implementation/tests committed (QA pending)

- Added `apps/web/e2e/directory.spec.ts` for name search, category + neighborhood composition, successful empty results, 503 retry with query preservation, stale-search suppression and pagination.
- Expanded homepage and populated-highlight responsive coverage to 320/360/375/390/430/768/820/1024/1280/1440px. Object detail and PlatformAdmin preview/editor are covered at the same ten widths in `product-responsive.spec.ts`.
- Updated `scripts/Test-H06-2-Web-Acceptance.ps1` so the targeted Playwright gate includes the directory and object-detail suites.
- Detail loading announcement and 404 copy were aligned with the accepted navigation suite; 503 tests also assert stale profile payloads never render.
- Documentation: `docs/H06_2_DIRECTORY_20261119.md` and `docs/H06_2_OBJECT_DETAIL_20261119.md`.
- Current implementation head before this SSOT update: `3bbbf2dae8d9f747a6c8927d6d90ce031c2d99c7`. Exact-SHA typecheck, production build, Playwright and owner browser review remain **PENDING**; no runtime acceptance is claimed.

## 2026-11-19 — H06.2 integrated implementation checkpoint (exact-SHA QA pending)

- Parent: [Issue #60](https://github.com/jvalkovv/YangSpaceApp/issues/60); [Draft PR #61](https://github.com/jvalkovv/YangSpaceApp/pull/61). Target branch remains `feat/yangspace-f2b-public-identity-foundation-20261113`, not `main`.
- H06.2-A: homepage highlights loading, genuine empty and unavailable/retry states implemented; operator-reported local QA at `e928b6c998ceec7e349c9fde48fb2588bca097cf`: focused 6/6, full 52/52, TypeScript/build PASS.
- H06.2-B: Object Detail redesigned with visible state-specific layouts (loading, success, not-found, transient unavailable/retry), fallback media, verification note, publications/contact blocks and Bulgaria civil-time formatting. Historical route/date/publication copy assertions remain intact; nested `main` landmark removed and loading status remains announceable while the outer page is busy.
- H06.2-C: directory/search/filter/pagination workflows retain anonymous contracts; standalone catalogue E2E plus integrated visitor navigation tests added. Mobile menu supports Escape from the toggle/nav and returns focus to the toggle.
- H06.2-D: homepage and offers/brochures/jobs at ten widths, populated Detail and authorized preview Admin/editor at ten widths, with clipping/overflow assertions and screenshots.
- QA operator: `scripts/Test-H06-2-Web-Acceptance.ps1` (PowerShell 5.1-safe native exit handling, exact local/remote SHA, typecheck/build, targeted/full browser E2E, SHA-256 evidence); `scripts/Verify-H06-2-Web-Postflight.ps1` (independent checksum/markers/source/remote checks).
- See `docs/H06_2_PRODUCT_UX_ACCEPTANCE_20261119.md` for complete contract and manual screenshot checklist.

**Implementation is source-committed, but integrated exact-head runtime QA, visual acceptance and independent postflight have NOT passed yet.** Keep PR #61 Draft, Issue #60 OPEN and product release HOLD. No production, backend H01–H05, public account/SMTP, infrastructure or package-policy activation changes.


## 2026-11-19 — H06.2-D PlatformAdmin preview visual consistency (source committed / QA pending)

- Added an operator-focused responsive style layer for the existing isolated Preview Console: clearer workspace hierarchy, form control sizing, panel spacing, list/action treatment, focus states and tablet/mobile stacking.
- The change is CSS-only. Admin endpoints, bearer authorization, preview-only warning, publication semantics and production boundaries are unchanged.
- Existing `product-responsive.spec.ts` covers the authorized mock PlatformAdmin workspace and editor at all ten responsive widths.
- Existing `public-journey.spec.ts` covers anonymous directory filter → object detail → catalogue return; the acceptance runner includes it.
- Current implementation head before this SSOT update: `3bbbf2dae8d9f747a6c8927d6d90ce031c2d99c7`. Exact-SHA typecheck/build/targeted/full Playwright and independent postflight remain **PENDING**. PR #61 remains Draft/HOLD.

## 2026-11-19 — H06.2 accessibility browser checks added (QA pending)

- Added `apps/web/e2e/accessibility.spec.ts`: first keyboard stop/visible skip-link focus, named directory search/filter controls, `aria-pressed` category state, single main landmark and reduced-motion behavior for detail loading.
- Added `admin-cms.spec.ts` to the targeted exact-SHA acceptance suite so PlatformAdmin authorization denial and preview CMS mutation contracts run in the focused gate as well as the full suite.
- Current source head before this SSOT update: `3bbbf2dae8d9f747a6c8927d6d90ce031c2d99c7`. Typecheck, production build, targeted/full Playwright, manual browser review and independent postflight remain **PENDING**.

## 2026-11-20 — H06.2 build correction and interactive browser QA package (runtime QA pending)

- Operator exact-SHA result at `51a9059d4a520b21d40f0279b536f0ef11b07983`: npm CI PASS (exit 0, with non-fatal install-scripts warnings), TypeScript typecheck PASS, Angular production build FAIL. Root cause: Angular typed the template keyboard event as `Event`, while `onMenuEscape` unnecessarily required `KeyboardEvent`. Playwright did not run after the build gate.
- Source correction: `onMenuEscape(event: Event)`; retains `preventDefault`, `stopPropagation`, menu close and focus restoration. Playwright config only reuses an existing dev server when the launcher explicitly sets `H06_2_REUSE_SERVER=1`; default isolated runner behavior remains unchanged.
- Added `apps/web/qa/mock-api.mjs`: localhost-only (127.0.0.1:5090), fictional in-memory demo data, search/detail and Preview Admin fixtures, no DB/filesystem persistence/SMTP/external services. Demo account and fake bearer token are local-only; public account flows are disabled.
- Added `scripts/Start-H06-2-Browser-QA.ps1` as the single entry point. It checks a clean worktree, safely fetches/fast-forwards only, resolves exact SHA, runs syntax/npm/typecheck/production build, starts mock API and Angular preview, opens the browser, waits for Enter, then runs focused and full headed Chromium Playwright, captures evidence, stops only its own processes and runs independent postflight.
- Extended `scripts/Test-H06-2-Web-Acceptance.ps1` with the interactive headed mode and mock API syntax gate; `scripts/Verify-H06-2-Web-Postflight.ps1` now requires that gate. The preview process cleanup call was statically checked against its `-ProcessSet` parameter.
- Operator guide: `docs/H06_2_BROWSER_QA_LAUNCHER_20261120.md`; integrated acceptance record updated in `docs/H06_2_PRODUCT_UX_ACCEPTANCE_20261119.md`.
- Implementation head at this checkpoint before this SSOT update: `3fd2a648c7037602f58d55083217b7ab52f09ca3`. The correction, local preview package and docs are committed; exact-SHA build, headed targeted/full Playwright, manual visual review and independent postflight are **PENDING**. PR #61 remains Draft/HOLD; Issue #60 remains OPEN.
- No production deployment, backend H01–H05 contract, production DB/key-ring, CT164, real SMTP, public registration/email dispatch, Owner grant, merchant evidence intake, DNS/proxy/ingress or GitHub Actions changes.


- Launcher hardening: the runner clears any inherited `H06_2_REUSE_SERVER` override before execution, enables it only for the process it starts, and restores the prior environment value after shutting down its own preview processes.
- Local API smoke gate checks the public homepage fixture, public object detail, local fake PlatformAdmin login/role, admin object list and publication content endpoint; no credentials or token are written to the smoke log.


- Additional QA hardening after the initial launcher checkpoint: detail loading E2E now asserts only the loading card disappears while the three intentional empty-state status regions remain; native stdout/stderr are captured separately to avoid PowerShell native-stderr promotion; evidence timestamps include milliseconds; launcher preserves the evidence directory and prints the first failed gate log even when acceptance fails.


## 2026-11-20 — H06.2 headed browser run passed; Postflight harness corrected

Operator exact-SHA run at `545674d60293c29cf7ec0e0abd43a5b5d0bf5a93`:
- GitHub sync/exact SHA: PASS.
- Mock API syntax + five endpoint smoke checks: PASS.
- npm CI: PASS (exit 0; four non-fatal npm install-scripts policy warnings remain informational).
- TypeScript typecheck + Angular production build: PASS.
- Targeted headed Playwright: **71/71 PASS**.
- Full headed Playwright: **103/103 PASS**.
- Source clean post-run: PASS.
- Evidence manifest SHA-256: `5d6a03972c38a1741b522acb4c6ed6b82670bf274342d60d2cba2a2f87c452d6`; all 123 file hashes in the operator-provided archive independently matched.

Postflight initially failed because comma-separated string concatenation inside PowerShell array expressions collapsed required markers into one string; the evidence producer also joined the commit and branch header markers. Corrected in the H06.2 branch:
- Producer now writes runtime, run-summary and manifest records using explicit `List[string]` entries.
- Postflight constructs required markers explicitly and normalizes the legacy combined commit/branch header.
- All integrity, artifact, exact-source, clean-worktree and remote-SHA checks remain required.

**Current acceptance: application build and 174 Playwright test executions PASS; corrected independent Postflight and owner visual acceptance remain pending at the new exact HEAD.** PR #61 remains Draft/Open; Issue #60 remains Open; release HOLD. No production deployment, backend activation, account registration or email dispatch is authorized.


## 2026-11-20 — H06.3 Public Discovery UX implementation

- Issue: #62.
- Branch: `feat/yangspace-f2b-h06-3-public-discovery-ux-20261120`.
- Stacked base: H06.2 branch at `080602b4a3eaa209f4045d4232495348512be06d` (PR #61 remains Draft / owner visual sign-off pending).
- Current implementation HEAD: `b997fbb88e4305d6dd23f094a355bff5d611b4d5`.

Committed frontend work:
- Move the primary directory search directly below the pilot notice and before categories/results; hero CTA targets the search.
- Reduce mobile hero vertical footprint while retaining the existing green/cream identity and desktop/tablet map artwork.
- Render the existing `imageUrl` field on directory cards with lazy loading, alt text and deterministic category fallback after absent/broken images.
- Display a restrained verified badge only for `isVerified=true`.
- Detect contradictory homepage counters (active offers/jobs advertised by directory cards but empty highlight collection) and show an explicit inconsistent/unavailable state with retry rather than a false empty state.
- Correct synthetic Playwright fixtures so Homepage counters/highlights and Object Detail offer data agree.
- Add `e2e/discovery-ux.spec.ts` for mobile ordering, image success/fallback, verification cue and inconsistent payload recovery.
- Add a dedicated exact-SHA H06.3 browser QA launcher, runner and independent postflight. Targeted tests include the new discovery spec.

No backend/API contract, DB, package manifest, dependency, account activation, SMTP, production deployment or infrastructure changes.

**Status: IMPLEMENTATION COMMITTED; RUNTIME QA PENDING.** The H06.3 Windows package has not yet been executed at this exact HEAD. PR must remain Draft and stacked on H06.2. No merge/deploy until exact-SHA QA and owner visual review pass.


## 2026-11-20 — H06.3 QA harness correction

The first H06.3 browser QA attempt completed mock API syntax/smoke, npm CI, Chromium installation, TypeScript typecheck and production build. Targeted Playwright did not execute: Playwright tried to start a second preview server on port 4300.

Root cause: `scripts/Test-H06-3-Web-Acceptance.ps1` set `H06_3_REUSE_SERVER=1`, but `apps/web/playwright.config.ts` still read the H06.2 variable `H06_2_REUSE_SERVER`.

Corrective commits:
- `adb77e7ef0c83931ffbb8bea0523d45c4ba953fb`: align Playwright config with H06.3 runner.
- `00a13be163f899cfa849a2882d2fdb149e271427` and `1ac7b34d561dd1083dc2343c106c268a631a3dc4`: add a pre-test runner contract guard.
- `6d0dc67a3c7016cf2eca23c280d1f565bb056be0`: require `PLAYWRIGHT_REUSE_CONFIG=PASS` in independent evidence postflight.

Previous evidence is preserved at `D:\\GitHub\\YangSpaceApp-QA-Evidence\\H06.3\\df705458a77861b5fe88226d774a7a77bfa92d79-20261120-072246-658`, manifest SHA-256 `7394897063e5b7f350e906fbbf6a20a05883b88ad025cc0cf8f5f4b4fbe44549`.

The first attempt is a QA harness configuration failure, not an application test failure. Runtime acceptance remains **PENDING**. Re-run from the existing clean H06.3 worktree; the launcher fetches and fast-forwards to the latest exact remote SHA. Do not create another worktree or kill unrelated port owners. No merge or deployment is authorized.


## 2026-11-20 — H06.3 Playwright findings and test correction

At exact SHA `68eadb43c729ec01a6d27aa3f4b2d214c76b0`, QA passed mock API syntax/smoke, Playwright reuse contract, npm CI, TypeScript and production build. Targeted Playwright executed 76 tests: 62 passed / 14 failed. Full suite and independent postflight did not run.

Root causes mapped to the failing test specs:
- DOM-order test used `classList[0]`, incorrectly assuming the first class token identifies `.quick-discovery`; fixed to test class membership.
- “Genuine empty” homepage fixture had non-zero active publication counters with empty highlight arrays; app correctly reported inconsistency. Fixture now zeros the counters.
- Public journey and 10 responsive assertions matched both a directory card and an offer link to the same business; selectors now scope to `.location-card`.

No application runtime defect was established by these failures; acceptance is still pending.

Test corrections are committed at `ef5e1e78cd7b5e47aa56e974e73ad6a3ca540e54`; subsequent documentation commits advanced the branch tip. Re-run the existing clean worktree using the H06.3 launcher, which fetches the current remote branch HEAD automatically. Targeted Playwright, full Playwright and independent postflight must all pass before owner visual review or merge/deploy. PR #65 remains Draft; release HOLD.
