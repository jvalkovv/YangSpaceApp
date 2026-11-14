# MVP Roadmap — Gates, Not Dates

## 2026-11-14 — Owner-approved direction / unified product milestones (docs-only)

See [Product Growth & Pilot](PRODUCT_GROWTH_PILOT_20261113.md). The six base product/architecture decisions are approved; supplementary residents/merchant growth modules are **candidate P1/P2 scopes**, not authorized runtime implementation. Preserve existing engineering gates **G0-G4, G-PREVIEW, G-PILOT**; the following M0-M7 are product work packages and do not rename those gates.

| Package | Priority | Deliverable | Exit evidence |
|---|---|---|---|
| M0 Architecture/SSOT | P0 | PostgreSQL target ADR, Identity session/CSRF ADR, geo and SEO design, integrated dependency graph | Owner-reviewed ADRs and single current tracker |
| M1 Foundation & Identity | P0 | #27 source review -> #17 integration -> #29 account HTTP/mail + protected PG runtime | Exact SHA builds, real HTTP/DB/auth negatives, mail outbox semantics |
| M2 Ownership & Moderation | P0 | Issue #20 reviewer decision, atomic claim/Owner/revoke/audit; Issue #24 privacy legal gate | Real scoped auth/idempotency with synthetic-first and legal clearance before real evidence |
| M3 Verified Business CMS | P0 | Locations, offers, brochures, jobs, uploads, expiry, field-level audit, owner freshness | End-to-end create/review/publish/hide/restore persisted with tenant controls |
| M4 Public Discovery | P0/P1 | Search/neighborhood, data accuracy, action CTAs, mobile UX, SEO; map/QR/favorites as P1 | Crawl/geo/performance/a11y + correct aggregated action counters |
| M5 Reliable Private Release | P0 | CT164 controlled activation, TLS/origin, migrations, alerting, backup/restore/rollback | G-PREVIEW then G-PILOT evidence as relevant |
| M6 Varna Pilot | P0 | Phase A 20-30 verified local places; Phase B 100 conditional on Phase A | Freshness, useful actions, repeatability, costs, 30 owner interviews and 10 intent signals in Phase B |
| M7 Commercial Growth | P1/P2 | QR/analytics/limited inquiries; later events, notifications and sponsored campaigns | Measured merchant value, separate privacy/DSA and billing/commercial ADRs |

**Guardrails:** no online checkout, automated billing, booking, POS, CV store, native app or AI assistant for MVP; no production Owner grant based solely on email confirmation; no field-level analytics masquerading as a real-world sale/visit. Small PRs close only after their individual 100%-accepted DoD and logged recovery evidence. Draft legacy-main PR #30 must not override this active V2 history.



> **ADR-018:** GitHub Actions не се ползват. Проверки с exact-SHA локален runner, evidence logs и ръчен release.

> **Execution precedence (2026-11-09):** This is the original strategic milestone outline. Use [POC_MASTER_PLAN.md](POC_MASTER_PLAN.md) and [POC_TRACKER.md](POC_TRACKER.md) as the current executable plan. The original M0 legacy remediation items below describe historical risks; the old app is archived, not compiled in YangSpace 2.0. The active engineering work is dependency determinism/manual clean-SHA QA/real v2 auth and data integration, not patching old booking endpoints back into production.


**Не се поемат срокове до оценка на кода и работа по P0 дефектите.** Първо пилотен квартал, после доказано повторяемо разрастване.

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

## 2026-11-12 13:41 Europe/Sofia — combined F2a regression PASS ×2; F2a-3 contract next

- **Exact tested combined SHA:** `445db4d9a5d28b798054c426afeba9b9be2c771b` on [Draft PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17). Two independent sequential operator Windows runs (13:28/13:41 Europe/Sofia): PowerShell runner syntax, .NET restore/Release build, tenant scope; PostgreSQL EF tool/restore/build/pending-model-diff, reviewed SQL SHA, disposable PG17 migration, FK/filtered unique constraints, sequential revision CAS, DB audit rollback, custom backup and second-DB restore all PASS. Both finish `F2A_TENANT_SCOPE_ISOLATED_ONLY=PASS`, `F2A_PG_DISPOSABLE_RUNTIME=PASS`, `COMBINED_HEAD_REGRESSION=PASS`. The operator reports no stage failures.
- Latest tenant evidence: `D:\GitHub\YangSpaceApp-F2a1-Combined-QA-20261112-0822\logs\yangspace\f2a-tenant-scope\445db4d9a5d2-20261112-134120-cc109012`.
- Latest PG evidence: `D:\GitHub\YangSpaceApp-F2a1-Combined-QA-20261112-0822\logs\yangspace\pg-disposable\445db4d9a5d2-20261112-134126-299426c5`.
- **Acceptance boundary:** console evidence confirms two disposable PG schema/constraint/recovery and SQLite tenant tests; local manifest/SQL/backup file bytes and cleanup inventory have not yet been independently received or verified with SHA-256. `REAL_BEARER_PARALLEL_WRITERS_PRODUCTION=NOT_TESTED`. Previous real Identity bearer HTTP TestServer PASS is SQLite-only, from merged [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19).
- **Open PR inventory:** #19 MERGED/CLOSED into F2a-1 branch; #17 OPEN/DRAFT/HOLD despite being mergeable; V2 parent #1 OPEN/DRAFT/HOLD. Do not merge #17/#1; broader reviewer decision, PostgreSQL HTTP, application writer/audit atomicity, restricted PG role and recovery gates remain unaccepted.
- **Next:** read-only SHA-256 postflight of latest tenant/PG local receipts (no repeat build/DB test). [Issue #20 — F2a-3](https://github.com/jvalkovv/YangSpaceApp/issues/20) contains proposed reviewer role, claim transition, membership ownership, transaction/rollback acceptance and conflict policy, **pending explicit approval before code**. No GitHub Actions, CT164/CT152 access, live DB, merchant activation or deployment.

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

## 2026-11-12 14:48 Europe/Sofia — execution governance reaffirmed: visible progress and PR closure

**Mandatory standing requirement, reconfirmed by owner on 2026-11-12:** every YangSpace implementation cycle reports progress in the active conversation at scope/commit/test/review/merge checkpoints, without relying on an hourly watcher. Include exact SHA, PR/issue, code vs operator-run vs runtime evidence, current PASS/PARTIAL/HOLD, blocker, next action and whether production was touched. Do not claim unattended background implementation or an unexecuted PASS. The existing hourly watcher is optional notification support only, not evidence.

**PR cleanup at each checkpoint:** inspect all open PRs, their target branch, own Definition of Done and exact candidate head. Only PRs that are **100% completed for their bounded scope**, with exact-SHA QA, evidence postflight, source review and owner-approved merge action, move READY → MERGED → CLOSED. Never manually close an incomplete PR for list hygiene; never infer pilot readiness from a child QA PR merge.

**Current inventory (2026-11-12 14:48 Europe/Sofia):**

| PR | Base | Status | Unmet scoped acceptance |
| --- | --- | --- | --- |
| [#21 — F2a-3 isolated synthetic claim transaction QA](https://github.com/jvalkovv/YangSpaceApp/pull/21) | #17 F2a-1 branch | **OPEN / DRAFT / SOURCE-ONLY** | Current exact-SHA Windows .NET build + SQLite transaction/fault-injection markers; evidence and static review; no PostgreSQL/real reviewer claim |
| [#17 — F2a-1 domain/tenant + merged bearer QA](https://github.com/jvalkovv/YangSpaceApp/pull/17) | V2 branch | **OPEN / DRAFT / HOLD** | Real PostgreSQL application bearer HTTP/parallel writers, reviewer claim workflow, atomic app audit, PG app role, recovery/release gates |
| [#1 — YangSpace V2 parent](https://github.com/jvalkovv/YangSpaceApp/pull/1) | `main` | **OPEN / DRAFT / HOLD** | Product/security/operational/pilot sign-off |
| [#19 — F2a-2 real Identity bearer read QA](https://github.com/jvalkovv/YangSpaceApp/pull/19) | #17 F2a-1 branch | **MERGED / CLOSED** | None for its *isolated SQLite/TestServer* scoped DoD; does not prove production authorization |

**Current engineering step:** [Issue #20 — F2a-3](https://github.com/jvalkovv/YangSpaceApp/issues/20) remains **PROPOSED** for reviewer/claim business policy; [Draft PR #21](https://github.com/jvalkovv/YangSpaceApp/pull/21) adds only a disposable SQLite EF transaction feasibility test: successful synthetic claim/org/location/Owner/audit transaction and a forced audit UNIQUE error after the first EF SaveChanges to prove rollback across batches. No HTTP claim review endpoints, schema migrations, claim-role grants, CT164/CT152 access, production deployment or Actions. PostgreSQL integration topology and real reviewer policy acceptance remain separate gates.

## Delivery governance — mandatory progress reporting & PR closure (accepted 2026-11-11)

This is a **standing execution requirement** for all YangSpace work, including external Work runs and manual operator actions. It is not a product feature and does not substitute for tests or owner review.

1. **Report at meaningful checkpoints:** after an accepted scope decision, commit, completed test stage, PR creation/review, merge/closure, release or blocker. Include exact commit SHA, PR/issue link, environment, PASS/PARTIAL/HOLD, evidence location, next dependency and any production-vs-source drift. Do not publish empty progress updates or report unperformed work as completed.
2. **Hourly GitHub change watch:** existing YangSpace GitHub progress automation checks branch head, PRs, Issue #11 and gates. Notify on substantive changes only; a scheduled check is not a continuous build/deploy service, and progress reports do not imply asynchronous implementation.
3. **Small, independent PRs:** each PR states scope, base/head SHA, changed files, QA results, rollback/deployment consequences and Definition of Done. Do not mix unrelated CMS/infra/UI changes. Never use GitHub Actions: apply [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md) and retain separate `logs/yangspace/{verify,api-integration,deploy,restore}/` evidence as applicable.
4. **Close completed PRs only by an approved merge:** `100%` is scoped to **that PR's** agreed Definition of Done, including required exact-SHA QA, independent review and explicit owner approval. Merge into its approved target branch (prefer squash for cohesive changes), then verify GitHub marks it **merged/closed**. A manual `closed/unmerged` status is for an expressly abandoned/superseded PR and must state why. Never call a full project ready merely because all small PRs have merged.
5. **One open source of truth:** keep a current inventory of `OPEN` / `DRAFT` / `READY` / `MERGED` / `CLOSED UNMERGED` PRs in [SSOT.md](SSOT.md) and the active parent PR; reconcile the head after every merge. Full feature-to-`main` PR #1 remains open while release/pilot gates remain HOLD.
6. **No implicit deployment:** merges do not activate CT164/CT152, apply migrations, unlock admin/registration, or promote demo to production. Require a distinct runtime approval and SHA-specific smoke/rollback evidence.
7. **In-conversation progress is mandatory, monitoring optional:** during an active implementation/review, provide concise Bulgarian progress reports before/after meaningful stages (source change, diff, test, finding, blocker, PR transition). Never wait for a scheduled hourly watcher to report progress. State `completed / evidence / next` with exact SHA and distinguish local static checks, operator-provided QA, real runtime and deployment. Do not imply the assistant will independently continue between messages, and do not add a new monitoring schedule unless requested.
8. **Review open PRs for 100%-scoped completion at each closure checkpoint:** for each open PR record its target branch, exact head, agreed DoD, completed vs missing QA, review, runtime/security and required owner approval. Only fully accepted scoped changes move to READY → MERGED → verify CLOSED; a merely compiling or generated migration is not "100%" when real DB/recovery or authorization tests are still required. Do not close Draft PRs for visual hygiene; if the scope is genuinely abandoned/superseded, close unmerged with rationale and linked replacement. After an accepted merge, refresh PR inventory and SSOT on the new base.


### 2026-11-12 08:15 Europe/Sofia — F2a-2 merged; F2a-1 parent revalidation required

**Scoped milestone: F2a-2 read-only bearer authorization COMPLETE.** [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) was marked ready after source review and operator evidence postflight, then squash-merged into the F2a-1 feature branch (not V2 or \`main\`).

- Tested code SHA: \`f579c70db9cc0986d2f1df8047158587f0e87eef\`.
- Documentation-only follow-up HEAD: \`bc0a77ea6b6bc794a2d9d6aefcbe3c12936a329e\`; GitHub compare confirms one modified Markdown file only, no code/test changes after the tested SHA.
- Squash merge / new F2a-1 branch HEAD: \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\`.
- Operator Windows exact-SHA restore, Release build with warnings-as-errors, tenant regression and real Identity bearer TestServer all PASS on \`f579c70...\`. Operator read-only postflight returned \`EVIDENCE_POSTFLIGHT=PASS\`: manifest, four runner/stdout/stderr stage groups, 23 expected postflight HTTP markers and SHA-256 receipts all verified. Manifest SHA-256: \`336D8A9DCB4C69483981A2A5124A5123E8AD9044C14A1619199439E2E121F366\`. Detailed hashes and local evidence path are recorded in [F2a-2 QA evidence](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-f2a1-domain-foundation-20261111/docs/F2A2_MERCHANT_BEARER_READ_QA_20261112.md).
- Accepted scope is disposable SQLite + ASP.NET Core TestServer only, with actual Identity-issued bearer tokens and the documented 401/404, tenant isolation, membership revocation, default-off, no-write and DTO allowlist checks. No PostgreSQL bearer runtime or production exposure is claimed.

### Current PR inventory (2026-11-12 08:15 Europe/Sofia)

| PR | Target | Exact current head | State / decision |
| --- | --- | --- | --- |
| [#19 — F2a-2 bearer-only merchant read](https://github.com/jvalkovv/YangSpaceApp/pull/19) | F2a-1 feature branch | merge \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` | **MERGED / CLOSED** |
| [#17 — F2a-1 domain + tenant foundation, now includes #19](https://github.com/jvalkovv/YangSpaceApp/pull/17) | V2 feature branch | \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` | **OPEN / DRAFT / HOLD** |
| [#1 — V2 parent / pilot](https://github.com/jvalkovv/YangSpaceApp/pull/1) | legacy \`main\` | current parent branch head; see PR | **OPEN / DRAFT / HOLD** |

**Why #17 remains HOLD:** the previous F2a-1 disposable PostgreSQL migration/constraints/synthetic backup-restore PASS belongs to exact source \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`, not automatically to the combined head \`17d08530...\`. First re-run exact-SHA source/build and disposable PostgreSQL migration/constraint/recovery regression against the combined head. Then complete PostgreSQL bearer HTTP and parallel EF writers, reviewer-only claim/membership lifecycle, atomic application business+audit fault-injection, least-privilege DB role, off-host recovery and owner release sign-off. Do not merge #17 or #1 yet.

### Next execution order

1. **F2a-1 + F2a-2 combined-head regression:** clean exact-SHA checkout of \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\`; source/build and disposable PostgreSQL regression. Keep the old PASS attached to its historical SHA until the new combined-head run passes.
2. **[F2a-3 contract/design — Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) before code:** reviewer identity/role policy, no self-approval, claim state machine, Owner membership grant/revoke, transaction boundary, actor/reason audit allowlist, concurrency/unique-conflict behavior, fault-injection matrix and recovery semantics. Keep claim mutation separate from the already-merged read-only endpoint.
3. **F2a-3 implementation:** only after the contract is recorded in Issue #4/ADR and reviewed; exact-SHA PostgreSQL tests must prove claim + org/location ownership + membership + audit atomicity, with rollback on every injected failure.
4. **F2b:** content mutations and revision/409 semantics only after F2a authorization and audit acceptance.

**PR closure rule applied:** #19 is the only newly completed PR and is merged/closed. #17 and #1 remain open because their own scoped security/release DoD is incomplete. No PR was manually closed unmerged. No GitHub Actions, CT164/CT152 access, live DB mutation, migration, feature activation or deployment.

### 2026-11-12 08:02 Europe/Sofia — F2a-2 isolated Identity bearer HTTP acceptance

**Operator exact-SHA PASS.**

- **Exact tested SHA:** \`f579c70db9cc0986d2f1df8047158587f0e87eef\`, PR #19, stacked on F2a-1 base \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`.
- Operator Windows output: runner syntax PASS; restore PASS; Release build with warnings-as-errors PASS; existing tenant regression PASS; real Identity bearer HTTP TestServer PASS; final \`F2A2_MERCHANT_BEARER_HTTP=PASS\`.
- Evidence directory: \`D:\\GitHub\\YangSpaceApp-F2a2-Bearer-QA\\logs\\yangspace\\f2a2-merchant-http\\f579c70db9cc-20261112-075949-ea648860\`. Runner checks its required marker set and writes a manifest only after successful stages. **The actual local manifest and log bytes have not yet been independently received/reviewed.**
- **Accepted scope:** isolated synthetic SQLite/TestServer only: real Identity-issued bearer tokens; default-off and QA-only environment checks; 401 anonymous/invalid/cookie; owner/editor positive access; cross-tenant, unknown, unclaimed, suspended and revoked 404; forged actor and PlatformAdmin-without-membership denied; registration/cookie login/write route denied; strict five-field DTO and no business/audit side effects.
- **Explicit boundary:** \`LIVE_POSTGRESQL_BEARER_HTTP=NOT_TESTED\`; PostgreSQL bearer and parallel EF writers, reviewer-only claim grant/revoke, atomic business+audit application transaction, least-privilege production role, off-host recovery and deployment remain HOLD.
- **Source review:** exact PR #19 source reviewed (Program fail-closed gate, Bearer-only policy, single joined membership/resource query, allowlisted DTO, test startup scope and exact-SHA runner); no blocking defect found in this bounded read-only slice. Independent human review and operator evidence-file postflight remain pending.
- **PR inventory:** #19 remains DRAFT/HOLD pending independent evidence postflight/review/merge checkpoint; #17 DRAFT/HOLD pending real provider authorization, concurrency and audited mutation gates; #1 DRAFT/HOLD pending program release gates. **0 open PRs eligible for closure at this checkpoint.** Do not close unmerged or claim full F2a/production acceptance.
- **Next execution:** read-only postflight of the local evidence manifest and stage logs; then independent review/owner merge decision for #19. Next product/security slice is reviewer-only claim verification and membership grant/revoke with atomic audit and DB fault-injection tests, specified before implementation. Keep it separate from this read-only route.
- No GitHub Actions, CT164/CT152 access, live DB changes, migration, feature activation or deployment.

### 2026-11-12 07:49 Europe/Sofia — F2a-2 HTTP TestServer startup recovery

- **Operator exact-SHA verification:** `ceea4723ccc5d2fc7612e63ec3ff4239b37ea0b8`, Windows isolated worktree, `f2a2-restore`, `f2a2-build`, `f2a2-tenant-regression` **PASS**. `f2a2-bearer-http` **FAIL** before HTTP assertions; `Program.cs:46` rejects missing `ConnectionStrings:Main`. Operator log `logs/yangspace/f2a2-merchant-http/ceea4723ccc5-20261112-074923-0ce50a73` (local-only; underlying bytes not independently received).
- **Root cause:** `WebApplication.CreateBuilder()` reads/validates connection config before `WebApplicationFactory.WithWebHostBuilder.ConfigureAppConfiguration` late test injection. Preserve the API's fail-closed startup validation.
- **New source fix:** [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) `f579c70db9cc0986d2f1df8047158587f0e87eef`, single QA-fixture file (+68 lines). Pre-host scoped process-only SQLite QA configuration; restores prior environment after sequential test; Production/Development fail-closed probes retained. No API/runtime schema/deployment changes.
- **Exact-new-SHA acceptance:** NOT RUN; require Windows runner's restore, Release build, tenant isolation and actual HTTP Identity bearer matrix plus manifest. No hypothetical PASS. #19, #17 and #1 all remain Draft/HOLD; **zero open PRs closure-eligible** at this checkpoint. CT164/CT152 and live DB untouched.

### 2026-11-12 07:39 Europe/Sofia — F2a-2 build failure / recovery and PR closure review

**Mandatory reporting policy above remains in force** (merged via PR #18): progress after each meaningful change, evidence-bound PASS/HOLD, and closing only scoped 100%-accepted PRs through approved merge. This checkpoint is source-only; no GitHub Actions, CT164/CT152 changes, live DB migration or merchant feature activation.

| Open PR / target | Exact current candidate | Evidence / unfinished gates | Decision |
| --- | --- | --- | --- |
| [#19 — QA-only merchant Identity bearer read](https://github.com/jvalkovv/YangSpaceApp/pull/19) → F2a-1 branch | `ceea4723ccc5d2fc7612e63ec3ff4239b37ea0b8` | Operator's previous `62505864...` Windows restore PASS, build FAIL `CS0136` for duplicate test locals `owner`/`editor`; new one-file, four-line-pair QA fixture rename committed. **New exact-SHA build and actual bearer HTTP TestServer NOT RUN**. | **DRAFT / HOLD**; repeat exact-head local runner, independently review manifest; no merge |
| [#17 — F2a-1 domain/PG foundation](https://github.com/jvalkovv/YangSpaceApp/pull/17) → V2 foundation | `1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001` | Disposable PostgreSQL migration/constraints/sequential CAS/audit rollback/pg_dump→pg_restore and SHA/cleanup checks PASS **for this exact source**. Reviewer claim-grants, real PG bearer, live EF parallel writes and production hardening pending. | **DRAFT / HOLD**; completed schema QA does not close entire PR |
| [#1 — V2 program](https://github.com/jvalkovv/YangSpaceApp/pull/1) → legacy main | Parent feature HEAD at time of review `0b67c5edcd4d2da3b5f9ee565e93f6a4fa18ad0e`; roadmap-only update will advance this | Production/pilot acceptance, tenant security, application-audit atomicity, operations and off-host recovery unaccepted. | **DRAFT / HOLD**; no merge to main |

**No open PR is 100%-accepted at this checkpoint; 0 PRs eligible for closure.** Historical merged PRs remain closed. Next gate is `Test-F2a-Merchant-Bearer-Local.ps1` using the newly fetched #19 commit, requiring restore/build (warnings-as-errors), tenant regression and genuine Identity bearer HTTP contract. Runner evidence path must be included in the next report. If the next test fails, preserve its stage output and return to RCA; no security test bypass.

### 2026-11-11 22:50 Europe/Sofia — progress and open-PR reconciliation

| PR | State | Scoped completion gate | Next |
| --- | --- | --- | --- |
| [#17 — F2a Organization/Claim/Tenant](https://github.com/jvalkovv/YangSpaceApp/pull/17) | **DRAFT / OPEN / HOLD** at `c96ff2e0ab76a2fc72713ff4e5516b5f40beb11d` | Source restore/build + isolated SQLite tenant/duplicate claim/revision tests passed; EF PostgreSQL SQL generation passed, latest ZIP static checksum/schema review passed. **Real disposable PostgreSQL migration, FK/UNIQUE enforcement, recovery and bearer API negative tests NOT ACCEPTED** | Commit independently reviewed EF-generated files without applying, then disposable PG DB/restore/tenant test evidence; recheck exact new PR SHA |
| [#1 — YangSpace V2 vs legacy main](https://github.com/jvalkovv/YangSpaceApp/pull/1) | **DRAFT / OPEN / HOLD** | Preview foundation exists, but true merchant ownership, audit, production DB, ingress and off-host recovery/pilot sign-off NOT ACCEPTED | Do not merge into `main` until all G-PILOT/release gates pass |
| [#16 — ADR-019 PostgreSQL direction](https://github.com/jvalkovv/YangSpaceApp/pull/16) | **MERGED / CLOSED** | Docs-only scope complete, no runtime activation asserted | Archive |
| [#13 — freshness](https://github.com/jvalkovv/YangSpaceApp/pull/13), [#14 — timestamps](https://github.com/jvalkovv/YangSpaceApp/pull/14) | **MERGED / CLOSED** | Exact candidate source/QA accepted for those bounded changes, not deployed | Archive |

**Proof boundary:** This roadmap snapshot does **not** approve PR #17 merge or database deployment. The latest uploaded PostgreSQL ZIP has matching operator/independent SHA-256 `f4ff533c34c11806b27a3572e7af1de31e26c507f0d18ca1ecc0933ddce5ca9e` and two correct filtered unique indexes plus `Locations.Revision`; still no applied PostgreSQL instance/restore. No CT164/CT152 changes. PR inventory is a dated snapshot, not live monitoring.

### 2026-11-11 current milestone — F1a and F1b accepted / F2-F3 next

- **Public freshness F1a**: [PR #13](https://github.com/jvalkovv/YangSpaceApp/pull/13) MERGED/CLOSED, source+API QA PASS on candidate `9656518bb3d795a1e8cfaa7c3004678ee27fa6d4`, squash `54b47576ab8206d599f837cd99340c008d49f0d6`. Read-only UI change only.
- **Publication timestamps F1b**: [PR #14](https://github.com/jvalkovv/YangSpaceApp/pull/14) MERGED/CLOSED, source+API+SQLite runtime QA PASS on candidate `2634014f64fcff237b29564d0572643f2cba034a`, squash `403b281abad2fb0c8048c2763a606c3435f3e5dd`. EF-generated migration preserves old unknown timestamps as NULL; all QA on disposable DB.
- **Next engineering package: F2** — Organization/BusinessClaim/Member authorization contract → atomic per-field actor audit and optimistic concurrency with negative cross-tenant tests. **F3** owner confirmation and moderation only after F2. See [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11).
- **No public release implied:** PR #1 remains DRAFT/OPEN; G0/G-PREVIEW/G-PILOT still HOLD. CT164 active demo and DB were not changed, off-host restore and direct-origin security need independent operator acceptance. Older exact-commit engineering QA snapshots below are history, not current branch HEAD.

### Accepted exact-commit engineering QA snapshot

- Source SHA **`2baeb797b4d9ca0f5ce9d6c3d311a7b8092be7dc`**, V2 feature branch after merged [PR #9](https://github.com/jvalkovv/YangSpaceApp/pull/9) and [PR #10](https://github.com/jvalkovv/YangSpaceApp/pull/10).
- Owner-provided Windows PowerShell evidence: `SOURCE_VALIDATION=PASS` (lockfile, npm ci/audit, Angular typecheck/build, Playwright, .NET restore/build) and `REAL_API_INTEGRATION=PASS` (disposable SQLite, restart/persistence, anti-escalation checks). Both tests were pinned to the **same exact SHA**.
- Local-only, uncommitted evidence directories:
  - `D:\GitHub\YangSpaceApp\logs\yangspace\verify\2baeb797b4d9-20261111-074951-04b1acd1`
  - `D:\GitHub\YangSpaceApp\logs\yangspace\api-integration\2baeb797b4d9-20261111-075201-77648186`
- **Engineering source QA PASS** does **not** constitute off-host restore, direct-origin firewall acceptance, production merchant ownership/audit, deployment acceptance or permission to merge PR #1 into legacy `main`. Reference [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11) for freshness and edit history.

## M0 — Legacy Recovery / Security / Manual QA

- [ ] Branch protection, restore point/tag на наследения baseline, зависимостите са инвентаризирани.
- [ ] Ротация на потенциално компрометирани DB/JWT credentials и премахване на secrets от tracked configuration.
- [ ] Fix на endpoint authorization и ownership за create/update/delete/booking/admin.
- [ ] Upload validation, storage isolation, ограничения и malicious-file testing.
- [ ] Кохерентна JWT/Identity конфигурация; ролеви политики и integration tests.
- [ ] Миграционен път от .NET 8 към .NET 10 и Angular 18 към поддържана версия.
- [ ] Зелен build + automated test baseline; повтаряем dependency restore, без автоматичен `npm audit fix`.
- [ ] Публикувани безопасни local development инструкции, без demo credentials.
**Exit gate:** никакви отворени P0 auth/upload/secrets дефекти; локалните exact-SHA source/API тестове са PASS; няма production deploy.

## M1 — Foundations

- [ ] ADR за база данни, географски модел и storage.
- [ ] Domain modules, EF migrations, unit/integration tests.
- [ ] Observability (health/metrics/logs), structured errors, feature flags if required.
- [ ] PVE staging LXC с Nginx, systemd, TLS, backup/restore процедура.
**Exit gate:** възстановима среда и работещ minimal vertical slice.

## M2 — Directory

- [ ] Organization + Location CRUD, categories, neighborhoods, hours, contact channels.
- [ ] Claim/verification flow, ownership and moderation queue.
- [ ] Public searchable directory, maps integration and location pages.
- [ ] SEO/SSR, sitemap, mobile accessibility.
**Exit gate:** 100+ качествени пилотни обекта могат да бъдат управлявани и открити.

## M3 — Campaigns & Brochures

- [ ] Оферти, период на валидност, изтичане, статуси, audit.
- [ ] Защитени uploads, PDF preview, media lifecycle.
- [ ] Owner dashboard с публикуване и основни статистики.
**Exit gate:** одобрените кампании се виждат коректно и изтичат автоматично.

## M4 — Jobs & Moderation

- [ ] Jobs CRUD с външно кандидатстване (без CV база).
- [ ] Пълна moderation/abuse-report обработка и data correction.
- [ ] Policy review: GDPR, рекламни намаления, авторски права, DSA (ако е приложим).
**Exit gate:** проверени end-to-end owner/admin/visitor flows.

## M5 — Pilot Launch / Validation

- [ ] Backup + restore test, security review, load smoke, performance/SEO acceptance.
- [ ] Onboard на първи търговци, публичен каталог, ограничен локален маркетинг.
- [ ] Dashboard за traffic, click-to-call, directions, актуално съдържание.
- [ ] Преглед на willingness-to-pay, без автоматично включване на billing.
**Exit gate:** repeatable onboarding, висока актуалност, измерена потребителска стойност.

## M6 — Варна / национално развитие (условно)

- [ ] Стандартизирани райони/градове, import governance, duplicate detection.
- [ ] Multi-location organization management, content quality automation.
- [ ] Monetization ADR по данни от пилота; billing само след правен/финансов review.
- [ ] Capacity planning, restore RTO/RPO tests, операционен runbook.

## Definition of Done за всяка функционалност

Source + tests + reviewed DB migration + auth check + audit/logging + docs + visual/mobile QA + runtime evidence. Всеки deployment има commit SHA, версия, backup reference, verify output и rollback path. Без `main` auto-deploy.
