# Target Architecture — .NET + Angular on PVE

## Целева структура

**Modular Monolith**, а не микросървиси. Един backend deployment с вътрешни граници; отделен frontend build/SSR и worker при нужда. Един LXC за пилота е допустим, с логическо разделяне на процесите, файловете и правата.

- **Frontend:** Angular (поддържана версия), mobile-first, публични SEO страници с SSR/prerender според избрания дизайн.
- **API:** ASP.NET Core 10 LTS, REST, OpenAPI, DTO mapping, validation, pagination, stable errors, ProblemDetails.
- **Domain:** Organizations, Locations, Geography, Categories, Offers/Campaigns, Brochures, Jobs, Moderation, Identity, Analytics.
- **Persistence:** EF Core и релационна база; SQL Server е наследена база. Изборът SQL Server vs PostgreSQL не е окончателен до ADR с тестове за миграция, backup, spatial и license/cost.
- **Media:** безопасни uploads, преобразуване/оптимизация, object storage или файлово хранилище с отделен backup.
- **Jobs:** expiration, moderation reminders, cleanup, outbox/event processing, re-indexing.
- **Proxy:** Nginx, HTTPS, security headers, rate limiting и ограничения на uploads.

## Модули и зависимости

- **Identity & Access**: owner/editor/moderator/admin; business/location memberships и policy-based resource authorization.
- **Directory**: organizations, locations, hours, contacts, address, geo, verification and lifecycle.
- **Content**: offers, campaigns, brochures, job posts; schedules and publication rules.
- **Moderation**: claim review, abuse reports, audit trail, publication approval.
- **Search/SEO**: filter/index contract, canonical URLs, schema.org/LocalBusiness where applicable, sitemap.
- **Measurement**: aggregated interactions, privacy-aware instrumentation.
- **Billing (future)**: feature entitlements independent from hard-coded prices; no MVP payment gateway.

Controllers never bypass authorization by extracting a bare user ID from unverified headers. Ownership is enforced in the API/service boundary and covered by integration tests.

## API conventions

- Public: `GET /api/v1/locations`, `GET /api/v1/locations/{slug}`, `GET /api/v1/offers`, `GET /api/v1/jobs`.
- Owner: `POST /api/v1/owner/locations/{id}/offers` and equivalents — requires membership check.
- Admin: `POST /api/v1/admin/claims/{id}/approve` — restricted role + audit.
- Explicit status codes, no raw EF entities in public responses, consistent error shapes, bounded page sizes, idempotency for business-critical writes.

These are **proposed contracts**, not implemented endpoints.

## Public SEO and delivery

- Публичните профили трябва да се индексират без JavaScript-only dependency.
- Human-readable slugs with stable IDs; canonical, metadata, sitemaps, structured data where appropriate.
- Не индексираме невалидни, оттеглени или дублирани бизнес профили.
- Media cache policy, responsive images, accessible UI, WCAG-oriented checks.

## Data/consistency

- Миграции с review, backward-compatible rollout, тестови данни без реална лична информация.
- Data ownership by organization + location; не допускаме cross-tenant достъп.
- UTC timestamps, local timezone for opening hours and campaign scheduling.
- Audit actions and immutable references за критични промени.
- Reconciliation for expired content, orphan media and stale profiles.

## Reliability & observability

- Health: /health/live, /health/ready с проверка за DB/worker dependency.
- Structured JSON logs с correlation/request ID; без tokens, лични данни и secrets.
- Metrics: API p95/p99, error rate, DB connections, storage, queued/failed jobs, media failures, age of verification.
- Alerts за downtime, backup failure, storage exhaustion, elevated 5xx, unprocessed moderation queue.
- Nightly data backup, отделно media backup и restore drills; RPO/RTO се определят преди go-live.
- Versioned releases, controlled rollout/rollback и ясни deployment logs.

## Не използваме

Azure hosting/deploy, Windows IIS, Jenkins, Kubernetes или Docker като production зависимост. GitHub остава само source control и PR; operator-run exact-SHA QA с локални logs замества CI. PVE LXC е целевият runtime. GitHub Actions не се използват.

## Legacy migration principle

Старите `Service`, `Provider` и `Booking` модели не се преименуват механично в `Location`. Това са различни домейни. Първо домейн моделиране, миграционна стратегия и тестове; после постепенно пренаписване/изолиране.
