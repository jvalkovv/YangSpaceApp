# S1-03 — Publication CMS: offers, brochures and jobs

**Date:** 2026-11-09
**Source:** `feat/yangspace-v2-foundation-20261108` (Draft PR #1)
**Runtime:** UNVERIFIED — code reviewed, no container deployment
**Target:** one YangSpace LXC, active canonical `yangspace.gndstore.eu`

## Scope

1. **Backend:** PlatformAdmin-only `/api/v1/admin/locations/{locationId}/content` returns all offers, brochures and jobs, including archived/expired. `PUT /locations/{locationId}/{kind}/{recordId}` changes metadata, dates and publication state. `POST .../{recordId}/hide` archives without deletion; `POST .../{recordId}/restore` re-enables valid content with 204 and returns 409 if expired. Mismatched parent/record IDs return 404. All operations inherit PlatformAdmin group authorization.
2. **Frontend:** dedicated `AdminPublicationsComponent` appears for the selected location inside admin; three grouped lists show archived, active, planned and expired states. Edit fields/period/HTTPS links, archive, restore and refresh. Create forms remain in parent and signal refresh after success. No untrusted HTML rendering.
3. **Public:** unchanged already-existing read-side filters require published location and publication, UTC start/expiry windows; this means hidden or expired content cannot appear in public detail/homepage once tests confirm the implementation.
4. **Test source:** PowerShell disposable-DB checks for security, create/update/archive/restore, no public leak and persistence after restart; mocked Playwright checks for editing and expired-item restore prevention. Neither suite has run on this new SHA.
5. **Explicit exclusions:** physical deletion, media upload, tenant/merchant scoped authorization, versioned audits, Production auth/migrations and public pilot access. Preview-only admin endpoints are not a Production readiness solution.

## Acceptance on one active LXC (only with explicit deployment authorization)

- Inventory source/ref, bootstrap mode, reverse proxy and TLS/ACL; verify operator approval, backup and rollback. Never weaken ASP.NET Production preview checks.
- Stage validation *within the same dedicated CT* using an isolated disposable DB and a different **ephemeral loopback port**, never live data, with no additional persistent CT.
- Validate API error codes (401/403, 404 wrong record parent, 400 validation, 409 expired restore, 204 idempotence) and same-state restart persistence.
- Confirm real admin browser flows, published/hidden/expired public visibility, UI loading and 375/390/768/1024/1440 responsive states, plus service health and logs.
- Use controlled atomic version activation with rollback rather than copying files over the running release. Keep G0/G-PREVIEW HOLD pending separate safety/availability gates.

**No container action, GitHub Actions, merge or automatic deployment is performed by the source change.**
