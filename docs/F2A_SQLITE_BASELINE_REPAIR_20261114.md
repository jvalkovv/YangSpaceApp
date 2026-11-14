# F2a-S SQLite migration baseline repair (Issue #38)

**Source:** F2a parent `1f1307250eed0d2e5df24570eb560e5ea16e20ee`.
**Problem:** SQLite provider's latest snapshot is older than F2a organization/claim domain model. F2b O2 outbox scaffold accidentally produces a combined migration including four missing entities, `Locations` field additions and EF's table rebuild. PostgreSQL outbox migration delta is clean. This is migration-history drift, not merely a SQL validator bug.

## Safety and procedure

- The only intended change for **F2a-S1** is a separately reviewed SQLite migration `F2aOrganizationClaimFoundation`, before any outbox migration. Use the current F2a model *without* an outbox DbSet, by running this new branch based on the F2a parent.
- `scripts/New-F2a-Sqlite-Baseline-Offline.ps1` is an **offline scaffold**: clean detached exact SHA, design-time SQLite in-memory connection factory, pinned dotnet-ef 10, .NET Release, source-only generated C# and SQL. It should produce exactly two new migrations C# files and one modified SQLite snapshot (no PostgreSQL migration changes).
- SQL allowlist expects exactly four new domain tables plus `ef_temp_Locations` used by the SQLite table-rebuild mechanism. No outbox. The script does **not** approve `DROP TABLE "Locations"` as safe: it requires the copy/rename pattern to be available for review, but safety is only established by a **disposable historic SQLite upgrade and backup/restore**.
- Before accepting F2a-S2 verify rows, keys, all previous Location fields/foreign keys, offers/brochures/jobs, Identity users/roles, EF history, pending claims, audit constraints and per-tenant isolation. Also test interruption/recovery and no data loss using disposable synthetic data; never apply to CT164.
- Only after standalone F2a migration + disposable upgrade/restore **PASS**, merge this scoped PR into F2a parent #17. New parent SHA requires updated/regression QA. Reconcile F2b #29 and outbox #37 *after* accepted F2a migration and rerun clean O2 scaffolding for exactly `IdentityEmailOutbox`.
- Generator source is NOT tested on operator Windows. All PRs stay Draft/HOLD. No GitHub Actions or automatic deployment.

**Evidence from original failed F2b O2:** SQLite SQL SHA256 `51c5e002aab53e8f4713d247304f727e012c8ee9271e0dd3a065eab3375a027d`; PostgreSQL outbox SQL SHA256 `3feb831210a87a64f1bacff9ffbb53dc17c20b464fc8ec8b380ac963759b2c50`. Original source `f84da94bb7c5a6334a79c607fb4543227e2be69d`. Preserve unsuccessful O2 worktree; do not copy its generated migrations into the new baseline PR.

## 2026-11-14 — operator checkpoint and SQL regex fix (pre-runtime QA)

Operator transcript at source `3eea6ee387b12fcbf79e51ac30b2739501025bf2` showed three uncommitted EF artifacts in the existing exact-SHA detached Windows worktree (migration `20261114172551_F2aOrganizationClaimFoundation`, Designer, changed SQLite snapshot). Subsequent scaffold rerun failed its expected `F2A_S1_DIRTY_WORKTREE` guard. Static C# console review suggests F2a-only changes; no reviewed SQL, independent local artifact hashes, disposable historic-database upgrade or restore acceptance was supplied. The operator looked for evidence under the main repo instead of the detached worktree; do not infer the generation stage result from that search.

Source QA defect fixed in this branch: both generator and independent postflight had `"ef_temp_Locations"\b` after the closing quoted identifier. This fails for valid EF SQLite `INSERT INTO "ef_temp_Locations" (...) SELECT ...`. The replacement `"ef_temp_Locations"(?=\s*\()` checks the expected opening column list without rejecting valid SQL. **Prior generated artifacts at old SHA must be preserved; this source fix changes the branch SHA.** Run a NEW clean detached worktree at the new head; do not reset/clean the old operator worktree or copy unchecked generated files. Tests and operator QA on the new head are still **NOT RUN / HOLD**. No live DB, CT164 or GitHub Actions.


## F2a-S2 — disposable historic upgrade and restore-recovery contract

The Windows operator runner is `scripts/Test-F2a-Sqlite-Organization-Upgrade.ps1`; its runtime contract is `apps/api.RuntimeTests/F2aOrganizationClaimUpgrade.cs`, selected only with `--f2a-organization-claim-upgrade-only`.

The runner consumes the exact S1 generated artifacts from the hash-verified operator evidence at source `35c9f576bbc64c0825c1a117b3ee415d3ca9869d`. It verifies the S1 manifest and each generated migration/designer/snapshot SHA256 before copying those artifacts into a clean exact-branch test worktree. The generated migration artifacts remain uncommitted after QA.

The disposable runtime contract:
1. Applies only the two previously accepted migrations to a newly created SQLite file.
2. Seeds synthetic historic Location, Offer, Brochure, Job and ASP.NET Identity user/role plus role/user claims, login, token and user-role data.
3. Creates a pre-F2a SQLite online backup.
4. Upgrades a distinct clone using EF Core's real `IMigrator` pipeline to `20261114183515_F2aOrganizationClaimFoundation`.
5. Verifies all original Location fields and child records, nullable new Location ownership, default revision, Identity rows/dependencies, exact migration history, new F2a relationships, FK check and SQLite integrity.
6. Injects failures before the SQLite FK-off command (after the initial F2a DDL segment), before SQLite foreign keys are re-enabled after the Locations rebuild, and before the F2a migration-history insert. Each failed clone is recovered by restoring the verified pre-upgrade backup to a separate file; the test deliberately does not retry the migration on a partially applied database.

The fault-injection cases prove the restore-only recovery path for these observed command boundaries. They do not simulate host power loss, storage corruption, process kill at every machine instruction, or prove off-host backup durability. Those remain separate operational gates.

Operator invocation (run in the exact test branch worktree, with the original S1 worktree preserved):

```powershell
.\scripts\Test-F2a-Sqlite-Organization-Upgrade.ps1 `
    -CommitSha <EXACT_TEST_BRANCH_HEAD_SHA> `
    -SourceWorktree 'D:\GitHub\YangSpaceApp-F2a-Sqlite-Baseline-QA-R2'
```

Acceptance requires all runner markers and a reviewed evidence manifest. No HTTP listener, CT164, PVE, production connection string, deployment or GitHub Actions are used. PR remains Draft/HOLD until the operator result is reviewed and the generated migration artifacts are committed with their verified hashes.

## 2026-11-14 — S2 generated-artifact preflight correction

Operator rerun at test source `66e654810b8eced7482f01a0c4e46f27f5adbc63` passed PowerShell syntax and the S1 manifest gate, then stopped before artifact copy, restore/build or SQLite execution. The error printed all three expected generated paths as one value. Root cause: ambiguous PowerShell array construction combined string concatenation (`+`) with comma-separated entries in both `$expectedFiles` and the later `$expectedStatus` exact-change-set list.

Both lists now use explicit `[string[]]` declarations and scalar format-string elements. The expected file list also has a three-entry/scalar guard. The S1 source worktree and its uncommitted generated artifacts were not modified.

**Next exact S2 runner source:** `507645cbf2e8dcea4095b1ac5058f27c19356ee9`.

Current acceptance remains:
- S1 manifest verification: PASS.
- S2 PowerShell syntax: PASS on prior runner revision; rerun required on updated SHA.
- S2 generated artifact copy/hash verification: PENDING.
- .NET restore/build: NOT STARTED.
- Disposable historic SQLite upgrade: NOT STARTED.
- Fault injection/restore recovery: NOT STARTED.
- CT164/production: NOT ACCESSED.

## 2026-11-14 — S2 null-safe changeset gate and resumable artifact copy

The operator rerun at source `132faba89f26c98f60224ff191c094479661ae8b` passed the S1 manifest gate, then failed with a strict-mode missing `Count` property. Source inspection identifies the failing expression: `(Compare-Object ...).Count`. When the expected and actual changesets are identical, `Compare-Object` emits no objects (null), so strict mode rejects the property access. This occurs after the runner has copied the three generated S1 artifacts and before it creates the S2 evidence directory or starts .NET restore/build.

The runner now:
- wraps every `Compare-Object` result in an array before checking `.Count`;
- accepts either a clean test worktree or exactly the three expected generated artifact changes;
- re-hashes any already-present destination artifact against the S1 source before proceeding, and copies only missing artifacts;
- still rejects any additional or altered worktree changes.

This makes the operator rerun safe if the previous attempt already copied the generated migration/designer/snapshot. No S1 source artifacts are modified. No database was opened by the failed attempt.

**Next exact S2 runner source:** `33d9843ae689c1ef643a340e85e6e1c665b2f0b4`.

Acceptance remains HOLD until the runner passes generated-artifact verification, .NET restore/build, disposable SQLite upgrade, and all fault-injection/restore-recovery cases. CT164/production remain untouched.

## 2026-11-14 — S2 runtime execution and first fault-boundary expectation correction

Operator run at exact test source `ce7dd1c9f998a5fc5f4c1aea57c04789c76d3873` established:
- S1 manifest, exact source and generated artifact SHA256: PASS.
- .NET RuntimeTests restore and Release build: PASS.
- Disposable historic SQLite upgrade: PASS.
- All seeded Location fields and Offers/Brochures/Jobs preserved: PASS.
- Identity users/roles/claims/logins/tokens and role membership preserved: PASS.
- Four F2a tables, Location/Organization relationships, indexes, SQLite integrity and FK checks: PASS.
- No Outbox table: PASS.

The first injected failure was correctly triggered before the SQLite `PRAGMA foreign_keys = 0` command. The test then failed its own expectation that Location's new `OrganizationId` and `Revision` columns were still absent. The failure marker proves the migration has already executed preceding DDL before the FK-driven SQLite table rebuild. The new columns are introduced before the FK-off/rebuild segment; at this interruption boundary they already exist with the legacy Location row retained, `OrganizationId IS NULL`, and `Revision=0`. The operator's next run at `3aeedb0730470e9394b36b92ef632916149ca88f` observed that `ef_temp_Locations` also already exists before the intercepted `PRAGMA foreign_keys = 0` command. The first fault boundary therefore expects the temporary table to exist while the original `Locations` table and its row remain intact. This is an observed command-order fact, not an inferred EF convention.

Corrected only the first fault-boundary expected state to require those two columns. The second boundary (before FK re-enable, after the rebuild) and third boundary (before EF migration-history insert) remain unchanged. This is a **test expectation correction**, not a migration/schema change.

**Next exact S2 test source:** `bad3c9670c4f51ce562465d545aeb50ee785a41b`.

S2 acceptance remains HOLD. The three fault-injection recovery scenarios must all pass on the updated exact source and the full evidence must be reviewed. The successful disposable upgrade does not authorize production apply. CT164/production were not accessed.

The interrupted-state assertion now emits the observed `Locations` column set, the individual `OrganizationId`/`Revision` states, Organizations/temp-table presence, and F2a history count before evaluating expectations. These diagnostics are retained in the operator stdout evidence if a later boundary still disagrees with the generated SQL.

## 2026-11-14 — S2 first fault boundary: temp table creation precedes FK-off interception

Operator rerun at exact source `3aeedb0730470e9394b36b92ef632916149ca88f` again passed S1 manifest/artifact hash gates, .NET restore/build, the successful disposable historic upgrade, data preservation, Identity preservation, F2a relationships, integrity/FK checks and no-Outbox assertions.

The first injected failure was triggered before execution of `PRAGMA foreign_keys = 0`. New diagnostics observed:
- `OrganizationId=True`
- `Revision=True`
- `Organizations=True`
- `ef_temp_Locations=True`
- F2a migration-history row count = 0

The failure was the test expecting `ef_temp_Locations` to be absent. The actual runtime command order has already created that temporary rebuild table before the intercepted FK-off command. The first boundary now expects the temp table to exist, while the original `Locations` table/row remain present and the F2a migration history remains unadvanced. No migration SQL, model or generated artifact was changed.

**Next exact S2 test source:** `69bb5a6a34215ff2200a69f76183aeb8bce73bb3`.

Acceptance remains HOLD. The next run must pass the first interrupted-state checks, verify backup restore, then exercise the later post-rebuild and pre-history fault boundaries. CT164/production remain untouched.

## 2026-11-14 — S2 full disposable upgrade and recovery operator QA PASS

The Windows operator reran the exact S2 source `93c620e0bbc61d73c8892bafe15fee6772ab0e9a` using the existing S2 worktree and preserved S1 evidence/artifacts.

Observed operator output:
- `F2A_S2_S1_MANIFEST=PASS`
- `F2A_S2_EXACT_TEST_SOURCE=PASS`
- `F2A_S2_GENERATED_ARTIFACT_HASHES=PASS`
- Runtime restore stage: PASS
- Runtime Release build stage: PASS
- Disposable historic upgrade and all recovery scenarios stage: PASS
- `F2A_S2_OPERATOR_QA=PASS`

The runner only prints the overall PASS after verifying all of these markers in the retained runtime stdout:
- Legacy baseline and successful disposable upgrade.
- Every legacy Location field and Offers/Brochures/Jobs preservation.
- Identity users, roles and dependent records preservation.
- New F2a schema relationships and no-Outbox assertion.
- SQLite integrity and foreign-key integrity.
- Failure injection at all three migration command boundaries.
- Restore recovery from the verified pre-upgrade backup after each injected failure.
- No blind retry of a partially applied migration.

Evidence directory:
`D:\GitHub\YangSpaceApp-F2a-S2-Upgrade-QA-20261114\logs\yangspace\f2a-sqlite-org-upgrade\93c620e0bbc6-20261114-224645-5294f69e`

**S2 runtime acceptance: PASS at the exact tested source above.** The runner's generated evidence manifest intentionally records `RELEASE_GATE=HOLD_PENDING_OPERATOR_EVIDENCE_REVIEW`. Therefore this result is not yet a release/merge approval: the complete manifest, stage receipts, runtime stdout/stderr and worktree final status still require independent review.

Still pending:
1. Review the S2 evidence directory and verify the manifest and stage receipts.
2. Commit the three S1-generated migration/designer/snapshot files using their already verified hashes.
3. Run exact-source build/regression after that artifact commit.
4. Only then decide whether PR #39 can leave Draft/HOLD and be merged into parent #17.

The operator runner reports `MIGRATION_ARTIFACTS=UNCOMMITTED`, `PRODUCTION_DATABASE=NOT_ACCESSED`, `CT164=NOT_ACCESSED`, `HTTP_LISTENER=NOT_STARTED`, and `PVE_DEPLOYMENT=NONE`. No production or CT164 database was changed.

## 2026-11-14 — independent S1/S2 operator evidence archive review

Reviewed the operator-uploaded `YangSpace-F2a-S1-S2-Evidence-Review.zip`.

Archive/evidence integrity:
- ZIP integrity test: PASS.
- Archive SHA256: `DBDEAE9D7E5F335D76B728A9332EBD1811CA199DFF75E8C3FFE27F3364D32689`.
- 37 entries: 27 S1 receipts and 10 S2 receipts.
- All 11 captured stderr logs are empty.
- S1 SQL review artifact SHA256 recalculated from the archive and matches `SHA256SUMS.txt`: `b85e7995c5388396c52c55496689413d03d8499fbbd8f30c807ea20ab9108202`.
- S1 build-before and build-after receipts both report Release build success with 0 warnings and 0 errors. EF tool restore/version receipts show dotnet-ef 10.0.0. Model-diff receipt reports no changes since the generated migration.
- S1 manifest correctly remains `RELEASE_GATE=HOLD`, `DISPOSABLE_UPGRADE=NOT_RUN`, and `LIVE_DB_APPLY=NOT_RUN`.

SQL review:
- Exactly four F2a domain tables are created: Organizations, BusinessClaims, ContentAuditEvents and OrganizationMembers.
- `ef_temp_Locations` is the SQLite table-rebuild staging table.
- The SQL includes the expected Locations copy/rebuild, drop/rename and FK PRAGMA transitions.
- No Outbox table or Outbox reference is present.
- The SQL archive checksum was independently recalculated and matches the recorded S1 checksum.

S2 runtime receipts:
- S2 manifest matches tested commit `93c620e0bbc61d73c8892bafe15fee6772ab0e9a` and S1 source `35c9f576bbc64c0825c1a117b3ee415d3ca9869d`.
- Restore and Release build receipts pass; build reports 0 warnings and 0 errors.
- Runtime stdout contains every required success marker: historic baseline, successful upgrade, all Location fields, Offers/Brochures/Jobs, Identity records/dependencies, F2a tables and relationships, no-Outbox, SQLite/FK integrity, all three injected failure boundaries, restore-only recovery for each, and all-boundaries summary.
- The test code uses disposable files under the OS temp directory and deletes its fixture directory in `finally`.
- S2 manifest records no production DB, CT164, HTTP listener or PVE deployment access.

Operator preflight note:
- The uploaded transcript shows `S2_TEST_HEAD_MISMATCH` in the later archive-review command because its local `$TestSha` literal omitted the final `a`. The correct 40-character tested SHA is `93c620e0bbc61d73c8892bafe15fee6772ab0e9a`.
- This is a typo in the post-run evidence-packaging check, not the S2 test invocation: the S2 runner itself checks the supplied commit against both local HEAD and the fetched origin branch before running and only then prints `F2A_S2_EXACT_TEST_SOURCE=PASS`. Its S2 manifest records the correct 40-character SHA.

Remaining artifact gate:
- The ZIP contains the S1 `SHA256SUMS.txt` but not the three generated EF C# artifacts themselves. Therefore the archive alone cannot independently recompute those three file hashes.
- The S2 runner did verify the three actual S1 worktree artifacts against those checksums before copying them into the S2 worktree; its manifest records `GENERATED_ARTIFACT_HASHES=PASS`.
- The operator's final status output confirms exactly the expected three uncommitted paths: modified SQLite snapshot plus new migration and Designer. Before committing, re-hash those three current files in the S2 worktree against the S1 `SHA256SUMS.txt` and confirm the exact three-file status.

**Evidence review result: PASS for S1/S2 receipts, SQL checksum and disposable runtime/recovery acceptance. Release/merge remains HOLD only for current three-file re-hash, commit, and exact-new-HEAD build/regression.** No production or CT164 access is authorized by this QA.
