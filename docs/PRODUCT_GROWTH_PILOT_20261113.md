# YangSpace — Approved Strategy + Growth Backlog Review (2026-11-14)

**Status:** APPROVED BASE DIRECTION / ADDITIONAL PRODUCT IDEAS REVIEWED, PRIORITIZED, NOT RELEASE-AUTHORIZED.
**Branch integration:** documentation-only Draft PR based on the active YangSpace V2 branch, not the legacy main branch. The older PR #30 remains a historical proposal; this document reconciles its scope.
**Authority:** SSOT is authoritative for engineering status and release gates; this document is product strategy and pilot experiment contract. Neither is evidence of implemented features.

## Product positioning and competitive test

YangSpace is a hyperlocal **customer-acquisition and verified discovery channel**, starting from one neighborhood of Varna. Residents browse freely; merchants publish current, accountable local information and receive measurable, privacy-safe actions.

A phone-number directory alone has weak willingness-to-pay because free alternatives already offer local listings, photos, hours and promotions (e.g. Google Business Profile). Differentiation must be (a) locally current offers/brochures/jobs, (b) a trustworthy verification and moderation signal, (c) a low-friction merchant publishing workflow, and (d) demonstrated useful customer actions.

External visual references are **inspiration, not assets or feature acceptance**: FoodHunt/Dribbble for mobile category/search cards; VibrantAd for NAP and local SEO hygiene; Adriano/Behance for flyer/document presentation; Beyti for local-first positioning only. Beyti's checkout/delivery marketplace model is explicitly excluded. Do not reproduce third-party copyrighted UI/images.

## Six base decisions approved by owner on 2026-11-14

1. One .NET 10 modular monolith, one Angular web client, PostgreSQL as the production target. SQLite remains limited to reviewed preview/disposable testing. A reviewed PostgreSQL operations and migrations ADR still needs an implementation acceptance gate.
2. Identity, reviewer/ownership and real Business CMS are P0 before growth expansion. Public account registration never auto-grants Owner/Editor or staff roles.
3. Correct geo data, truthful business discovery and external navigation are P0; interactive map and proximity ranking are P1 until verified coordinates and accessibility/performance have passed.
4. Varna pilot in two stages: Phase A **20-30 verified local businesses**; Phase B **100 verified businesses** if Phase A meets explicit quality/engagement/ops gates.
5. No automatic billing, checkout or subscriptions before evidence of merchant willingness-to-pay and sustainable unit economics. Proposed EUR 1-5/month is a pricing hypothesis only.
6. One SSOT, existing engineering gates (G0-G4, G-PREVIEW and G-PILOT), scoped PRs closed only at 100% of their OWN DoD. Local exact-SHA QA, command logs, manual PVE/CT164 release, no GitHub Actions.

## Feature priority — reviewed expansion, not automatic approval for implementation

| Audience | Feature | Priority | Implementation slice / prerequisite |
|---|---|---|---|
| Resident | Business/service discovery, category, neighborhood, search | P0 | Verified location records, canonical page, public API filters |
| Resident | Active offers, brochures and external job links | P0 | Publisher/reviewer lifecycle, expiry, safe files |
| Resident | Contact actions: call, website, directions, offer/job link | P0 | Clearly labeled external action; aggregate counts without claiming actual visits or purchases |
| Merchant | Verified profile, owner claim, scoped CMS | P0 | Identity, reviewer-only decision, tenant isolation, audit |
| Merchant | Profiles, offers, brochures, jobs and freshness | P0 | Moderation, lifecycle, version attribution, expiry |
| Resident | Interactive map, accessible map/list toggle | P1 | Accurate coordinates, no precise-GPS requirement for baseline browsing |
| Merchant | QR code linking to canonical business page | P1 / early pilot experiment | No account/token in QR; print/export; counts distinguish scan from real customer |
| Resident | Favorite businesses | P1 | Initially optional local-browser favorites without account; synced favorites only after privacy design |
| Resident/merchant | Requests for quotations / inquiries | P1 controlled experiment | Structured, category-limited, anti-spam, privacy notice, retention, merchant response SLA; NO paid lead billing |
| Merchant | Aggregated useful-action reporting | P1 | Action taxonomy, bot/exploit suppression, data-minimization; separate action counts from offline conversion |
| Resident | Neighborhood events | P2 | Organizer authorization, review, date/expiry, no tickets/checkout |
| Resident | Opt-in favorite/offer notifications | P2 | Consent/preference center, unsub, dedup, frequency cap, quiet hours, deliverability |
| Merchant | Promoted posts | P2 | Clearly labeled sponsored placement, transparency, non-deceptive organic ranking, moderation and reporting |
| Merchant | Area/category campaigns | P2 | Time/location/category rules, inventory caps, non-sensitive targeting, contracts and outcome reports |
| Merchant | Non-transactional basic plan / subscription proposal | P2 hypothesis | Pilot economics + explicit legal and billing ADR; no payment integration in MVP |

Outside MVP: orders, delivery, booking, checkout, online payment, native marketplace, CV storage, ATS, POS, accounting, warehouse, loyalty, native app, AI assistant, nationwide imports. Products in other repositories remain separate.

## Strict identity versus merchant authorization

Anonymous discovery requires no login. Verified account identity and email are **not** proof of business ownership. Identity registration -> confirmed account -> business claim -> reviewer decision -> atomic scoped OrganizationMember grant -> business CMS rights. Reviewer and Owner authority can be revoked; server checks current persisted rights per request; no self-approval.

Issue #24 requires written legal/privacy clearance before ANY real business evidence collection, storage, or live claim decision. Synthetic-only QA is permitted behind default-off nonproduction gates.

## Three conditional revenue hypotheses

| Stream | Offer | Measurement | Gate |
|---|---|---|---|
| Merchant plan: initially EUR 1-5/month hypothesis | Additional publishing and verified action reporting, NOT pay-to-exist in a bare directory | Merchant payment intent, active users, verified leads/actions, support+moderation cost per business, churn | Do not price, charge or auto-invoice until Phase B evidence and business/financial review |
| Clearly labeled promoted publications | Limited time-bounded premium visibility | Eligible inventory, clicks, qualified inquiries, complaint/refund rates; sponsored/organic separation | Legal/DSA applicability review, labeling, seller identity, rate/inventory controls |
| Category/neighborhood campaigns | Geographically contextual placements and transparent reports | Campaign-period aggregate actions, reach only if measured lawfully, transparent reporting | Same legal/privacy gates, no behavioral or sensitive targeting, manual commercial contracting before any automated billing |

**Critical honesty rule:** CTA click, QR scan, page view or navigation open is not a verified in-store visit, booking, sale or unique person. Qualified inquiry also is not a sale. Claims of attributed real-world outcomes require merchant confirmation and an approved methodology.

## Privacy, content and reliability contracts

- Analytics data model: business/location, bounded action type, UTC time bucket and optional campaign identifier; report aggregates; do not persist visitor dossiers, precise GPS traces or persistent visitor fingerprints for this feature. Abuse/security logs have a different purpose and governed retention. Instrument only after lawful basis/cookie-consent assessment.
- Inquiries: opt-in form, minimal contact data, explicit recipient and purpose, spam controls, moderation/complaints, secure storage and access, approved retention and deletion. No automatic sharing with multiple businesses.
- Notifications: explicit marketing subscription where required, independent from service-account email, clear unsubscribe, sender deliverability and outbox/failure handling.
- Sponsored content: visible paid label, sponsor identity and parameters as legally applicable; no deceptive mixing, do not silently rank organic by payment, and assess DSA applicability before launch.
- Each public business/profile needs verification provenance, last-confirmed timestamp and correction/report flow. Distinguish "edited" from "verified".
- Production: PostgreSQL TLS VerifyFull, non-privileged DB user, offline reviewed migration, protected media, moderation queue, transactional outbox, health/metrics/errors, off-host restore and manual rollback. CT164 only permanent app host; no CI/deploy via GitHub Actions.
- Browser session design is an open ADR: current F2b memory-only bearer has refresh UX limits; compare with secure HttpOnly cookie/BFF and CSRF requirements before implementation. No session change is authorized by these docs.

## Pilot experiment and GO / NO-GO

**Phase A (20-30):** one bounded Varna neighborhood; verified NAP/coordinates/contact, at least one complete public owner/admin flow, verified active publication examples, resident useful actions. Measure moderation time, publishing time, verification cost, anonymous mobile discoverability, privacy-safe action reliability and corrections.

**Phase B (100 if Phase A passes):** ≥100 verified businesses, ≥30 owner interviews, ≥30 actively maintained offer/brochure/job records, ≥10 concrete merchant payment-intent signals. These are pilot targets, not current counts, universal success guarantees or already tested results.

GO only after: zero security bypasses in negative QA, zero expired/unpublished items in public API, browser accessibility/performance and SEO gate PASS, tested backup/restore and published RTO/RPO, actionable engaged local demand and repeatable onboarding. Otherwise improve coverage/value proposition, not nationwide expansion.

Proposed web-vitals targets for review: p75 LCP <=2.5s, CLS <=0.1, INP <=200ms; target acceptance must include real field data where sample permits. Operational SLOs, RTO/RPO and onboarding costs require measurement and explicit approval before marking GO.

## One executable sequence; no new engineering-gate numbering

M0 ADR/SSOT -> M1 Identity/PG integration (#27 -> #17 -> #29) -> M2 Reviewer claim and atomic Owner service (Issue #20, privacy #24) -> M3 verified merchant CMS/publication workflow (Issues #6/#7/#11) -> M4 geo/list/mobile discovery, indexable SEO and public action events -> M5 CT164 private release, restore and security -> M6 Varna pilot A then B -> M7 measured growth experiments.

Existing acceptance **G0/G1/G2/G3/G4/G-PREVIEW/G-PILOT** remain the only engineering/release gate names. Do not introduce alternate G1/G2/G3 meaning.

## References for legal/product review

- Google Business Profile: https://business.google.com/en-all/business-profile/?price=FREE
- FoodHunt visual inspiration: https://dribbble.com/shots/25612595-FoodHunt-Restaurant-Cafe-UI-App
- Beyti reference, marketplace excluded: https://www.beyti.app/
- VibrantAd local SEO: https://vibrantad.com/blog/local-seo-checklist-petaling-jaya-klang-valley
- Adriano flyer visual reference: https://www.behance.net/gallery/161177669/adriano-restaurant-menu-and-flyer
- EU DSA Regulation 2022/2065: https://eur-lex.europa.eu/eli/reg/2022/2065
- EU privacy/cookie guidance: https://europa.eu/youreurope/business/growing/digitalising/online-privacy/index_en.htm

**Scope boundary:** this is a product direction and implementation proposal integrated into existing V2 docs, NOT app code, SMTP, database, CT164 deployment, legal advice or product-release approval.
