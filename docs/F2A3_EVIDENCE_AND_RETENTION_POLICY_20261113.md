# F2a-3 — Evidence, Privacy and Retention Policy v1

**Date:** 2026-11-13  
**Status:** OWNER-APPROVED PRODUCT / TECHNICAL POLICY (2026-11-13) — LEGAL / PRIVACY CLEARANCE REQUIRED BEFORE REAL EVIDENCE COLLECTION OR PROCESSING  
**Parent work item:** [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20)  
**Related technical contract:** [F2a-3 Claim Review Security Contract](F2A3_CLAIM_REVIEW_SECURITY_CONTRACT_20261112.md)

> This policy was explicitly approved by the product owner on 2026-11-13 as the operational product/technical baseline. It is not legal advice and does not establish a GDPR lawful basis. Real evidence collection or processing, production upload/storage and live claim-decision activation remain blocked until the separate legal/privacy review is completed and the responsible controller confirms the processing can proceed.

## 1. Purpose and hard boundary

Evidence is collected only to establish:

1. that the claimed business exists and its identity matches the directory record;
2. that the applicant is authorised to represent or manage that business;
3. that the business is genuinely associated with the specific physical location being claimed, where a public premises address is represented;
4. that a licence or authorisation is current where the activity is legally regulated.

Do not collect evidence merely to build a richer business profile, marketing database, identity archive or general fraud dataset.

**Current implementation remains unchanged:** PR #23 proves only reviewer authorization/conflict checks through isolated SQLite Identity bearer HTTP. No upload endpoint, private evidence store, claim decision route or evidence/audit mutation is authorized by this draft.

## 2. Evidence categories and acceptable proof

Use public authoritative sources first. Store the source, lookup time and relevant result; do not download a registry extract if a verifiable public record is sufficient.

| Category | Primary acceptable proof | Fallback / supplementary proof | Acceptance rule |
| --- | --- | --- | --- |
| Business identity | Current official company/business register entry or official public registry result | Official tax/business registration or current sector registration where no general register exists | Legal/trading name and registration identifier must match or be explainably linked to the claimed business |
| Applicant authority | Official register showing the applicant as a current representative/director/authorised person | A signed authorisation from a registered representative, limited to this claim; verify the signatory against the official record | Applicant authority must be established independently of control of the email/phone |
| Physical location association | Current lease/occupancy agreement, property title (relevant page only), official premises permit/licence | Recent utility/service bill, official authority correspondence or redacted business invoice showing the entity and claimed address | One authoritative document; where unavailable, two independent secondary sources and manual escalation. A tenant may claim a location; ownership of the property is not required |
| Current operation at address | Current official premises/sector permit or registry record tied to the address | Recent redacted supplier invoice, delivery document or other business record showing the business and address | Supplementary where identity and occupancy documents do not independently establish current operation |
| Regulated activity | Current licence/permit verified in the competent authority's register | Current licence copy with issuer, scope and expiry visible | Mandatory only for categories legally requiring that licence; verify scope and validity, not merely possession of a document |
| Contact / online presence | Domain control, business email, phone OTP or website control | Published business contact details and human callback | Supplementary only. Email/phone/domain control alone never proves legal identity or physical premises |

### 2.1 Business types without public walk-in premises

- A service-area business must be classified as such and must not be presented as a verified walk-in address.
- An online-only business does not need a lease or utility bill for a physical premises it does not claim.
- For sole traders or other natural-person businesses, use the applicable official registration or business permit and collect only the minimum representative information needed.
- If the product cannot represent a service-area or online-only business accurately, keep the claim pending rather than accepting misleading address evidence.

### 2.2 Reviewer checklist

A reviewer may approve only when every applicable criterion is satisfied:

- [ ] Business identity matches the claimed listing.
- [ ] Applicant authority is independently established.
- [ ] Physical address association is established, or the listing is explicitly classified as service-area/online-only.
- [ ] Required regulated-activity licence is current and covers the claimed activity.
- [ ] No unresolved duplicate, ownership, self-review or active-membership conflict exists.
- [ ] Evidence is readable, current enough for its purpose and not materially altered.
- [ ] Unnecessary personal/financial information has been redacted or the item has been returned for a redacted replacement.
- [ ] Reviewer records a bounded outcome/reason code and source reference, not a copy of the document contents.

One strong source per applicable criterion is preferred. Two independent secondary sources are a fallback, not a way to bypass an unavailable mandatory licence. Screenshots of social profiles, generic email addresses, phone OTP, storefront photographs or a website alone are never sufficient proof of legal identity or authority.

## 3. Data minimisation — prohibited or restricted material

**Do not request or retain by default:**

- national identity card/passport copies, personal identification numbers or full identity-document numbers;
- bank statements, payment-card data, account credentials or bank login screenshots;
- tax returns, payroll, employee lists, customer lists or full accounting ledgers;
- health, biometric, genetic, political, religious, union or other special-category personal data;
- criminal-record information;
- unrelated residential addresses, family information or personal contact details;
- full contracts or invoices when only a page/field proving the business-address relationship is necessary.

Ask the applicant to redact unrelated names, amounts, account numbers, signatures and identifiers. A document containing unnecessary personal data must be returned for a redacted copy or rejected; reviewers must not create informal local copies to work around the rule.

A government ID check is **not part of the default workflow**. If a future fraud case genuinely requires one, it needs a separately documented necessity assessment, lawful basis, narrower procedure and explicit approval before collection.

## 4. Storage and access controls — required before any upload feature

1. Store raw evidence in a dedicated **private evidence vault**, outside the public web root and separate from public business media. A private encrypted volume or private object store is acceptable; no public bucket or permanent public URL.
2. Encrypt transport with TLS and encrypt stored objects and backups. Key access must be restricted separately from ordinary application/database access.
3. The API must stream evidence only after claim-scoped authorization. Prefer an authenticated backend download; if a temporary URL is unavoidable, it must be single-purpose, short-lived (maximum 60 seconds) and never logged.
4. Only the applicant for that claim and the specifically authorised reviewer assigned to it may access the raw evidence. A reviewer must not access a claim where the reviewer is the applicant or an active member of the target organisation.
5. PlatformAdmin and support access do not automatically grant evidence access. Emergency break-glass access must require a separate permission, reason, time limit and immutable audit event.
6. Log upload, view, download, replacement, decision and deletion metadata: actor, claim/evidence ID, action, timestamp, result and correlation ID. Never log file bytes, document text, secrets or full contents.
7. Generate storage object keys server-side. Do not use applicant-supplied paths or original filenames as storage paths.
8. Initial file allowlist proposal: PDF, JPEG and PNG only; maximum 10 MB per file, five files per claim and 25 MB total. Reject archives, executables, macro-enabled documents, password-protected files and mismatched MIME/file signatures. Scan for malware before reviewer access.
9. No third-party OCR, AI document analysis, external verification SaaS or onward sharing in the first slice. Any such processor requires a separate vendor, data-processing, transfer and privacy review.
10. Prefer EU/EEA storage. The actual hosting provider, region, subprocessors, backup location and any international transfer must be recorded and reviewed before production evidence collection.

## 5. Proposed retention schedule

These are **operational defaults for owner/legal review**, not a statement that the periods are legally sufficient. The controller must confirm the purpose, lawful basis and period before collection.

| Data class / claim state | Proposed retention | Deletion behaviour |
| --- | --- | --- |
| Pending claim raw files | 90 days after the applicant's last meaningful activity | Expire an inactive claim; delete raw files within 30 days after expiry |
| Rejected claim raw files (and a withdrawn state only if separately approved) | 180 days after the final decision/closure | Delete raw files within 30 days after the period ends; this is an operational dispute window, not a promise of a formal appeal right |
| Approved claim raw files | 12 months from approval or the latest re-verification, whichever is later | Re-verify before expiry. If evidence is not refreshed, mark the verification as stale; do not silently change ownership or membership. Delete the old raw files within 30 days after replacement/expiry |
| Superseded evidence | 30 days after the replacement is accepted | Delete the superseded raw object; preserve only the minimum verification/audit metadata |
| Evidence access/security logs | 12 months from the event | Then delete or irreversibly aggregate, subject to a documented legal hold |
| Claim decision and verification metadata (no raw documents) | While the claim is active, then 24 months after claim closure | Then delete or irreversibly anonymise, unless a specific legal obligation or legal hold applies |
| Encrypted backups containing deleted objects | Maximum 35 days after deletion from primary storage | Restore procedures must replay deletion tombstones before restored data is made available |

A legal hold must be tied to a specific dispute, request or legal obligation, have an owner and reason, be access-restricted, reviewed at least every 90 days and released promptly when no longer necessary. It must not become a blanket indefinite retention mode.

The deletion job must be idempotent and observable. It must record object ID, claim ID, deletion reason, scheduled/deleted timestamps and result without copying the evidence itself into the audit log. Backup expiry and restore-time tombstone replay are part of deletion acceptance, not optional housekeeping.

## 6. Metadata retained instead of document copies

The application may retain only the minimum evidence-verification metadata needed to explain a decision:

- evidence ID and claim ID;
- category and issuing authority/source type;
- document issue/expiry date where relevant;
- received, reviewed and deletion timestamps;
- reviewer ID and outcome/reason code;
- verification source URL/reference where public and stable;
- content hash for integrity/duplicate detection, subject to legal review;
- retention deadline, deletion status and legal-hold reference.

Do not retain extracted document text, OCR output, a duplicate document in the database, or unrestricted free-text reviewer notes. Any reviewer note must be bounded, structured and free of unnecessary personal data.

## 7. Lawful basis, transparency and legal review — NOT DECIDED

No GDPR lawful basis is selected by this draft. Legal counsel / the responsible privacy owner must document:

- controller and processor roles for YangSpace and each hosting/storage/scan provider;
- the purpose and lawful basis for each data class (possible Article 6(1)(b) pre-contractual steps and/or Article 6(1)(f) legitimate interests require an actual necessity/balancing assessment; Article 6(1)(c) applies only where a specific legal obligation exists);
- why each evidence category and retention period is necessary and proportionate;
- privacy-notice wording before upload, including purpose, categories, recipients/processors, storage period/criteria, rights and complaint route;
- data-subject access, correction, restriction, objection and erasure request handling, including the limits of erasure where a documented legal obligation or legal-claims exception applies;
- processor agreements, subprocessor list, storage region and international-transfer safeguards;
- whether a DPIA is required after a documented risk screen.

Do not treat consent as a default substitute for identifying the correct lawful basis. The GDPR principles of purpose limitation, data minimisation, storage limitation, privacy by default, security and accountability must shape the implementation (Articles 5, 6, 13, 17, 25, 30, 32 and 35 as applicable): [official EUR-Lex GDPR text](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng/).

## 8. Owner approval record and remaining legal/privacy decision

| Decision | Recommended default | Approval state |
| --- | --- | --- |
| Evidence categories | Sections 2 and 2.1 | **OWNER APPROVED — 2026-11-13** |
| Minimum proof threshold | One authoritative source per applicable criterion; otherwise two independent secondary sources + manual escalation | **OWNER APPROVED — 2026-11-13** |
| Prohibited data / redaction | Section 3; no ID copy or financial records by default | **OWNER APPROVED — 2026-11-13** |
| Private storage and claim-scoped access | Section 4 | **OWNER APPROVED — 2026-11-13** |
| File allowlist and limits | PDF/JPEG/PNG; 10 MB each; 5 files; 25 MB total | **OWNER APPROVED — 2026-11-13** |
| Pending retention | 90 days inactive + deletion within 30 days | **OWNER APPROVED — 2026-11-13** |
| Rejected retention | 180 days + deletion within 30 days | **OWNER APPROVED — 2026-11-13** |
| Approved evidence retention | 12 months from approval/latest re-verification; stale verification if not refreshed | **OWNER APPROVED — 2026-11-13** |
| Metadata / access log retention | 24 months after claim closure / 12 months for access logs | **OWNER APPROVED — 2026-11-13** |
| Backup deletion horizon | 35 days maximum; restore-time tombstone replay | **OWNER APPROVED — 2026-11-13** |
| Lawful basis, notice, processor and transfer review | Written review by responsible privacy/legal adviser | **LEGAL REVIEW REQUIRED** |

## 9. Implementation gate / Definition of Done

The owner has approved the operational defaults on 2026-11-13. No real evidence collection, production upload/storage or live claim-decision activation starts until the legal/privacy review confirms the processing can proceed and the responsible controller documents the lawful basis and required notices/processor controls. Implementation work before that clearance must remain isolated, non-production and incapable of collecting real personal data.

After approval, a separate child PR must implement and test:

- private storage adapter with no public URL/path traversal;
- claim-scoped applicant/reviewer authorization and same-token reviewer revocation;
- MIME/signature validation, size limits, malware scan and safe failure;
- encrypted storage/backup and key separation;
- immutable access/decision/deletion audit metadata with no evidence content;
- retention scheduler, idempotent deletion, legal-hold exclusion and backup tombstone replay;
- data-subject request workflow and privacy notice;
- real PostgreSQL bearer HTTP, parallel claim writers, transaction fault injection and uncertain-COMMIT reconciliation for the decision workflow;
- exact-SHA Windows parser/restore/Release build, isolated runtime evidence, cleanup and independent manifest/hash postflight.

**Current release gate: HOLD.** Product/technical policy approval is recorded; legal/privacy clearance is outstanding. No real evidence collection, production upload/storage, live Approve/Reject activation, Owner grant, production flag, CT164/CT152 access, live DB operation or deployment is authorized.
