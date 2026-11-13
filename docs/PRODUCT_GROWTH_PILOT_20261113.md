# YangSpace — product growth and local pilot (decision 2026-11-13)

Status: **APPROVED STRATEGIC DIRECTION / DOCS ONLY**; not a deployment or acceptance decision.  
Owner: VNTech. Repo: YangSpaceApp. Primary market: one pilot neighborhood in Varna.

## Positioning

A trustworthy neighborhood discovery platform: real local businesses, working hours, verified addresses, fresh brochures/offers and local jobs. The product is **not a generic static business directory**. Differentiate on data freshness, transparent moderation, real resident actions and low-friction merchant publishing.

No merge, public merchant activation, production write or commercial rollout is authorized by this document. Existing M0–M5 security, ownership and runtime gates take precedence.

## Product slices

| Priority | Feature | Acceptance |
|---|---|---|
| P0 | Verified business/place registry | Source, verified timestamp, ownership/claim state, moderation audit, expiration |
| P0 | Search, category, locality and mobile map | Accurate geolocations, fast mobile, canonical/indexable public pages |
| P0 | Merchant ownership + safe self-service | Reviewer-only approval, cross-tenant denial, revocation, audit atomicity, no staff privilege leakage |
| P0 | Fresh offers/brochures/jobs | Expiry, review trail, correction workflow, invalid content not displayed |
| P1 | QR/deep links for each business | Canonical URL, printable code, measurable (privacy-aware) visits |
| P1 | Action analytics for owners | Aggregated call/directions/website/offer actions; no fake impressions or private visitor dossiers |
| P1 | Pilot merchant onboarding | Claim, approve, publish, update and support from one operations flow |
| P2 | Local events and opt-in notifications | Consent, deduplication, expiry, abuse control, unsubscribe and privacy review |
| P2 | Promoted campaigns | Clearly labeled sponsored placements; organic search intact; transparent contracts and moderation |
| P2 | Local contact/inquiry opportunities | Abuse-proof contact handoff, opt-in lead collection and data-retention rules |

**Deliberately not MVP:** subscriptions/billing, booking, checkout, public merchant self-registration until claim controls pass, in-platform job applications/CV storage, native app, loyalty, AI assistant, general POS integration, national expansion.

## Local commercial test (M5)

1. Inventory >=100 verified business locations in one *bounded* neighborhood; capture verification source and last-confirmed date.
2. Interview >=30 merchant owners. Demonstrate the actual publish/update experience; record the problem being solved, existing alternatives and time required to maintain their listings.
3. Aim for >=30 active listings with current offer/brochure/job; measure resident **useful actions** (calls, navigation, offers, job links) and repeat visitor usage.
4. Seek >=10 explicit payment-intent signals against a concrete package; real paid trials are stronger evidence than verbal interest.
5. Compare maintenance minutes/business/month + moderation and hosting cost against willingness-to-pay. The 1–5 EUR per business concept is only an **unvalidated hypothesis**, not an approved price.
6. GO for another neighborhood only with maintained freshness, verified real actions, repeatable merchant onboarding, manageable support and demonstrated monetization signal. Otherwise iterate proposition, not nationwide import.

## Technical/reliability gates

- Preserve one product backend and one public CMS owner. Locality is a data model/filter, not a per-neighborhood deployment.
- Current tenant/claim reviewer authorization work remains Draft/HOLD until exact-head QA, PostgreSQL schema/transaction evidence, real bearer ownership denial, concurrency and durable audit tests pass.
- Strict merchant vs moderator permissions, temporary upload isolation, file validation and image safety; no real secrets or business evidence stored in public Git.
- API/database health, worker queues, request latency, image storage, moderation backlog, freshness, click-through instrumentation. Do not record unnecessary personal data.
- GDPR/data correction, promotion claims/price transparency, content licensing and reporting/notice handling require explicit legal review.
- Per-stage evidence: exact SHA, tests, authorization matrix, migrations/rollback, backup and restored data proof, mobile/SEO QA and monitored pilot.

## Excluded cross-product scope

YangSpace does **not** contain VNTech Accounting, Warehouse or Trade/POS/fiscal device functionality. Those are separate planned products/repositories. Finance Hub retains its own product purpose. No Booking CRM, VCore or OfficeBite code is pulled into YangSpace.

## Next 3 gates

- **G1:** close current identity/ownership/claim security acceptance without enabling public merchant changes.
- **G2:** finish verified location, brochure/offer and jobs CMS plus mobile discovery and end-to-end isolated deployed QA.
- **G3:** pilot one Varna neighborhood and make monetization decision from operating data. Later campaigns/events/notifications need separate ADR, security and user evidence.
