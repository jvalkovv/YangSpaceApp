# F2a-3 — Reviewer-controlled claim decisions and atomic Owner membership

**Date:** 2026-11-12 (Europe/Sofia)  
**Status:** **PROPOSED / SECURITY DESIGN HOLD — requires explicit owner policy acceptance**. No implementation, migration, production flag, merchant onboarding, credentials or live environment changes are authorized by this document.  
**Primary work item:** [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20), derived from [Issue #4](https://github.com/jvalkovv/YangSpaceApp/issues/4); F2b content-audit follow-on is [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11).

## 1. Accepted technical baseline (not release)

- [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) merged read-only Identity bearer TestServer authorization, default-OFF and QA-only with disposable SQLite. This is not a PostgreSQL bearer runtime certification.
- [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) exact code SHA `445db4d9a5d28b798054c426afeba9b9be2c771b`: two Windows combined source/tenant/disposable PostgreSQL regression passes and operator read-only evidence postflight PASS. SQL `2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7`; backup `94c7c2cb1a2fcb93633c23efcb642764baea04f866e464e0728a22e06bdd664e`. Tested schema/constraints/restore only, not claim mutations or release.
- Current EF entities: `Organization`, `OrganizationMember`, `BusinessClaim`, `BusinessLocation`, `ContentAuditEvent`. `BusinessClaim.Status` and `Organization.Status` are string-converted enums; `BusinessClaim.Revision`, `Organization.Revision`, `OrganizationMember.Revision` and `BusinessLocation.Revision` are application-managed concurrency tokens.
- Existing DB constraints: one Approved claim per `LocationId`; one Pending claim per `(LocationId, OrganizationId)`; unique membership `(OrganizationId, UserId)` even if revoked; audit uniqueness `(EntityType, EntityId, RevisionAfter)`. `BusinessLocation.OrganizationId` stays `NULL` until explicitly verified; non-null existing owner is never silently overwritten.

## 2. Proposed decisions — owner approval required before source mutation

| Policy | Proposed choice | Approval gate |
| --- | --- | --- |
| Reviewer authority | Dedicated offline-provisioned `MerchantClaimReviewer` Identity role, distinct from `PlatformAdmin` and Merchant Owner/Editor | Explicit confirm / alter before coding |
| Reviewer revocation | Check **current** Identity reviewer role in storage on every decision, not only stale bearer token claims | Must prove same-token role revocation |
| Conflict of interest | Deny applicant reviewing own claim; deny active member of the target organization reviewing claim; actor sourced only from authenticated bearer | Enforce before DB update, inside transaction again |
| Claim evidence | Reviewed proof of control/representation recorded through an approved private policy; never infer ownership from email, phone, listing text or `IsVerified` | Define accepted evidence categories, reviewer checklist and retention first |
| Scope of first API slice | Reviewer-only Approve/Reject in disposable QA; submission, applicant withdrawal, owner writes, member invitation and ownership transfer all **out of scope** | New fail-closed default-OFF `ClaimReviewQa` gate, never piggyback on `MerchantRead` |
| Claim lifecycle | `Pending → Approved` or `Pending → Rejected` with required `expectedRevision`; Withdrawn remains domain state but no applicant endpoint in this slice | Decided claims immutable; stale writes give generic 409 |
| Location ownership | Approve only if target `BusinessLocation.OrganizationId IS NULL`; existing non-null owners require separate verified transfer workflow | No direct takeover via new claim |
| Member onboarding | Approval creates exactly one Owner membership for the applicant in the approved org, atomically | No implicit grant from submitted claim |
| Re-grant/revoke | Existing unique `(OrganizationId,UserId)` means revoked-member regrant cannot insert another row; choose explicit audited reactivation in later slice | No self-service re-grant; prevent last-owner removal |
| Audit cardinality | One allowlisted immutable event **per changed entity/revision**, all in the same transaction; no raw evidence, secrets or request JSON | Verify relation to `(EntityType, EntityId, RevisionAfter)` UNIQUE index |
| Duplicate/replay | Use stored unique operation/idempotency fence **before enabling retry**; unknown COMMIT outcome cannot safely be interpreted as failure | Must design schema/protocol and prove uncertain commit once-only effects |

## 3. Proposed API and authorization boundary

- Proposed QA route: `POST /api/v1/staff/claims/{claimId:guid}/decision`, server-observed `Approve|Reject`, `expectedRevision` and bounded `reason` (max 500 characters). This is a **design sketch, not a mapped endpoint**.
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

1. Reviewer/evidence/transfer/re-grant policy explicitly accepted; update [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) decision records. This design remains **HOLD** until that approval.
2. Introduce only a separate stacked source PR on the F2a-1 feature branch; no silent modification to existing read-only merchant authorization, public directory or production configuration.
3. EF migration (if needed) generated and reviewed, with isolated PG apply/negative index tests and explicit migration/rollback receipts. Avoid hand-editing generated snapshots.
4. Real bearer HTTP tests in an approved isolated PostgreSQL topology and concurrent clients; all negative assertions verify status **and** zero DB side effects. Forced errors and uncertain COMMIT evidence must pass.
5. Exact-SHA .NET restore/build warnings-as-errors, static review, peer review, signed/hashed local manifests, cleanup and owner approval. Only then merge the scoped mutation PR into its feature parent.
6. **Out of scope:** active CT164/CT152, real user enrollment, production database/ingress, merchant writes, pilot launch and off-host disaster recovery. PR #17 and V2 PR #1 remain Draft/HOLD until their own broader acceptance gates are complete. No GitHub Actions.
