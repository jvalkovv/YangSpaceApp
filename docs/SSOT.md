# YangSpace 2.0 — Single Source of Truth (SSOT)

**Status:** ACTIVE planning/status authority on the YangSpace V2 feature branch; introduced by merged PR #9. This document does **not** authorize deployment, database mutation, exposure, or feature-to-`main` merge.  
**Baseline date:** 2026-11-11 (Europe/Sofia).  
**Latest accepted F2a-2 code QA:** tested code SHA `f579c70db9cc0986d2f1df8047158587f0e87eef` (Windows restore/build/tenant regression/real Identity bearer TestServer PASS; operator evidence postflight PASS), documentation-only follow-up `bc0a77ea6b6bc794a2d9d6aefcbe3c12936a329e`, squash-merged by PR #19 as `17d08530db7fed0bbe7ed4444577a7b0e6b73c4d` into the F2a-1 feature branch. This is **not merged into the V2 feature branch or `main`**. Earlier F1b source QA remains historical and valid for its own SHA. Historical CT164/CT152 deployment remains `a4802805421f10e834ad35bbf2f259db4e4eab84`. [Parent PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) stays Draft. `main` remains the legacy application.  
**Scope of this update:** GitHub source/PR reconciliation and operator-provided exact-SHA Windows QA + read-only evidence postflight; **no new build, PostgreSQL runtime, real-browser, PVE, or public-origin execution** was performed for this document.

> **Authoritative summary:** strong design and functional *preview foundation*; real multi-business/owner workflows, public-pilot content governance, SEO, ingress protection, observability and off-host recovery are not accepted. **G0, G1, G2, G3, G4, G-PREVIEW and G-PILOT remain HOLD at this reviewed source baseline.** Do not infer percentages or production readiness from code, screenshots, HTTP 200 or historical PASS markers.

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

