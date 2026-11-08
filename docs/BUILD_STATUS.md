# YangSpace 2.0 — Verification Status

## Implemented (source, not yet acceptance-tested)

- `apps/api`: .NET 10 minimal API; EF Core SQLite **preview**; public catalog/meta/detail; demo data; Identity-based isolated PlatformAdmin content creation and hide.
- `apps/web`: Angular 22 mobile-responsive listing, filters, detail and demo admin workflow.
- `infra/pve`: sample Nginx/systemd + controlled pinned SHA GitHub build script; **no PVE provisioning was done**.
- `.github/workflows/main_yangspace.yml`: CI build + API smoke + Angular typecheck/build. **No deployment job**.

## Must pass before live preview

- [ ] GitHub CI API and Angular jobs green on exact commit.
- [ ] Local `dotnet run` and Angular `npm start` working together.
- [ ] Manual visual QA: 375/390/768/1024/1440 px, keyboard navigation, link targets.
- [ ] Admin login, create location/offer/brochure/job and hide verified, guest actions rejected.
- [ ] Verify dependencies, generate and commit `package-lock.json`, switch to `npm ci`.
- [ ] QA input validation and accessibility tests.
- [ ] Security review of ASP.NET Identity preview only and content media URL handling.
- [ ] PVE CT/DNS/TLS/backup/restore planning approved before deployment.

## Not implemented / explicitly deferred

Business-owner verification & memberships, production DB migrations and storage, direct uploads, true moderation/audit workflow, real local business data, SSR/SEO go-live, image asset pipeline, pricing/payments.

## Major caveat

SQLite `EnsureCreated` is intentionally enabled only in non-production `Preview:Enabled`; not a production migration strategy. Admin preview uses bearer token in memory (no localStorage). These are developer-preview controls, not authorization for public production launch.
