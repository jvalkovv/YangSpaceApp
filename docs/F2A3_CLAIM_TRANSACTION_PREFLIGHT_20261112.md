# F2a-3 — disposable claim transaction preflight

**Status:** **EXACT-SHA ISOLATED QA ACCEPTED**: Windows restore/build/transaction PASS and operator read-only evidence postflight PASS at tested **code** SHA `91f34f17a52e11dd107645ebb44ea0e578e4fa73`; production PostgreSQL reviewer/authorization/mutation remains **NOT TESTED / HOLD**.  
**Dependency:** [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) and the [proposed security contract](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-v2-foundation-20261108/docs/F2A3_CLAIM_REVIEW_SECURITY_CONTRACT_20261112.md). Reviewer policy, claim evidence/retention, transfer and re-grant semantics still require owner approval. 

## What this slice does

- Adds **test-only** `ClaimTransactionContract` to the existing `apps/api.RuntimeTests` executable. A new exclusive `--claim-transaction-only` switch runs this contract without starting any public API, connecting to configured databases or changing application startup guards.
- Uses a fresh SQLite `:memory:` database with foreign keys enabled and real EF `DirectoryDb` entity/index mappings. All Identity user, claim, organization, location, member and audit records are **synthetic fixtures**; no claimant credentials or real review decisions are accepted.
- Success path exercises a single EF transaction that marks synthetic Pending claim Approved, activates its organization, links a previously unclaimed location, inserts exactly one initial Owner membership and four per-entity allowlisted audit records; verifies all persisted rows from an independent EF context.
- Fault path intentionally inserts a duplicated `ContentAuditEvent` `(EntityType, EntityId, RevisionAfter)` index key to force a real SQLite constraint exception *after staging claim, organization, location, membership and audit writes in the same transaction*; explicitly rolls back; verifies the second synthetic fixture retains Pending claim, pending organization, NULL owner, zero membership and zero audit records.
- Implements **no** reviewer authentication, evidence verification, idempotency fence, real PostgreSQL application transaction, parallel writer support, claim review endpoint, public registration or production configuration. It is a **feasibility regression**, not a live security control or completed F2a-3.

## Exact-SHA Windows acceptance

From a dedicated detached and clean worktree at the exact child branch tip, run `scripts/Test-F2a3-Claim-Transaction-Local.ps1 -CommitSha <40-char-current-branch-sha>`. The runner checks local Git HEAD, remote F2a-3 tip, allowed branch, clean worktree, .NET restore with NuGet audit warnings-as-errors, Release build warnings-as-errors, and the isolated transaction contract with explicit markers.

Required markers:

```text
CLAIM_TX_DISPOSABLE_SQLITE=PASS
CLAIM_TX_APPROVAL_PERSISTED_ATOMICALLY=PASS
CLAIM_TX_AUDIT_CARDINALITY=PASS
CLAIM_TX_DUPLICATE_AUDIT_FORCED_FAILURE=PASS
CLAIM_TX_FAILURE_ROLLBACK_ALL_ROWS=PASS
CLAIM_TX_SCOPE=SQLITE_ONLY_NO_HTTP_AUTH
F2A3_CLAIM_TX_ISOLATED_ONLY=PASS
```

Evidence is under `logs/yangspace/f2a3-claim-tx/<sha12>-<run-id>/` and must include restore/build/transaction stdout/stderr/runner receipts and `manifest.txt`. A `gate.txt` denotes HOLD. Operator must publish exact SHA, stage PASS, evidence path and postflight hashes. **Do not report PASS before actual Windows execution.**

## Follow-up acceptance gates

1. User explicitly approves/adjusts Issue #20 policy: offline `MerchantClaimReviewer` provisioning, applicant/self-conflict, claim evidence & retention, no blind ownership transfer, reactivation of revoked membership, last-owner protection and audit event cardinality.
2. Independently review the test code/fixtures and execute exact-SHA Windows acceptance; merge only when its *own isolated QA-only DoD* is fully complete.
3. Create a separate mutation implementation PR with real Identity bearer, live role lookup, transaction and audit retry/commit semantics. It must use a reviewed local-only PostgreSQL HTTP integration topology and real Npgsql application transactions. This preflight does **not** satisfy those gates.
4. Parent [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17), V2 [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1), CT164/CT152/live DB and public merchant routes stay DRAFT/HOLD or inactive as before. No GitHub Actions or infrastructure mutations.

## 2026-11-12 15:31 Europe/Sofia — exact-SHA operator run and evidence postflight

**Tested application/test code SHA**: `91f34f17a52e11dd107645ebb44ea0e578e4fa73`. Operator Windows source identity and clean worktree PASS; PowerShell runner syntax PASS; `STAGE_PASS=f2a3-restore`, `STAGE_PASS=f2a3-build`, `STAGE_PASS=f2a3-transaction`; `F2A3_CLAIM_TX_ISOLATED_ONLY=PASS`.

**Evidence directory**: `D:\GitHub\YangSpaceApp-F2a3-ClaimTx-QA\logs\yangspace\f2a3-claim-tx\91f34f17a52e-20261112-151758-9c434338`.

**Read-only operator postflight**: `F2A3_MANIFEST=PASS`, `F2A3_STAGE_FILES=PASS`, `F2A3_TRANSACTION_MARKERS=PASS`, `F2A3_EVIDENCE_POSTFLIGHT=PASS`. Checks exact manifest key-value scope including `TEST_PROVIDER=SQLITE_DISPOSABLE_MEMORY`, `REAL_IDENTITY_REVIEWER_HTTP=NOT_TESTED`, `REAL_POSTGRESQL_APP_TRANSACTION=NOT_TESTED`, `PARALLEL_WRITERS=NOT_TESTED`, `CLAIM_REVIEW_POLICY=NOT_APPROVED`, `CLAIM_MUTATION_ROUTE=NOT_EXPOSED`, `CT164_DB=NOT_ACCESSED`, `CT152=NOT_ACCESSED`, `RELEASE_GATE=HOLD`. No `gate.txt`, all three runner/stdout/stderr triples present, six transaction markers present. Operator recomputed SHA-256 of every evidence file. Underlying local evidence files were not separately uploaded to GitHub.

| Evidence file | SHA-256 (operator-computed) |
| --- | --- |
| `f2a3-restore.runner.cmd` | `4BD88AF53B0848FD9E3B1CD01028A1B584D66E04722BB38339105E162F0A2268` |
| `f2a3-restore.stdout.log` | `DE161AB916F3D1FFD5FE07B3E50FFBFE565B5C1C1B7629BEA78BEAD26E4B13E2` |
| `f2a3-restore.stderr.log` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` (empty) |
| `f2a3-build.runner.cmd` | `0D5C68518964241FB13BA5637A87E746BD25DD53AC80ED9F93913F6179E4739D` |
| `f2a3-build.stdout.log` | `5C0891403539074CC6A4088513C718597BEA6A00AF1E2042A2D45BB8AEFD707B` |
| `f2a3-build.stderr.log` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` (empty) |
| `f2a3-transaction.runner.cmd` | `D46C5ADA0AD0B7A9D0B87411A3F362754BE03E7334380B70688EC8750507BB6B` |
| `f2a3-transaction.stdout.log` | `0812D68FC76DD152AE63F7CC6F5F3EBA1D58058A03D06DDD7A7A8258E75127F4` |
| `f2a3-transaction.stderr.log` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` (empty) |
| `manifest.txt` | `ED0069DB82C9339D0C98290715C1BA1B655741B5D5D723D8DD5D0152DFFB7442` |

**Scope accepted:** disposable SQLite EF synthetic transaction with four audit records; on the failed second `SaveChangesAsync`, the explicit outer transaction rolls back the claim, organization, location ownership, member grant and audit records. Nothing here validates identity reviewer authorization, actual PostgreSQL EF application transaction, concurrent clients, uncertain COMMIT, or any live deployment. Parent [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) and V2 [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) remain HOLD for their larger security/release gates.
