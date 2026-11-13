# YangSpace 2.0 — Execution Tracker

> **Current gates and ordering:** [SSOT.md](SSOT.md). Keep this file as the detailed task-ID/evidence ledger; dated rows record their own acceptance scope and may be older than the SSOT snapshot. Do not promote a task to DONE from a newer source commit without fresh exact-SHA runtime evidence. CT164 guest preparation is not an activated application release.

**Tracked source baseline:** `20aa30c7da581316ba31900b6953025a3d1f3b00` (homepage code) · **visual QA docs:** `e2edefb70c648199ba8e82f83d7a3396424c88c4`.  
**Branch:** `feat/yangspace-v2-foundation-20261108` · **PR:** [#1](https://github.com/jvalkovv/YangSpaceApp/pull/1) (Draft).  
**Status values:** DONE = proven, PARTIAL = source present without full gate, TODO = not built/verified, BLOCKED = unmet dependency, HOLD = intentionally deferred.  
**Governance:** evidence is required before marking DONE. Do not infer progress percentages from file counts.

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
- Parent [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) remains OPEN/DRAFT/HOLD; V2 [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) remains OPEN/DRAFT/HOLD. No CT164/CT152, live DB, real evidence, GitHub Actions or production deployment.


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

## 2026-11-12 13:41 — F2a combined HEAD exact-SHA QA accepted

| Item | Result | Evidence / remaining dependency |
| --- | --- | --- |
| #17 combined tenant regression | **DONE — exact-SHA operator PASS twice** at `445db4d9a5d28b798054c426afeba9b9be2c771b` | `f2a-restore`, `f2a-build`, `f2a-tenant-scope` PASS, final tenant marker PASS; latest evidence `logs/yangspace/f2a-tenant-scope/445db4d9a5d2-20261112-134120-cc109012` in the operator-local QA worktree |
| #17 disposable PostgreSQL schema/constraints/recovery | **DONE — exact-SHA operator PASS twice** | Model-diff and SQL hash PASS; migration apply, FK/partial unique, CAS, DB audit rollback, backup/restore PASS; latest evidence `logs/yangspace/pg-disposable/445db4d9a5d2-20261112-134126-299426c5`. Local manifests/SQL/backup and container cleanup require independent SHA-256 postflight. |
| #19 actual Identity bearer tenant HTTP | **DONE — scoped SQLite/TestServer; merged/closed** | Tested code SHA `f579c70db9cc0986d2f1df8047158587f0e87eef`; no claim of real PostgreSQL bearer HTTP. |
| [#20 F2a-3 reviewer claim/membership lifecycle](https://github.com/jvalkovv/YangSpaceApp/issues/20) | **CONTRACT PROPOSED; implementation NOT STARTED** | Confirm reviewer role, self-approval denial, claim state machine, ownership transfers, membership re-grant, last-owner policy, transaction boundaries, audit and fault injection. |
| #17 PR + V2 #1 | **OPEN / DRAFT / HOLD** | PostgreSQL bearer HTTP/parallel writers, reviewer/mutation workflow, atomic application audit, least-privilege PG role and recovery/release gates outstanding. |

**No CT164/CT152 operations, live DB writes, merchant activation, deployment or GitHub Actions.**

## 2026-11-12 08:15 — F2a-2 read-only bearer authorization accepted

| Work package | Result | Evidence / next required gate |
| --- | --- | --- |
| F2a-1 domain + PostgreSQL schema foundation / [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) | **PARTIAL / DRAFT / HOLD**; current combined head \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` | Earlier disposable PostgreSQL migration/constraints/backup-restore PASS is exact-SHA scoped to \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`; rerun source/build + disposable PostgreSQL regression on combined head. Then real PG bearer/parallel writer, reviewer claim lifecycle, atomic application audit/fault injection, least-privilege role and off-host restore. |
| F2a-2 read-only Identity bearer authorization / [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) | **DONE scoped acceptance; MERGED/CLOSED** via squash \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` into the F2a-1 feature branch | Exact tested code SHA \`f579c70db9cc0986d2f1df8047158587f0e87eef\`: Windows restore/build/tenant regression/real Identity bearer TestServer PASS; operator evidence postflight PASS. Manifest SHA-256 \`336D8A9DCB4C69483981A2A5124A5123E8AD9044C14A1619199439E2E121F366\`. Disposable SQLite/TestServer only; no production or PG bearer acceptance. |
| [F2a-3 reviewer claim decision + membership lifecycle — Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) | **NOT IMPLEMENTED / contract next** | Confirm dedicated reviewer authority, no self-approval, claim state machine, Owner grant/revoke, atomic ownership+membership+audit transaction, concurrency/unique conflicts, fault-injection and recovery before coding. |
| F2b owner CMS mutations + per-field audit / Issue #11 | **NOT IMPLEMENTED / BLOCKED by F2a** | Real PostgreSQL transactional API tests, expected-revision/409, field allowlist, one audit event per accepted change; no owner write endpoints yet. |
| V2 parent / [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) | **DRAFT / OPEN / HOLD** | No V2-to-main merge, merchant activation, production rollout or pilot approval. |

**PR inventory at this checkpoint:** #19 MERGED/CLOSED; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD. No other open PR is 100%-accepted for closure. No GitHub Actions, CT164/CT152, live DB changes or deployment.

## F2a start — 2026-11-11: authorization/audit design gate

| Work package | Result | Next required evidence |
| --- | --- | --- |
| YS-S2-01 / ADR-019 production provider | **Target PostgreSQL ACCEPTED; rollout HOLD** | [ADR-019](ADR-019-POSTGRESQL-PRODUCTION-TARGET.md): operator topology, PG-specific EF migrations, backup/restore, and DB hardening not yet done |
| YS-S2-04 / Issue #4 | **F2a contract PROPOSED** | [F2a ownership and audit proposal](F2A_TENANT_OWNERSHIP_AND_AUDIT_CONTRACT_20261111.md); no schema migration/merchant endpoints |
| YS-S2-05 / tenant access | **NOT IMPLEMENTED** | Real bearer owner A↔B 404/no-leak negative tests, grant/revoke, old-token denial |
| Issue #11 F2b / audit + concurrency | **NOT IMPLEMENTED** | Transactional one-event-per-change and per-resource revision 409 tests |
| F3 owner confirmation/moderation | **BLOCKED by F2** | Reviewed membership/actor source and valid confirmation event |

**No active CT164 DB or public endpoint changes.** This is a status snapshot describing a proposed design; no acceptance of the provider ADR or security controls is implied.

## 2026-11-11 18:22 — F1 freshness delivery checkpoint (current)

The **current V2 application source** after merged [PR #14](https://github.com/jvalkovv/YangSpaceApp/pull/14) is `403b281abad2fb0c8048c2763a606c3435f3e5dd` (squash SHA, not separately QA-run). The exactly tested F1b source is `2634014f64fcff237b29564d0572643f2cba034a`. This checkpoint supersedes the 07:xx source snapshot below **for current workstream status only**.

| Task / requirement | Result | Evidence and boundary |
| --- | --- | --- |
| Issue #11 F1a — truthful public freshness | **DONE scoped source/QA**; PR #13 **MERGED/CLOSED** | Candidate `9656518...`: source+API tests PASS; squash `54b47576...`; no live deploy |
| Issue #11 F1b — nullable publication lifecycle timestamps | **DONE scoped source/QA**; PR #14 **MERGED/CLOSED** | Candidate `2634014...`: Source+Playwright, real API/security, EF initial→F1b upgrade and recovery all PASS on disposable DB; 9 nullable historical columns; squash `403b281...` |
| Local F1b verify evidence | **PASS at candidate SHA** | `logs/yangspace/verify/2634014f64fc-20261111-181828-f2898981`; `logs/yangspace/api-integration/2634014f64fc-20261111-181923-5dbe2df0`; `logs/yangspace/sqlite-runtime-integrity/2634014f64fc-20261111-181244-81cbfb42` (under isolated operator worktree) |
| Issue #11 F2/F3 — owner-scoped field audit & confirmation | **NOT STARTED** | Requires Organization/Member claims and negative cross-tenant tests; no real owner CMS entitlement |
| Parent PR #1 and public pilot gates | **DRAFT/OPEN; HOLD** | No CT164 active DB migration, release, off-host recovery/ingress acceptance; current public demo still older release `a480280...` |

The previous checkpoint below is retained as historical evidence; do not use its old `2baeb797...` SHA or open-PR list as the current status.

## 2026-11-11 latest verified source QA & PR hygiene

This checkpoint **supersedes older exact-SHA source-QA TODO entries for engineering validation only**, without changing the historical rows below. It does not mark the full S0/security/release/production gate DONE.

| Item | Evidence | Scoped result |
| --- | --- | --- |
| Combined V2 feature commit `2baeb797b4d9ca0f5ce9d6c3d311a7b8092be7dc` | Owner confirmed fetched SHA and ran `Verify-Source-Local.ps1 -CommitSha` and `Verify-Api-Local.ps1 -CommitSha` on **same** SHA | `SOURCE_VALIDATION=PASS`, `REAL_API_INTEGRATION=PASS`; disposable DB only |
| Frontend/typecheck/browser/restore/build | Source runner stages `web-lock`, `web-install`, `web-audit`, `web-typecheck`, `web-build`, `web-playwright`, `api-restore`, `api-build` all `STAGE_PASS` | **PASS** for exact SHA |
| Real auth/API integration | API runner `api-prepare`, `api-restart-verify`, `api-bootstrap-denial`, `api-post-bootstrap-verify`, `api-post-bootstrap-recovery` pass | **PASS**; escalation denied, non-admin role retained |
| Local operator artifacts | `logs/yangspace/verify/2baeb797b4d9-20261111-074951-04b1acd1`, `logs/yangspace/api-integration/2baeb797b4d9-20261111-075201-77648186` on operator Windows PC | Local logs **reported**, not Git-tracked |
| Completed child PRs | [#9](https://github.com/jvalkovv/YangSpaceApp/pull/9) and [#10](https://github.com/jvalkovv/YangSpaceApp/pull/10) | **MERGED/CLOSED**; [parent #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) stays **Draft/Open** |
| Business freshness requirement | [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11) | **OPEN** for implementation; not part of completed UI fixes |
| Operational gates | Current live demo reported at `a480280...`; no fresh exact-candidate activation, off-host restore, verified effective firewall, live merchant auth/audit | **HOLD** |

**Ongoing owner requirement:** provide concise evidence-linked progress at each meaningful checkpoint (plus existing hourly GitHub change watch); distinguish source QA from runtime deployment, and merge/close only fully scoped accepted/owner-approved PRs. Do not close unmerged PRs for list hygiene. Canonical policy: [MVP roadmap](MVP_ROADMAP.md#delivery-governance--mandatory-progress-reporting--pr-closure-accepted-2026-11-11).

## 0. Evidence baseline (confirmed, no rerun by this document)

| ID | Item | Status | Evidence / qualification |
|---|---|---|---|
| YS-B-01 | Homepage green/cream design and visual acceptance | DONE | 375/390/768/1024/1440 baseline; post-fix screenshot review, docs/HOMEPAGE_ACCEPTANCE.md |
| YS-B-02 | Local Angular typecheck and production frontend build | DONE | Windows console output, 2026-11-09 |
| YS-B-03 | Local API + frontend + proxy operational | DONE | `LOCAL_STACK=PASS`, 5 HTTP checks |
| YS-B-04 | Automated browser regressions | DONE | 17/17 Playwright PASS on `20aa30c`, 18.2s; mocked API |
| YS-B-05 | Public read-only demo API | PARTIAL | 6 fictional locations/3 offers/1 brochure/1 job; no real public-data acceptance |
| YS-B-06 | Preview administrator UI + API | PARTIAL | Source present; Playwright auth test mocks backend; real CRUD lifecycle unverified |
| YS-B-07 | Manual source and API validation | DONE at `b30c1c8` | Exact same SHA source 8/8 PASS, API real integration and bootstrap non-escalation PASS; operator logs `logs/yangspace/verify/b30c1c819aa1-20261109-160220-9e00ef05` and `logs/yangspace/api-integration/b30c1c819aa1-20261109-160130-c608197c` |
| YS-B-08 | PVE LXC deployment | PARTIAL | CT164 created/started by operator on 2026-11-10 (Debian 13.6, DHCP 192.168.1.110, 2 vCPU/4 GB/20 GB); app/release, security, recovery and proxy remain HOLD. See docs/CT164_PROVISIONING_20261110.md |

## S0 — Engineering reproducibility and risk closure (START HERE)

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S0-01 | P0 | Record baseline, source/QA scope, branch protection and current blockers | PR #1 has precise accepted homepage SHA; release gates and documentation agreed | DONE |
| YS-S0-02 | P0 | Freeze dependency graph | Commit lockfile; validate npm ci from a clean SHA archive, dependency audit and .NET restore | DONE for verified `133cfd6`: committed lockfile, clean Git archive, npm ci, audit and .NET restore/build all PASS; source, npm ci, audit and .NET verified again at `6e8ba7b`; revalidate next HEAD |
| YS-S0-03 | P0 | Manual reproducible source QA | `Verify-Source-Local.ps1` on exact SHA: npm ci, npm audit, Angular build, 17+ Playwright, .NET restore/build and logs | DONE at `b30c1c8`: 8/8 stages PASS, `logs/yangspace/verify/b30c1c819aa1-20261109-160220-9e00ef05`; new release-policy HEAD requires revalidation before build |
| YS-S0-04 | P0 | Real .NET API integration | `Verify-Api-Local.ps1`: isolated temp DB, random loopback port, guest/admin auth, publish/expiry/hide, process restart | DONE at `b30c1c8`: guard, restore/build, real API prepare/restart and bootstrap denial + nonadmin recovery PASS; `logs/yangspace/api-integration/b30c1c819aa1-20261109-160130-c608197c`; new release-policy HEAD needs its own SHA evidence |
| YS-S0-05 | P0 | Security inventory of new & legacy surfaces | Default-deny preview registration, fail-closed bootstrap, legacy secret assessment/rotation, protected preview, ownership/rate-limit plan | PARTIAL — SEC-01 registration deny and SEC-02 nonadmin anti-promotion both runtime PASS at `b30c1c8`; legacy secret reuse/rotation, private preview network boundary and future tenant authorization still open |
| YS-S0-06 | P1 | Correct PVE release candidate policy | Remote-tip SHA, reviewed-source/script identity, verified QA scope, protected reviewer approval bound to two SHA-256 manifest digests; inactive build only | PARTIAL — Windows Git Bash synthetic policy all positive/negative cases PASS at `04b1505` (`SIMULATED_MSYS_NTFS`); actual Linux approval ACL, trusted evidence transfer and private CT release acceptance deferred to dedicated YangSpace LXC |
| YS-S0-07 | P1 | Repo hygiene and legal ownership | LICENSE decision, review/branch policy, pinned SDK/node docs, changelog and manual QA instructions | TODO |

**S0 exit:** S0-02/03/04/05/06 verified by manual evidence, not Actions; no open P0 defect; only then mark G0 PASS. Source code and local browser success alone are insufficient.

## S1 — Authenticated real CMS vertical slice

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S1-01 | P0 | Real preview admin bootstrap and authenticated login | Bootstrap secret protected, no default credentials; admin role grant verified; non-admin denied; register endpoint gated | PARTIAL — backend authorization/SEC-02 previously runtime PASS; Angular login now checks authenticated `/admin/me` PlatformAdmin before enabling CMS; new browser tests prepared, pending container runtime |
| YS-S1-02 | P0 | Location full CRUD + hide/restore | Create/edit/publish/hide/restore persist across restart, validate duplicate slug & bad input, positive/negative API tests | PARTIAL — API restore added and idempotent; editor supports save, hide and restore with draft state; updated disposable-DB API regression and mocked Playwright scenarios pending CT runtime |
| YS-S1-03 | P0 | Offers, brochure links, jobs full CRUD | Admin-only list/create/edit/archive/restore, valid/expired public filtering, status synchronization and negative tenant-scoped path tests; hard delete intentionally forbidden | PARTIAL — S1-03 source implemented with separate content manager and REST endpoints; PowerShell disposable-DB + Playwright regressions prepared, unexecuted; real CT acceptance pending |
| YS-S1-04 | P0 | Admin page production-grade UI | Forms, validation, loading errors, status, edit modals/pages, accessibility + screenshots | PARTIAL — real edit/publish UI, role verified login, validation, conflict handling, request timeout and responsive editor source prepared; visual/keyboard/manual CT acceptance pending |
| YS-S1-05 | P0 | End-to-end unmocked workflow | Admin → DB → GET public → hide → public absent → restart → persistent; negative auth evidence | TODO |
| YS-S1-06 | P1 | Demo fixture isolation | Fictional samples are idempotent in preview; no accidental public real-data migration, environment-switch tests | PARTIAL |
| YS-S1-07 | P0 | Production-safe CMS access and first-admin enrollment | Bearer-only PlatformAdmin, gateway-origin policy, no registration, offline one-time admin grant, approved database schema, explicit security preflight | PARTIAL — source gates/operator CLI/negative startup test prepared; NO migrations, live proxy-ACL review, deployment or runtime proof |

**S1 exit:** admin can complete real DB-backed workflows and an unprivileged caller cannot perform them; logs + screenshots recorded.

## S2 — Domain, production persistence and ownership

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S2-01 | P0 | DB ADR: PostgreSQL vs SQL Server | Written benchmark, license/ops/spatial/backup/restore comparison and selected provider | TODO |
| YS-S2-02 | P0 | EF Core migrations/versioning | Clean init + upgrade + rollback compatibility/drill, no `EnsureCreated` in production path | PARTIAL — pinned local EF Core 10 design-time tool, guarded no-DB factory and exact-SHA PowerShell migration scaffold operator committed; generated InitialDirectorySchema + SQL review + runtime DB apply/rollback still PENDING |
| YS-S2-03 | P0 | Organization/Location/Geo taxonomy | Relational constraints, seeded normalized lookups, categories/hours/geo indexes | TODO |
| YS-S2-04 | P0 | Owner/member/claim workflow | Verified reviewer decision creates scoped membership; revoke works; audit, denial and cross-tenant tests | TODO |
| YS-S2-05 | P0 | Policy-based tenant isolation | Tests for owner A vs owner B across all private endpoints (reads/writes); no global `IsOwner` shortcut | TODO |
| YS-S2-06 | P1 | Data lifecycle and GDPR controls | Retention, subject access/correction, source & verified_at, traceable admin changes | TODO |

## S3 — Merchant CMS, publications and moderation

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S3-01 | P1 | Merchant dashboard/location editor | Organization and branch picker; clear edit/draft/review/publish states | TODO |
| YS-S3-02 | P1 | Offers campaigns & automatic expiry | Draft→review→publish→expire, timezone/UTC edge-case tests, worker reconciliation | TODO |
| YS-S3-03 | P0 | Trusted media upload/preview | MIME sniffing, file limits, safe filenames/object keys, quarantine/scan policy, rights attestation, restore | TODO |
| YS-S3-04 | P1 | Brochures and job postings lifecycle | Verified rights, valid dates, outbound job links, no CV storage; positive/negative tests | TODO |
| YS-S3-05 | P1 | Moderation/admin audit/reporting | Review queue, reasoned approve/reject/suspend, abuse reports, immutable audit trail | TODO |
| YS-S3-06 | P1 | Owner/admin UX acceptance | Real multi-role scenario across 375/390/768/1024/1440px, keyboard tests, screenshot evidence | TODO |

## S4 — Search, SEO and quality

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S4-01 | P1 | Browse/search by neighborhood/category/geo | Accurate filtering, query validation, DB indexes, boundary tests, pagination & dedupe | PARTIAL |
| YS-S4-02 | P1 | Public profile enrichment | Coordinates, map/directions, verified freshness, contacts, opening hours & exceptions | TODO |
| YS-S4-03 | P1 | Dedicated offers/brochure/jobs indexes | Public listing/search pages, published/valid-only, no leakage of hidden entries | TODO |
| YS-S4-04 | P1 | SEO SSR/prerender architecture | Canonicals/meta/structured data/sitemap, indexable public pages, demo/noindex isolation | TODO |
| YS-S4-05 | P1 | Accessibility/performance & analytics | A11y, full visual QA, real browser Core Web Vitals + privacy-aware action analytics | TODO |

## S5 — PVE private preview (after S0+S1)

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S5-01 | P0 | Read-only PVE inventory and topology decision | Available CT ID, OS, IP, storage, backup, gateway/DNS/TLS and resource budget documented | PARTIAL — host+CT+storage+bridge inventoried; actual DHCP .110, reservation/DNS, thin-pool risk, independent backup and gateway ACL still open |
| YS-S5-02 | P0 | Provision isolated LXC and services | Unprivileged where feasible, systemd, Nginx, correct service/deploy user, secrets and private network | PARTIAL — CT164 Debian 13.6 unprivileged guest created and booted; service accounts, network ACL, OS hardening and app services pending |
| YS-S5-03 | P0 | GitHub pinned-SHA controlled release | Exact approved SHA + manual QA manifests, `releases/<sha>`, atomic activation, no auto-pull/restart | BLOCKED |
| YS-S5-04 | P0 | Backup/restore/rollback exercise | Consistent DB/media backup, off-host copy, restore into scratch, bad-release revert, command logs | BLOCKED |
| YS-S5-05 | P1 | Observability/maintenance | Separate API/proxy/deploy/verify logs, health, error-rate/latency/disk alert, restart policy | TODO |
| YS-S5-06 | P0 | Protected preview acceptance | Browser on private TLS/ACL endpoint, persistence after reboot, auth denied to outsiders, smoke PASS | BLOCKED |

## S6 — Public pilot, first neighborhood (after G2/G3/G4/G-PREVIEW)

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S6-01 | P1 | Confirm first neighborhood and pilot outreach | One selected neighborhood, merchant contact process, rights/source register | TODO |
| YS-S6-02 | P1 | Recruit verified merchant content | First consented real businesses + owner verification, no copy/paste of third-party protected materials | TODO |
| YS-S6-03 | P0 | Public security/legal review | Permissions, uploads, privacy, T&C, reports/takedown, promotions rights and accessibility checked | TODO |
| YS-S6-04 | P0 | Production readiness drill | TLS/firewall, migrations, no demo data, restore/RPO-RTO, load, alerts, release audit | TODO |
| YS-S6-05 | P1 | Launch KPI and iteration | Fresh profiles and useful visitor actions, merchant/owner activity and support time measured | TODO |

## S7 — Varna / Bulgaria scale (gated)

| ID | Work package | State |
|---|---|---|
| YS-S7-01 | Multi-neighborhood Varna onboarding, duplicate/quality detection | HOLD |
| YS-S7-02 | Subscription pricing/entitlements and payment ADR after pilot evidence | HOLD |
| YS-S7-03 | National expansion capacity/data governance/operational playbooks | HOLD |

## Current active queue (strict order)

1. `YS-S0-02` — lockfile committed; verify clean exact-SHA npm ci, audit and .NET restore.
2. `YS-S0-03` — run [S0 Manual QA](S0_MANUAL_QA_RUNBOOK.md) with `Verify-Source-Local.ps1` and inspect logs.
3. `YS-S0-04` — run `Verify-Api-Local.ps1` and inspect auth/DB/expiry/restart evidence.
4. `YS-S0-05` — auth/secrets/preview exposure review.
5. `YS-S0-06` — solve Draft PR ↔ PVE approved SHA restriction.
6. `YS-S1-01..05` — finish real content CRUD and workflow.
7. `YS-S5-01` — begin PVE **read-only** inventory once PVE preview scope is authorized.

## Per-commit evidence template

```text
TASK_ID:
BRANCH:
COMMIT_SHA:
CHANGED_FILES:
TEST_COMMANDS_AND_RESULT:
BROWSER_SCREENSHOTS:
API_DB_INTEGRATION_EVIDENCE:
SECURITY_NEGATIVE_TESTS:
RUNTIME_TARGET (local / private PVE / public):
BACKUP_REF (if mutation):
ROLLBACK_PATH:
REVIEWER:
GATE_DECISION: PASS / HOLD
```

**Never mark a task complete because only the code exists.** Record failing tests as BLOCKED with root cause, not as completed with an exception.

### 2026-11-09 — Evidence pair and security hardening state

The operator's API and source logs **both PASS** for `f3f4e1c9e29a640bab2637fac9ff98098747e41a` (S0-02/03/04 accepted at that SHA only). Next source update modifies only preview authentication/bootstrap behavior, explicit default config and integration checks; before any G0 release candidacy both operator checks must re-run against the exact new HEAD. `YS-S0-05` remains PARTIAL because historical secrets/rotation, protected preview exposure and ownership model are unresolved. [Security review](SECURITY_REVIEW_V2_20261109.md). No GitHub Actions, no PVE deployment.

### 2026-11-09 — Matched-SHA S0 acceptance and SEC-02 regression extension

Operator-confirmed on `6e8ba7bf2b82773a087e19737bb64e29b732e7d1`: source QA eight stages PASS and real API test/negative registration guard PASS, with matching manifest commit. A follow-on SEC-02 test now exercises bootstrap with an existing regular identity on **the same disposable SQLite DB**, requires startup refusal, then restores original bootstrap settings and verifies both admin login and ordinary-user denial. **New test source only; runtime PASS pending on the next commit.** Historical secrets, private TLS/ACL access and merchant ownership remain open. No PVE or GitHub Actions.

### 2026-11-09 — S0-06 manual release evidence policy

Current verified operator candidate `b30c1c819aa1aac1cd056c4fb8a03c4b7a469dd9` Source and API integration both PASS, including SEC-02. A new **source-only** S0-06 release gate requires a protected operator approval with exact SHA/ref and SHA-256 of both manifests; rejects Playwright SKIPPED and missing SEC-02 assertions; verifies origin and that executing scripts match candidate bytes; never activates. Synthetic negative-case Bash test added. Linux/PVE execution **NOT TESTED**, keep G0 HOLD and the private preview undispatched. See [PVE runbook](POC_PVE_RELEASE_RUNBOOK.md) and [Issue #5](https://github.com/jvalkovv/YangSpaceApp/issues/5).

### 2026-11-09 — S0-06 Git Bash false rejection remediation

Operator fetched `7d60705030f6230a7032af15ead794badd9063d3`. Three scripts passed `bash -n`, but `test-preview-release-evidence.sh` rejected its FIRST valid fixture with `RELEASE_BLOCKED: expected unique COMMIT_SHA value in source.manifest`. This is a release-policy test failure, not a source/API regression. The previous normal Source QA + Real API Integration PASS at `b30c1c8` remain valid for that SHA. Updated Bash manifest comparator checks exactly one required key and exact content, accepting one optional CR before LF without relying on `grep -x` line-ending behavior. New synthetic test cases include LF, source CRLF, all-manifest CRLF, duplicate SHA, appended suffix, and explicit expected negative rejection reason. Local Linux unit test of comparator PASS (LF/CRLF positive; duplicate/suffix/double-CR rejected); full Git Bash policy test on the new SHA is **PENDING**. Restore executable bit on build shell script. No deployment or GitHub Actions.

### 2026-11-09 — Windows Git Bash: NTFS permission-mode test mismatch

Operator pulled `3d0e6e94fc3768b28c759aef7fe72b0182931086`, and all three release scripts passed `bash -n`. Synthetic fixtures reported PASS for normal LF, Windows CRLF source/all manifests, duplicate SHA, modified value, skipped browser, incomplete bootstrap, failed API, incorrect approval scope, duplicate evidence key, and wrong source ref. Next negative case `mutable_approval_file` **incorrectly succeeded** because `chmod 666` via Git Bash on NTFS did not yield reliable POSIX permission bits for `stat -c %a`. The synthetic test overall remained FAIL; this is **not** evidence of approval ACL enforcement on Linux.

**Source remediation (runtime pending):** retain the Linux release validator's real `stat` permission/ownership checks unchanged; explicitly refuse a real build outside Linux. For the Windows-only synthetic regression, inject a *test-only exported Bash stat function* returning mode 666 for the synthetic approval path, assert the injection is active, and verify the validator rejects the mutable-mode condition. Linux synthetic testing performs an actual `chmod 666` and validates the permission bit change (no simulation). The final policy output reports `POLICY_PERMISSION_SOURCE=SIMULATED_MSYS_NTFS` or `REAL_LINUX_MODE`. Linux runtime permission evidence still required for S0-06 completion. No PVE or GitHub Actions.

### 2026-11-09 — Git Bash inside-checkout negative fixture correction

Operator ran S0-06 synthetic policy on commit `0c577434885a54a98c30dcf682914dbd9c4625ee`. Shell syntax and the full positive LF/CRLF set passed, all negatives through `mutable_approval_file` and `missing_approval` passed; the final `evidence_inside_checkout` case failed with `RELEASE_BLOCKED: SOURCE: absolute readable regular file (not symlink) required` instead of the expected **inside checkout** refusal. Root cause: the test computed REPO from `git rev-parse --show-toplevel`, which can return a Windows drive-qualified Git path rather than a POSIX absolute path. The fixture never reached the intended boundary assertion. The release validator remains unchanged. Source-only fix canonicalizes the test REPO with `pwd -P` + `realpath -e` and uses the existing readable tracked test script as the deliberately forbidden fixture, asserting that it really resolves below the repository root. Full Git Bash retest **PENDING**, Linux ACL and real PVE preflight untouched. No auto deployment, no Actions.

### 2026-11-09 — S1 first location CMS vertical slice, target hostname agreed

User accepted future in-container smoke/QA at `yangspace.gndstore.eu` when a dedicated YangSpace LXC is provisioned; **do not introduce Ubuntu/WSL or a separate permanent QA stack now**. No DNS/CT/ports/production state changed at this stage. Use a disposable DB and an isolated, ephemeral local test process even when run on that future CT; do not run destructive CRUD tests against the publicly served database. Check browser UX and private authentication through the real hostname only after protected TLS/gateway/ACL acceptance.

New source work: `apps/api/AdminEndpoints.cs` has idempotent admin-only `POST /locations/{id}/restore` (204/404); existing hide remains. `apps/web/src/app/admin.component.ts` verifies admin role before showing CMS, supports create/edit/publish/hide/restore, validates location inputs, handles conflict/nonadmin/timeout and keeps token in memory. CSS adds responsive editor/list states. `scripts/Invoke-Api-Integration.ps1` adds nonadmin 403, missing-record 404, double restore 204, public 200, re-hide and hidden-read cases; `apps/web/e2e/admin-cms.spec.ts` adds mocked login/edit/status/conflict regression. **Source only** — no Angular/.NET runtime tests or live endpoint acceptance for this new HEAD; mark S1-01/02/04 PARTIAL, S1-03/05 open. Keep PR Draft, G0 HOLD, no Actions/PVE deployment.

### 2026-11-09 — S1-03 publication lifecycle source / single-active-CT policy

One **active YangSpace LXC and one canonical app domain** (`yangspace.gndstore.eu`); no extra permanent dev/staging/QA instances or Ubuntu. Cloudflare A record/proxy configuration was shown by the operator but **application, TLS and public reachability have not been verified**. This does not approve a PVE write or immediate Internet-exposed preview authentication.

S1-03 source adds PlatformAdmin-only per-location `GET /locations/{id}/content`, `PUT /locations/{id}/{offers|brochures|jobs}/{recordId}`, and idempotent `POST /.../{recordId}/{hide|restore}` endpoints. All mutation records are looked up by both location and record identifiers; cross-location access is 404. `IsPublished` represents reversible archive (no physical DELETE); restoring expired content returns 409 until edited and valid. Public APIs continue filtering by parent published state, individual `IsPublished`, and UTC validity. A dedicated Angular publication manager renders all three collections, state, editable fields, archive/restore actions and conflicts; the existing creation controls cause it to refresh. CSS styles are responsive.

The expanded disposable-DB PowerShell integration regression covers unauthorized GET/PUT/restore 403, invalid inputs 400, cross-location 404, repeated hide/restore 204, expired-restore 409, public visibility and restart persistence. Mocked Playwright tests cover CMS changes and expired restore disabled.

**Evidence:** static source/diff review only; no .NET/Angular/Playwright/API runtime for this new HEAD. Windows validation scripts are not automatically compatible with Linux. Before publishing to a live domain, implement/verify Linux-compatible isolated acceptance, backup/rollback, protected admin security and Production-safe auth (the current API's preview-only admin gate is still disabled in Production). G0/G-PREVIEW HOLD, PR Draft. No Actions, PVE mutation or deployment.

### 2026-11-09 — Production CMS fail-closed separation (source only)

One active dedicated YangSpace LXC and canonical `yangspace.gndstore.eu`, no extra permanent QA environments. The **public directory** remains read-only when `Admin:Enabled=false`. The new `Admin:Enabled=true` **is NOT authorized for deployment**: it requires ASP.NET Production, Preview disabled, registration disabled, explicit AllowedHosts, an environment-injected 32-byte Base64 gateway secret, and a loopback peer that supplies the correct internal header after origin-side authentication. All admin writes additionally require the PlatformAdmin **Identity bearer** scheme. Production Identity exposes only `POST /api/auth/login` and `POST /api/auth/refresh`; cookie query options and registration are denied. The reverse proxy must verify Cloudflare Access/other approved authenticated operator policy at origin and strip client-supplied internal headers. The app alone cannot prove this edge/gateway integration.

Production database: an absolute SQLite file path outside releases, `Mode=ReadWrite`, existing file, actual reviewed EF migrations applied offline, and an offline-provisioned PlatformAdmin are mandatory. No PreviewBootstrap/EnsureCreated/fictional seeding in Production. First admin is created by an explicit interactive `--yangspace-provision-admin` operator command with separate consent and no HTTP listener; no existing user promotion or routine startup grants. **No EF migration has been generated**, and historical preview EnsureCreated DB must NOT be converted in place. Source-only negative startup tests added for missing/malformed gateway secret, missing SQLite DB and preview+production misconfiguration. Runtime tests, migrations, actual TLS/ACL, backup/recovery, and secrets rotation remain OPEN. G0/G-PREVIEW HOLD. No changes to PVE, actions, DNS, active service, or database.

Production persistence clarification: even with the CMS disabled, Production startup requires a pre-existing external `Mode=ReadWrite` SQLite database and a reviewed, applied EF migration with no model drift. The first-admin gate applies only when Admin.Enabled=true. This source increment does **not** include a migration; G0 and production activation stay HOLD.

### 2026-11-10 — S2-02 Initial EF migration tooling (not yet generated)

The current YangSpace branch has **zero** EF migration artifacts. A repeatable manual operator for the existing Windows machine is added at `scripts/New-InitialMigration-Local.ps1`: exact full Git SHA and branch check, clean worktree, pinned EF Core 10.0.0 local CLI/tool package, disposable in-memory design-time context (never Production or Preview DB), build-before/after, three generated C# files/snapshot, `has-pending-model-changes` check and initial SQL review script/hash manifest under ignored `logs/yangspace/migration-scaffold`. There is **no migration file yet** because EF tooling has not executed; hand-authored model snapshots are rejected. Generator never runs a DB update, starts listeners, modifies active DB, creates PVE resources or pushes GitHub. Source code static checks only; Windows dotnet runtime needs operator execution. Production remains blocked by its migration presence/drift guard and private proxy/security acceptance. See `docs/S2_INITIAL_MIGRATION_20261110.md`.


## 2026-11-12 — real disposable PostgreSQL gate preparation

| Item | State | Evidence / next gate |
| --- | --- | --- |
| Windows Docker Linux Engine | **PASS — capability only** | Docker v29.9.1, Linux and local named-pipe context confirmed by operator |
| PG initial migration tracked in Git | **PASS at `e1287058...`** | Exactly three independently reviewed EF files; exact-commit Windows tenant/build and EF snapshot-diff PASS |
| PG disposable runner + SQL test data | **SOURCE ADDED, NOT RUN** | Network-disabled, no host port/mount, tmpfs container; test constraints/revision CAS/audit DB rollback and synthetic backup→second-DB restore |
| Real PostgreSQL initial migration / restore | **HOLD — not executed** | Require operator new exact-SHA test output and logs, no inferred PASS |
| Owner bearer and F2b audit/recovery/production | **HOLD** | Separate identity/transactional/least-privilege/off-host gates |











## F2a-1 schema contract audit and remediation — 2026-11-11 22:09

| Workstream | Current status | Evidence / acceptance |
| --- | --- | --- |
| PostgreSQL initial migration archive | **Static integrity PASS; schema contract HOLD** | Source `ccd2acb...`; archived ZIP SHA-256 `96544589a2a27560a64df8e9da09235d9f81c6716ae151dc92bbc4d915e252c5`; verified generated EF/SQL; 15 tables, 22 indexes; missing active-claim uniqueness and Location Revision |
| BusinessClaims uniqueness | **SOURCE FIX / NOT TESTED** | One Approved per Location and one Pending per Location+Organization via filtered unique indexes; distinct orgs may apply concurrently |
| Location Revision | **SOURCE FIX / NOT TESTED** | New application-managed long EF concurrency token, initial 0; HTTP 409 and audited writes remain F2b |
| Isolation and concurrency regression | **SOURCE ADDED / NOT RUN** | Duplicate approved/pending rejection, competing applicants, resubmission after withdrawn, stale EF update rejection in SQLite `:memory:` |
| New PostgreSQL scaffold | **NOT RUN** | Generate from clean exact-new-HEAD worktree, review C#/Designer/snapshot/SQL/hashes; do not reuse old ZIP |
| Real PostgreSQL/HTTP | **HOLD** | Disposable PG migration/backup/restore, ownership and audit API tests required before merge |
| Production/demo | **NO CHANGE** | PR #17 remains Draft; CT164/CT152 and active SQLite untouched |











## F2a-1 dependency security gate — 2026-11-11 21:07 Sofia

**Operator exact candidate `aa4fec12873b5319072087317a6b64eb6a8a5162`:** PowerShell preflight PASS; `STAGE=f2a-restore` **FAIL EXIT=1** due NuGet Audit NU1902/NU1903 for transitive `Pkcs/Xml 9.0.0` introduced by the PostgreSQL migrations project's EF design-time graph. Restore blocked as designed (warnings-as-errors). Build, tenant scope and PG scaffold NOT RUN. Evidence at `D:\GitHub\YangSpaceApp-F2a1-PG-QA\logs\yangspace\f2a-tenant-scope\aa4fec12873b-20261111-210730-fb959c92` (operator-local).

**Fix source `8d2511308ff8e14b6d9a25ba6ec5539c8f2de4aa`:** patched Pkcs/Xml `10.0.14` pins in PG migrations csproj plus safe Git stdout handling for detached scaffolding. **No QA PASS on fix yet**. Required: exact-current-HEAD re-run, NuGet restore/build/tenant regression, then scaffold/review PostgreSQL EF artifacts. CT164/live DB untouched. [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) remains Draft/HOLD.











## 2026-11-11 19:07 — F2a-1 implementation and provider architecture checkpoint

| Item | Current state | Evidence / next gate |
| --- | --- | --- |
| ADR-019 / PR #16 | **MERGED/CLOSED** at `fb3e032944f0afafed3922c87b0e53dfb99c3220` | PostgreSQL target direction selected; activation is not authorized |
| PR #17 tenant source | **Draft/Open**, candidate `bc498f5bfdc933d2959b8352db3bb3e5523dbf30` | 7-file domain candidate plus provider split; no owner endpoints or audit writes |
| Prior F2a isolated QA | **PASS at `b9aaaace...`** | Owner evidence: PowerShell syntax, .NET restore, Release build with warnings-as-errors and disposable SQLite `:memory:` tenant contract PASS; `D:\GitHub\YangSpaceApp-F2a1-QA\logs\yangspace\f2a-tenant-scope\b9aaaace4c85-20261111-190535-047e666b` |
| Provider-refactor NuGet restore | **FAIL on `aa4fec1...`, FIX SOURCE on `8d2511...`, re-run required** | NU1902/NU1903 on transitive `System.Security.Cryptography.Pkcs/Xml 9.0.0` in PostgreSQL migrations project; both pinned to audited `10.0.14`, `TreatWarningsAsErrors` retained; fix not yet tested |
| PostgreSQL migration | **NOT GENERATED** | New exact-SHA scaffold script `scripts/New-PostgreSqlInitialMigration-Local.ps1`; emits C# + review SQL only, no connection/apply |
| PostgreSQL DB tests | **NOT RUN** | Require isolated disposable PG database: fresh schema, legacy-data backfill rehearsal, FK/unique/isolation, backup/restore, real API bearer tests |
| Production/demo | **UNCHANGED** | No CT164/CT152 operation, active DB write, service provision, release or public owner route |

**Next operator action:** exact-SHA Windows source build + tenant contract at `bc498f5bfdc933d2959b8352db3bb3e5523dbf30`. Only after PASS run the PostgreSQL initial migration scaffold. Keep PR #17 Draft/HOLD and parent #1 Draft/Open.



## 2026-11-13 — F2a-3 Evidence & Retention Policy owner-approved

Canonical policy v1: [F2A3_EVIDENCE_AND_RETENTION_POLICY_20261113.md](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-f2a1-domain-foundation-20261111/docs/F2A3_EVIDENCE_AND_RETENTION_POLICY_20261113.md).

- Owner approved the operational policy on 2026-11-13.
- Legal/privacy clearance is still blocked on [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24); lawful basis and privacy notice are not yet approved.
- No real evidence collection/processing, production upload/storage or live claim-decision activation is authorized before written legal/privacy clearance.
- Isolated non-production work may proceed only with synthetic data and only if it cannot accept real evidence or be enabled in production.
- The broader parent PR #17 and release remain Draft/HOLD.



## 2026-11-13 — F2a-3 next execution split

- [Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25): bounded synthetic-data-only evidence validation/retention foundation; no endpoint, migration, production storage or real documents.
- [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24): legal/privacy clearance; mandatory before real evidence collection/processing, production upload/storage or live claim-decision activation.
- Policy owner approval is recorded; release remains HOLD until the independent legal/privacy gate and all technical acceptance gates pass.


## 2026-11-13 — F2a-3 synthetic evidence policy code candidate

[Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25) is now implemented as [Draft PR #26](https://github.com/jvalkovv/YangSpaceApp/pull/26), exact candidate `519809875658e7e27232d6a49536f2d71933ea69`.

Source-only scope: pure evidence file validation, deterministic retention calculation, UTC timestamp validation, fail-closed malware scanner configuration guard and synthetic-only runtime tests. No HTTP route, evidence schema/migration, production storage or real evidence intake.

**State: NOT WINDOWS-TESTED / DRAFT / HOLD.** Exact-SHA restore, Release build, runtime execution and independent evidence postflight are still required. [Legal/privacy Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains the hard gate before any real evidence collection or production activation.
