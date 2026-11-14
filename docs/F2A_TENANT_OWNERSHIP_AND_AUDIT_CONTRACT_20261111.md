# YangSpace F2a — ownership, claims and audit security contract (proposal)

**Date:** 2026-11-11 (Europe/Sofia). **Status:** PROPOSED / DRAFT — architecture review, not an accepted ADR, schema migration, implementation or deployment.
**Primary implementation issue:** [#4 — tenant ownership](https://github.com/jvalkovv/YangSpaceApp/issues/4). **Product and audit acceptance:** [#11 — business freshness](https://github.com/jvalkovv/YangSpaceApp/issues/11).
**Baseline:** V2 `a720d96a0f665b691c6eb4f1213d42f14768a3b5` (code at F1b squash `403b281abad2fb0c8048c2763a606c3435f3e5dd`).

## 1. Verified source reality and safety boundary

- `DirectoryDb` currently stores Identity users, locations, offers, brochures and jobs. No Organization, OrganizationMember, BusinessClaim, audit revision, tenant row-level scope or authenticated business owner endpoints exist.
- `/api/v1/admin` requires global `PlatformAdmin` bearer authorization. Production additionally requires a protected internal gateway. These controls are **not** proof that ordinary merchants can access only their own business.
- Public readers are anonymous and must remain read-only. `UpdatedAtUtc` on publications (F1b) has no actor and is not ownership/verification evidence.
- The committed EF initial + F1b SQLite migrations and disposable recovery harness are accepted for the **development scope** only. CT164 public preview remains the older `EnsureCreated` demo database; do not migrate it in place.
- **Production provider direction selected:** [ADR-019](ADR-019-POSTGRESQL-PRODUCTION-TARGET.md) accepts PostgreSQL as the target for future verified multi-tenant data. Current SQLite stays as read-only fictional preview/disposable QA; no live conversion, dual write, PG instance provisioning, PG migrations or owner access yet. Before F2a schema merge, prove provider-specific PostgreSQL migration and isolated recovery gates; claim-review and retention details remain proposed.

## 2. Proposed domain and lifecycle (not yet implemented)

| Record | Minimum safe fields | Invariants |
| --- | --- | --- |
| `Organization` | `Id`, `Name`, `Status`, `CreatedAtUtc`, `Revision` | One accountable organization per claimed location; status changes audited |
| `OrganizationMember` | `Id`, `OrganizationId`, `UserId`, `Role`, `GrantedByUserId`, `GrantedAtUtc`, `RevokedByUserId?`, `RevokedAtUtc?`, `Revision` | Explicit reviewed grant; no invitation/self-registration implies membership; unique current membership per org/user; revoke denies instantly on next request |
| `BusinessClaim` | `Id`, `LocationId`, `ApplicantUserId`, `Status`, `SubmittedAtUtc`, `ReviewedByUserId?`, `ReviewedAtUtc?`, `DecisionReason?`, `Revision` | Applicant cannot approve own claim; reviewer role+reason required for approve/reject; evidence private; duplicate active claims prevented |
| `BusinessLocation` | existing fields + `OrganizationId?`, `Revision` | Legacy/unclaimed records keep NULL ownership, only platform staff can modify; owner scope by server-side joined membership |
| `ContentAuditEvent` | `Id`, `OrganizationId?`, `LocationId`, `EntityType`, `EntityId`, `Action`, `ActorUserId`, `ActorRole`, `OccurredAtUtc`, `RevisionBefore`, `RevisionAfter`, `CorrelationId`, allowlisted changed fields, optional approved reason | Append-only; actor from authenticated bearer (not body/header), business change and event in **one DB transaction**; no public raw event output |

**No proof by association:** `IsVerified` on `BusinessLocation` is not a substitute for an approved claim, live membership or a freshness confirmation. Historical unknown owners/actors remain **unknown**, not backfilled with a fabricated platform identity.


### F2a-1 database uniqueness policy — 2026-11-11

The initial generated PostgreSQL SQL from source `ccd2acb965e98498dd415a5a080a441b88879a3a` was independently reviewed and **HELD**: `BusinessClaims` lacked active-claim uniqueness, and `BusinessLocation` lacked `Revision`. The original ZIP (SHA-256 `96544589a2a27560a64df8e9da09235d9f81c6716ae151dc92bbc4d915e252c5`) is **review evidence only**, not commit-ready.

Proposed F2a-1 source policy, prior to new exact-SHA QA:

- **Approved:** at most **one** `Approved` claim for each `LocationId` across all organizations. Unique filtered index on `LocationId` with persisted string predicate `"Status" = 'Approved'`.
- **Pending:** at most **one** `Pending` claim for a given `(LocationId, OrganizationId)`, regardless of applicant. A different organization may have its own competing pending application for the same location, subject to reviewer evaluation. Filtered unique index with `"Status" = 'Pending'`.
- **Resolved:** `Rejected` and `Withdrawn` claims remain historical records; the same organization may submit a new `Pending` application after resolution. Approving a new organization for a previously approved location will require an authorized **atomic** ownership-transfer/reviewer action that resolves the old approved claim first.
- **Location revision:** `BusinessLocation.Revision` is a nonnullable application-managed `long` optimistic concurrency token, initial value 0. Explicit increment+CAS on successful mutations is the F2b API responsibility; no HTTP 409 or audit atomicity is asserted by the schema alone.
- These two filtered indexes use provider-neutral EF predicates valid for SQLite and PostgreSQL. Provider-specific SQL must be checked again from EF-generated artifacts and verified with real PostgreSQL negative tests before acceptance. **No generated C#/Designer/snapshot may be edited by hand.**

## 3. Authorization model — deny by default

1. Authenticate using **Identity bearer**; resolve subject ID from the authenticated principal, never from a supplied `ActorUserId`, organization ID or custom client header.
2. Query live, non-revoked membership for **each requested organization/location**. It must be checked server-side for both list/read and write. Do not accept a global boolean `IsOwner` or only trust JWT role claims as membership state.
3. Business owner/editor: only own organization locations and their own offers, brochures and jobs. Record lookup must jointly bind `OrganizationId`, `LocationId` and child entity ID to prevent IDOR, including nested content.
4. Unclaimed/legacy locations: staff-controlled only; cannot be claimed into owner-edit rights until staff verification and audited approval. `PlatformAdmin` is a separate privileged staff workflow, never an implicit merchant membership.
5. Claim approval/revocation is an authenticated platform reviewer operation with explicit reason, actor, timestamp and durable audit; apply approval + location ownership + membership grant atomically. No unaudited privilege escalation.
6. Return 401 unauthenticated; return generic 404 for foreign scoped resources to avoid cross-tenant existence disclosure; 403 for authenticated but policy-prohibited global operations. Do not return private actor/email/claims in public API.
7. Private CMS endpoints must remain **disabled** for real merchants until complete integration tests pass. Neither draft docs nor a table migration enables the feature.

## 4. F2b audit/concurrency contract

- Immutable records for create/update/publish/hide/restore and reviewer decisions (claim approve/reject/member revoke). Expiry-by-clock is visibility logic; do **not** invent an actor audit event each time a GET detects expiry.
- Explicit field allowlist **per entity**. Avoid credentials, tokens, raw request JSON, unredacted email/phone, claim evidence, IP, free-form description content, URL query secrets and arbitrary user metadata. For sensitive changed fields store `changed=true` / redaction, not raw previous values.
- Each successful state transition writes **exactly one** audit record in the same transaction as the modified resource. A failed audit insertion must roll back the resource write; idempotent no-ops create no event.
- Per-resource integer `Revision` increases by one only on state transition, with atomic compare-and-swap update (`WHERE Id = ? AND Revision = expected`) or provider-equivalent concurrency token. **Stale revision → HTTP 409**, no data change and no audit; validate both content and parent scope inside the same transaction.
- API contract for revisions must be explicit: strong `ETag`/`If-Match` or a required `expectedRevision` (never silently overwrite). Failure response must not reveal foreign tenant state.
- `CorrelationId` must be generated/validated server-side and bounded. `OccurredAtUtc` is server-side UTC, not supplied by browser. Audit history is paginated, owner scoped, permission-checked and separate from public safe summaries.
- Retention/deletion/redaction for audit and identity must be approved under privacy policy before onboarding real users; audit append-only **at API layer** is not a claim of tamper-proof storage.

## 5. Mandatory negative integration matrix before enabling owner CMS

| Scenario | Expected effect |
| --- | --- |
| Guest accesses owner list/edit or audit | 401, zero data/audit changes |
| Owner A guesses Organization B, Location B or nested child B IDs | 404, zero cross-tenant disclosure/mutation |
| Unverified applicant attempts own claim approval or changes actor ID | 403/404, no membership elevation or forged actor |
| Nonmember or just-revoked member tries old bearer token | Denied on next request, no row or audit event |
| Staff approves claim without reason/authorized reviewer | Denied, no partial location/member grant |
| Staff approves valid claim | Exactly one location ownership transfer + membership + attribution atomically |
| Audit event insertion fails mid-transaction | Original edit rolled back; no success response |
| Two clients edit revision N simultaneously | Exactly one succeeds as N+1, other 409, one audit event |
| Retry successful no-op hide/restore | Idempotent response; revision/audit unchanged |
| Public catalog/detail/audit query | No private actor IDs, claim evidence, draft content or audit payload |
| Migration from initial+F1b to proposed schema on disposable populated database | Preserves all existing rows, legacy `OrganizationId=NULL`, FK/integrity PASS, backup restore PASS |

## 6. Work breakdown and acceptance sequence

- **F2a-0 (this document):** record threat model, DB/provider dependency, permission contract, audit invariants and test matrix. **Docs-only; no claim of runtime readiness.**
- **F2a-1 (after explicit DB ADR):** generated EF migration for Organization, Member, Claim and nullable legacy location ownership; disposable upgrade/rollback+restore; no new merchant public routes.
- **F2a-2:** reviewer-only claim approval/revocation, membership grant and scoped read authorization, with failure-atomic audit of access grants. Real bearer and IDOR negative tests.
- **F2b-1:** append-only audit events + revision enforcement on every existing write path; transaction failures and stale-write tests, including F1b publication endpoints.
- **F2b-2:** owner/editor private CMS scoped routes and history/read-side; no global admin policy reuse as ownership. Browser and real API negative tests.
- **F3:** owner confirmation, moderation/freshness and UI, only once F2a/F2b authorization is accepted.

## 7. Release & operator gates

- ADR-018: **no GitHub Actions**, manual exact-SHA source/API/integrity tests and retained evidence, no unreviewed release.
- CT164/CT152 unchanged; no active demo DB migration, no unapproved public auth/registration, no `EnsureCreated`→`Migrate` conversion.
- DoD for any F2 implementation PR: reviewed scope and EF-generated migration, exact-SHA .NET/API security/DB upgrade tests, forced audit failure, owner A↔B isolation, point-in-time backup restore and clear admin-vs-owner boundary.
- This contract is **PROPOSED** until product owner confirms the provider and claim-verification policy. No approval is inferred by merging an unrelated PR or passing documentation checks.
