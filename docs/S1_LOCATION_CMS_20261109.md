# YangSpace S1 — Location CMS vertical slice

**Date:** 2026-11-09
**Branch:** `feat/yangspace-v2-foundation-20261108`
**Target:** `yangspace.gndstore.eu` when an isolated YangSpace LXC has been reviewed and provisioned
**Current gate:** SOURCE IMPLEMENTED / RUNTIME UNVERIFIED

## Scope

- **Security:** preview login must first prove PlatformAdmin access with `GET /api/v1/admin/me`; a nonadmin token must not expose the CMS. Tokens remain memory-only.
- **Locations:** create a draft or published location, edit all supported fields, prevent obvious invalid inputs, surface HTTP 409 duplicate slug without discarding unsaved edits, hide, and restore. Admin-only endpoint `POST /api/v1/admin/locations/{id}/restore` is idempotent.
- **UI:** visible editing state, publish/hidden state, responsive form, error/status announcements, explicit cancel/save.
- **Regression source:** mocked Playwright flows for role gate/edit/restore/hide/conflict, separate disposable .NET API integration assertions for restore/403/404/repeat/hidden behavior.
- **Excluded from this slice:** full offer/job/brochure editing/deletion, owner/tenant access, media upload/moderation, production DB migration and public pilot.

## Runtime acceptance — only after target CT approval

1. Verify exact GitHub SHA, read-only PVE inventory, private gateway/TLS/ACL and backup/rollback before any CT writes. No separate QA CT.
2. In the dedicated YangSpace CT, build and test the immutable exact SHA using ephemeral loopback API port and a disposable SQLite test DB. Test identity bootstrap denial, nonadmin 403, location create/edit/hide/restore and persistence after process restart without touching the running service/database.
3. Run browser UI checks on multiple widths. After *separately approved* activation, smoke the actual protected `https://yangspace.gndstore.eu` endpoint, including admin editing, errors and public visibility. Do not use destructive synthetic CRUD against the live site DB.
4. Capture version, command logs, REST status, browser screenshots, service health/latency and rollback evidence. Keep G0/G-PREVIEW HOLD until their independent security/infra gates are complete.

**No GitHub Actions; no auto-deploy. No PVE mutation has occurred in this source iteration.**
