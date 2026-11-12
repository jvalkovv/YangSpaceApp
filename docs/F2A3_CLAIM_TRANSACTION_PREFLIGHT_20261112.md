# F2a-3 — disposable claim transaction preflight

**Status:** SOURCE CANDIDATE / NOT BUILD-TESTED / DRAFT / NOT ACCEPTED.  
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
