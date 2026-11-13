# F2a-3 — Reviewer-controlled claim decisions and atomic Owner membership

**Date:** 2026-11-12 (Europe/Sofia)  
**Status:** **TECHNICAL SECURITY POLICY APPROVED — BOUNDED REVIEWER-AUTHORIZATION QA ACCEPTED (PR #23 MERGED); EVIDENCE / RETENTION GATE OPEN; CLAIM-DECISION RELEASE HOLD**. Owner approval was given on 2026-11-12. This authorizes only the isolated reviewer-authorization QA slice; it does not authorize claim-decision mutations, evidence collection, merchant onboarding, production flags, credentials or live environment changes.  
**Primary work item:** [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20), derived from [Issue #4](https://github.com/jvalkovv/YangSpaceApp/issues/4); F2b content-audit follow-on is [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11).

## 2026-11-12 — owner decision and implementation boundary

The owner explicitly approved the proposed **technical security policy** in the active project conversation.

Approved technical decisions:
- Dedicated offline-provisioned `MerchantClaimReviewer` role; `PlatformAdmin` is not implicitly a reviewer; no HTTP self-grant.
- Check current persisted Identity role membership on every reviewer request/decision; the same bearer must lose access immediately after role revocation.
- Deny applicant self-review and deny review by an active member of the target organization.
- Claim decision model remains `Pending → Approved|Rejected` with `expectedRevision`; a decision is immutable.
- Approve only when `BusinessLocation.OrganizationId IS NULL`; no direct takeover of an already-owned location.
- First-time applicant Owner membership, location ownership, claim decision and per-entity audit must be one database transaction.
- No implicit revoked-member re-grant; no last-Owner removal in the first decision slice.
- Audit remains allowlisted and immutable; use a unique operation/idempotency fence and independent reconciliation for an unknown COMMIT. Never blindly retry.
- Separate, default-OFF QA feature gate; no Production/Development activation and no coupling to `MerchantRead`.

**Not approved / separate mandatory gate:** evidence categories, acceptable proof checklist, private evidence access/storage, retention duration, deletion/erasure and legal basis. These must be documented and explicitly approved before any real Approve/Reject endpoint can be implemented or enabled.

**Current authorized work:** reviewer identity authorization, live role-revocation enforcement, claim-specific conflict-policy helper and isolated SQLite Identity bearer HTTP tests only. The QA access probe is not a claim decision route. No claim mutation, evidence storage, owner grant, audit write, migration or deployment is authorized by this decision.

## 1. Accepted technical baseline (not release)

- [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) merged read-only Identity bearer TestServer authorization, default-OFF and QA-only with disposable SQLite. This is not a PostgreSQL bearer runtime certification.
- [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) exact code SHA `445db4d9a5d28b798054c426afeba9b9be2c771b`: two Windows combined source/tenant/disposable PostgreSQL regression passes and operator read-only evidence postflight PASS. SQL `2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7`; backup `94c7c2cb1a2fcb93633c23efcb642764baea04f866e464e0728a22e06bdd664e`. Tested schema/constraints/restore only, not claim mutations or release.
- Current EF entities: `Organization`, `OrganizationMember`, `BusinessClaim`, `BusinessLocation`, `ContentAuditEvent`. `BusinessClaim.Status` and `Organization.Status` are string-converted enums; `BusinessClaim.Revision`, `Organization.Revision`, `OrganizationMember.Revision` and `BusinessLocation.Revision` are application-managed concurrency tokens.
- Existing DB constraints: one Approved claim per `LocationId`; one Pending claim per `(LocationId, OrganizationId)`; unique membership `(OrganizationId, UserId)` even if revoked; audit uniqueness `(EntityType, EntityId, RevisionAfter)`. `BusinessLocation.OrganizationId` stays `NULL` until explicitly verified; non-null existing owner is never silently overwritten.

## 2. Owner-approved technical decisions and separate gates

| Policy | Decision | Status |
| --- | --- | --- |
| Reviewer authority | Dedicated offline-provisioned `MerchantClaimReviewer`; separate from `PlatformAdmin` and merchant Owner/Editor; no HTTP self-grant | **APPROVED** |
| Reviewer revocation | Read current Identity role membership from persistent storage on every request/decision; same-token revocation must deny immediately | **APPROVED** |
| Conflict of interest | Deny applicant reviewing own claim; deny an active member of the target organization reviewing that claim; actor comes only from bearer subject | **APPROVED** |
| Claim evidence | Evidence categories, reviewer checklist, private access/storage, retention, erasure and legal basis | **SEPARATE APPROVAL GATE OPEN** |
| Scope of current QA slice | Default-OFF reviewer access probe + live role check + conflict-policy helper/tests; no claim mutation route | **APPROVED / QA ONLY** |
| Claim lifecycle | Future decision is `Pending → Approved` or `Pending → Rejected`, with required `expectedRevision`; decided claims immutable | **APPROVED FOR FUTURE MUTATION SLICE** |
| Location ownership | Approve only if target `BusinessLocation.OrganizationId IS NULL`; existing owners require a separately verified transfer workflow | **APPROVED** |
| Member onboarding | First-time approval creates exactly one Owner membership atomically with the claim/location changes | **APPROVED FOR FUTURE MUTATION SLICE** |
| Re-grant / last Owner | No implicit revoked-member re-grant or last-Owner removal in the first decision slice; any later lifecycle requires its own audited policy | **APPROVED** |
| Audit cardinality | One allowlisted immutable event per changed entity/revision, inside the same transaction; no raw evidence, secrets or request JSON | **APPROVED FOR FUTURE MUTATION SLICE** |
| Duplicate / replay | Unique stored operation/idempotency fence; unknown COMMIT requires independent reconciliation, never blind retry | **APPROVED FOR FUTURE MUTATION SLICE** |

## 3. Proposed API and authorization boundary

- Future mutation route sketch: `POST /api/v1/staff/claims/{claimId:guid}/decision`, server-observed `Approve|Reject`, `expectedRevision` and bounded `reason` (max 500 characters). It is **not authorized or mapped in the current QA slice**; evidence/retention approval is a prerequisite.
- Current QA probe: `GET /api/v1/staff/claim-review/access`, only when the separate `ClaimReview:QaEnabled` gate is enabled in `YangSpaceQa` with disposable SQLite. It returns no claim or user data and performs no writes.
- Use ASP.NET Identity **BearerScheme** only and a dedicated authorization policy. A `PlatformAdmin` without the reviewer role remains forbidden. Always check the role live in persistent Identity state, with explicit denial if reviewer revoked after token issuance.
- Never trust `ActorUserId`, membership, organization ownership, role or reviewer metadata in headers/body. Resolve actor ID from the authenticated subject; resolve applicant, target org, location and current claim solely via DB.
- 401 for missing/invalid bearer; 403 for authenticated but globally unauthorized reviewer; generic 404 for unknown/foreign/ineligible scoped resource; generic 409 for expected-revision or competing decisions. Responses never disclose competing claimant details.
- New QA-only review flag is separate from existing `MerchantRead:Enabled` (currently SQLite+YangSpaceQa-only), Preview or Admin feature gates. Production and Development must fail closed. No public registration or access-grant routes.

## 4. Proposed transactional approval algorithm

1. Inside **one** `DirectoryDb` with one explicitly pinned relational connection and transaction, resolve authenticated reviewer and verify the live dedicated reviewer role; reject conflict of interest.
2. Load Pending claim, applicant, target organization and location using claim ID and expected revision; refuse any already-owned location (`OrganizationId != NULL`), suspended or ineligible target organization, and invalid evidence-check result.
3. Acquire a database-level concurrency boundary for the claim and location (row lock / conditional update with original revisions). Enforce the already-reviewed partial UNIQUE indexes and handle unique constraint conflicts as generic 409.
4. Update claim to Approved with verified reviewer/reason/time and increment claim revision. Update location `OrganizationId`, revision and server UTC change time. Activate organization only according to accepted verification policy; update its revision.
5. Insert exactly one new Owner membership for applicant if not present; if a revoked membership row already exists, treat this as a policy/flow conflict in the first slice (do not bypass the current unique index). Never insert a second active Owner record or demote existing Owner implicitly.
6. Insert allowlisted audit records for each changed business entity, tied to authenticated reviewer, bounded reason, correlation and revision before/after. An audit FK/UNIQUE failure must roll back **all** domain changes.
7. Commit once. On uncertain COMMIT acknowledgement, **never blindly repeat** the transaction; use the approved unique operation fence and independent outcome reconciliation before returning/retrying. Do not fire an external event pre-commit.
8. Rejection is a separate single-transaction `Pending→Rejected` + audited revision update; it must not grant membership, alter location ownership or activate the organization.

## 5. Required failure and race matrix (not implemented)

| Case | Required receipt |
| --- | --- |
| Anonymous, invalid bearer or cookie-only | 401; zero DB changes |
| Valid staff without `MerchantClaimReviewer`; revoked reviewer role with same token | 403; zero DB changes |
| Applicant self-review, reviewer active in target org, forged actor/request ownership fields | 403/404; zero claim/ownership/member/audit changes |
| Pending claim for already-owned location, foreign/unknown claims | 404/409 per reviewed policy, no resource identity leaks or takeover |
| Two reviewers approve competing pending claims for same location | Exactly one succeeds; losing request 409; one approved claim and one Owner grant, no orphan audit |
| Stale expectedRevision, replay or repeat approve/reject | Deterministic 409 or explicitly idempotent prior result, no duplicate audit or membership |
| Failure after claim mutation / location mutation / org status / membership grant / audit insert | Full rollback of all rows; independent second DB connection verifies zero partial effects |
| Failure just before/after DB COMMIT, including lost acknowledgement | No blind retry, once-only persisted result; reconcilable and observable unknown outcome |
| Revoked member re-grant / last Owner revoke | Explicit policy denial in this first slice; no duplicate or orphan membership |
| QA feature disabled, Production/Development, self-registration/owner writes | Route inaccessible and feature fails closed; live CT164/CT152 unchanged |

## 6. PostgreSQL HTTP test topology risk

The existing disposable PG runner deliberately uses `--network none` with no published ports/host mounts. **A host-side `WebApplicationFactory` cannot directly connect to that isolated server.** Do not claim 'real PostgreSQL bearer HTTP' from a SQLite TestServer or the existing PG schema-only runner. A separately reviewed **local-only** PostgreSQL integration topology is required (for example, isolated internal test network with no host-published ports and a co-located test process), alongside an explicit credential/cleanup/egress preflight. No change to the actual Proxmox deployment architecture is implied.

## 7. Acceptance sequence and immutable evidence

1. Technical reviewer/ownership/concurrency policy is **owner-approved on 2026-11-12** and recorded in [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20). Evidence categories, reviewer checklist, private evidence handling and retention/erasure remain a separate mandatory gate.
2. Current child PR #23 implements only QA-gated reviewer bearer authorization, live role revocation and conflict-policy checks on the F2a-1 feature branch. It does not expose claim mutations or write business/audit rows.
3. A later decision-mutation PR may begin only after the evidence/retention gate is explicitly approved; it must have its own isolated PostgreSQL HTTP/concurrency/fault-injection acceptance.
4. EF migration (if needed) generated and reviewed, with isolated PG apply/negative index tests and explicit migration/rollback receipts. Avoid hand-editing generated snapshots.
5. Real bearer HTTP tests in an approved isolated PostgreSQL topology and concurrent clients; all negative assertions verify status **and** zero DB side effects. Forced errors and uncertain COMMIT evidence must pass.
6. Exact-SHA .NET restore/build warnings-as-errors, static review, peer review, signed/hashed local manifests, cleanup and owner approval. Only then merge the scoped mutation PR into its feature parent.
7. **Out of scope:** active CT164/CT152, real user enrollment, production database/ingress, merchant writes, pilot launch and off-host disaster recovery. PR #17 and V2 PR #1 remain Draft/HOLD until their own broader acceptance gates are complete. No GitHub Actions.


## 2026-11-13 — reviewer authorization QA result

The bounded QA-only implementation in [PR #23](https://github.com/jvalkovv/YangSpaceApp/pull/23) passed exact-SHA Windows acceptance and independent evidence postflight at source SHA `e4ba6bbd697eeaeec4bf6fafe81ab78e30d3f5c4`; merged as `c54c6cc4f4cd58b184c5d6b0dc7ec6a7c3507808`.

The 27/27 isolated SQLite Identity bearer HTTP markers passed, including live same-token role revocation and fail-closed QA boundaries. This does not certify PostgreSQL bearer HTTP, claim mutation, Owner membership grant, audit transaction, concurrency, uncertain COMMIT or production.

The separate evidence categories/checklist, private evidence handling, retention/erasure and legal-basis gate remains OPEN. No real Approve/Reject endpoint may be implemented or enabled before that approval and its separate PostgreSQL/concurrency/fault-injection acceptance.


## 2026-11-13 — evidence, privacy and retention policy owner-approved

The owner approved the operational policy on 2026-11-13. Canonical policy: [F2A3_EVIDENCE_AND_RETENTION_POLICY_20261113.md](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-f2a1-domain-foundation-20261111/docs/F2A3_EVIDENCE_AND_RETENTION_POLICY_20261113.md).

**Owner approval: PASS. Legal/privacy clearance: PENDING/BLOCKED — [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24).** The lawful basis is not selected by the policy and must be documented by the responsible controller with qualified privacy/legal review.

No real evidence intake, production upload/storage, live claim decision or Owner grant may be activated before legal/privacy clearance. Only isolated non-production implementation using synthetic data, incapable of accepting real evidence, may proceed under [Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25); [Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains the legal/privacy release gate. Existing reviewer-auth QA remains bounded to isolated SQLite bearer HTTP; no release or production approval is implied.


## 2026-11-13 — F2a-3 synthetic evidence policy code candidate

[Issue #25](https://github.com/jvalkovv/YangSpaceApp/issues/25) is now implemented as [Draft PR #26](https://github.com/jvalkovv/YangSpaceApp/pull/26), exact candidate `e70558086e43afff5bd27db428a3f363d7e7498f`.

Source-only scope: pure evidence file validation, deterministic retention calculation, UTC timestamp validation, fail-closed malware scanner configuration guard and synthetic-only runtime tests. No HTTP route, evidence schema/migration, production storage or real evidence intake.

**State: NOT WINDOWS-TESTED / DRAFT / HOLD.** Exact-SHA restore, Release build, runtime execution and independent evidence postflight are still required. [Legal/privacy Issue #24](https://github.com/jvalkovv/YangSpaceApp/issues/24) remains the hard gate before any real evidence collection or production activation.
