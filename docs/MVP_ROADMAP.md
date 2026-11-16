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


## 2026-11-14 — PR #27 synthetic QA ACCEPTED / parent HOLD

PR #27 merged to F2a parent at `1b59aa94cee410212df954223693153faa1c7d1d`; operator exact-source `ede8a3f...` PG reviewer HTTP and concurrency passed. Next parent exact-head QA and F2b account HTTP/mail. This documentation change is not a new runtime test or release.

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


# MVP Roadmap — Gates, Not Dates

> **ADR-018:** GitHub Actions не се ползват. Проверки с exact-SHA локален runner, evidence logs и ръчен release.

## 2026-11-13 — F2a-1 branch synchronized with current V2 base

**Branch integration commit:** `eb75e0022634499de489578f6f1003be2706a1f1`.
- First parent: F2a-1 pre-integration HEAD `93809f4f8c6cab18d261123195ec518df4ca9ba5`.
- Second parent: current V2 base HEAD `92f248dc88fcc888f93c2829727e22db0eb65312`.
- Merge tree: `c8f095d9413949725c70eec2eb8e1225dc0fce03`, identical to the F2a-1 pre-integration tree; no runtime/source bytes were changed by this merge.
- GitHub compare from V2 base to F2a-1 integration: **83 ahead / 0 behind**, merge base is the current V2 HEAD. PR #17 now reports **mergeable=true / clean**, but remains **OPEN / DRAFT / HOLD**.

Four overlapping documentation paths were reconciled:
- `MVP_ROADMAP.md` and `POC_TRACKER.md`: current V2 content retained; F2a acceptance/evidence history retained.
- `SSOT.md`: two superseded baseline statements were updated to reflect accepted F2a-3 policy and child QA; the remaining V2 baseline content is retained.
- `F2A3_CLAIM_REVIEW_SECURITY_CONTRACT_20261112.md`: proposed-only status was superseded by the owner-approved technical and evidence/retention policies. Legal/privacy clearance remains blocked by Issue #24.

**Next exact-SHA operator gate:** use the final current HEAD shown on [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) after this documentation sync. In a clean detached Windows worktree, run `scripts/Test-F2a-Tenant-Scope-Local.ps1` followed by `scripts/Test-F2a-PostgreSql-Disposable-Local.ps1`. Both runners pin the F2a-1 branch remote tip to the exact tested SHA. This validates the combined current source tree; historical child PASS results remain attached to their original exact SHAs. Do not run the PR #22/#23/#26 branch-pinned child runners against this parent branch.

This synchronization is not parent PR acceptance, release approval, legal clearance, production intake, deployment or a claim that the outstanding PostgreSQL bearer/parallel-writer/recovery gates have passed.



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


## 2026-11-12 — F2a-3 technical policy approved; reviewer authorization child PR #23

Owner approved the technical reviewer/ownership/concurrency policy recorded in [F2a-3 security contract](F2A3_CLAIM_REVIEW_SECURITY_CONTRACT_20261112.md) and [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20). **This is not approval of evidence categories, evidence checklist, private evidence handling, retention/erasure or legal basis**; those remain a mandatory separate gate before any real claim decision mutation.

[Draft PR #23](https://github.com/jvalkovv/YangSpaceApp/pull/23) is stacked on [F2a-1 PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) at parent source SHA `7f3e8d5b37f591c3c0344996b850ad23a3155b0a`. Its bounded scope is dedicated `MerchantClaimReviewer` bearer authorization, persistent live-role check (same-token revocation), self-review/active-member conflict policy and disposable SQLite Identity TestServer tests. A minimal QA access probe is the only new route; claim decision POST, ownership changes, Owner grants, evidence storage, audit writes and migrations are not included.

**Status: DRAFT / SOURCE-ONLY / OPERATOR WINDOWS QA NOT RUN / HOLD.** Next: fetch exact PR #23 head into a clean detached Windows worktree; native PowerShell parser, .NET restore, Release build with warnings-as-errors, exact-SHA HTTP contract runner, then independent manifest/stage/evidence postflight and scoped review. The test is SQLite bearer HTTP, not PostgreSQL bearer HTTP. PR #22's already accepted disposable PostgreSQL transaction QA must not be repeated for this authorization-only change.

Open PR inventory: #23 OPEN/DRAFT/HOLD; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD. PRs #19/#21/#22 remain MERGED/CLOSED. No GitHub Actions, CT164/CT152, live DB, public merchant activation or deployment. Continue reporting exact SHA, evidence path, PASS/PARTIAL/HOLD, blockers and next action at each meaningful checkpoint; close only a scoped PR with 100% accepted DoD.

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


## 2026-11-13 — F2a-3 reviewer authorization operator QA failure and correction

Operator exact-SHA run on `7ed7ab3d0e00da41fb3d11afeb6ed45c69c2fa1b`: PowerShell parser, .NET restore and Release build **PASS**; isolated Identity bearer HTTP **FAIL** during reviewer-role revocation. EF Core reported duplicate tracked `IdentityUser`: the test reused the reviewer entity created in an earlier DI scope inside a later `UserManager` scope. This is a test-harness entity-lifetime defect; the HTTP acceptance is not PASS.

Failure evidence (operator machine): `D:\\GitHub\\YangSpaceApp-F2a3-ReviewerAuth-QA\\logs\\yangspace\\f2a3-reviewer-auth\\7ed7ab3d0e00-20261113-060137-7782b371`. Runner wrote `gate.txt`; no passing manifest was produced.

Correction committed in PR #23: the revocation test reloads reviewer by ID through the active scoped `UserManager`, verifies the role is present, then removes it using that same-scope entity. Runner now requires markers for both the reload and pre-revocation role check. Current corrected source candidate at the time of this entry: `9ced228838c11b4584229160fe1b40565aff3c7f` (the documentation commit that follows may advance the SHA).

**Current state: SOURCE CORRECTION COMMITTED / CORRECTED SHA NOT YET TESTED / PR #23 DRAFT-HOLD.** Next: clean detached worktree at the latest PR head; native parser; restore; Release build with warnings-as-errors; isolated SQLite Identity bearer HTTP; independent stage/manifest/hash postflight and scoped review. No PostgreSQL, CT164/CT152, live DB, claim decision mutation or deployment. Evidence/retention approval remains a separate prerequisite for any future real decision endpoint.


## 2026-11-13 — F2a-3 reviewer authorization QA accepted

- Bounded child PR [#23](https://github.com/jvalkovv/YangSpaceApp/pull/23) **MERGED** into the F2a-1 parent feature branch. Merge commit: `c54c6cc4f4cd58b184c5d6b0dc7ec6a7c3507808`.
- Exact tested source SHA: `e4ba6bbd697eeaeec4bf6fafe81ab78e30d3f5c4`.
- Windows native parser, .NET restore, Release build with warnings-as-errors, isolated Identity bearer HTTP and independent evidence postflight: **PASS**.
- All **27/27** required HTTP contract markers passed. Exact source identity, clean worktree, manifest, three stage receipts, stage commands, no failure gate and SHA-256 receipts independently verified.
- Evidence: `D:\GitHub\YangSpaceApp-F2a3-ReviewerAuth-QA-v2\logs\yangspace\f2a3-reviewer-auth\e4ba6bbd697e-20261113-061414-30f957ed`.
- Manifest SHA-256: `7c3c3894c229f9020c9892f8b618d0220b79dd4561e64b8eea26b8a0385aa9d0`.
- HTTP stdout SHA-256: `a1007fed8f985d94bfcceb7ee221425b2d2ea55f6571ba7e68c2a9a87313f2b7`.

**Scope accepted:** live Identity reviewer-role authorization/revocation, conflict-policy helper, default-OFF QA guardrails and disposable SQLite bearer HTTP. This is not PostgreSQL bearer HTTP or claim mutation acceptance.

**Next gate:** Issue #20 remains OPEN. Before a real Approve/Reject endpoint, explicitly approve evidence categories, accepted proof checklist, private evidence access/storage, retention/erasure and legal basis. Then create a separate decision-mutation child with PostgreSQL HTTP, parallel claim writers, transaction rollback and uncertain-COMMIT reconciliation tests. Parent PR #17 remains Draft/HOLD. No GitHub Actions, CT164/CT152, live DB or deployment.


## 2026-11-13 — F2a-3 Evidence & Retention Policy OWNER-APPROVED

Canonical policy v1: [F2A3_EVIDENCE_AND_RETENTION_POLICY_20261113.md](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-f2a1-domain-foundation-20261111/docs/F2A3_EVIDENCE_AND_RETENTION_POLICY_20261113.md).

**Owner approval recorded:** 2026-11-13. Approved operational defaults cover evidence categories/proof thresholds, minimisation/redaction, private claim-scoped access/storage, file allowlist/limits, retention, deletion, audit and backup recovery.

**Legal/privacy gate remains BLOCKED:** [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) tracks controller/processor roles, lawful basis and necessity assessment, privacy notice, processor/subprocessor and transfer review, DPIA screening, data-subject requests and final written clearance. Owner approval is not legal clearance.

No real evidence collection/processing, production upload/storage or live claim-decision activation is authorized until Issue #24 is accepted. Isolated non-production work using synthetic data only may proceed if it cannot accept real evidence and remains disabled from production.

**Next:** [Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25) is opened for the bounded synthetic-data-only evidence handling foundation; [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains the blocking legal/privacy gate for any real evidence. Parent PR #17 remains Draft/HOLD.


## Historical checkpoint — F2a-3 synthetic evidence policy candidate (pre-QA)

[Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25) was implemented in [PR #26](https://github.com/jvalkovv/YangSpaceApp/pull/26), exact tested source SHA `519809875658e7e27232d6a49536f2d71933ea69`.

At this pre-execution checkpoint the candidate was still NOT WINDOWS-TESTED / DRAFT / HOLD. The later exact-SHA acceptance and squash merge are recorded at the top of this roadmap. [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains the hard legal/privacy gate before any real evidence collection or production activation.

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

