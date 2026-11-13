# MVP Roadmap — Gates, Not Dates

**Не се поемат срокове до оценка на кода и работа по P0 дефектите.** Първо пилотен квартал, после доказано повторяемо разрастване.

## M0 — Legacy Recovery / Security / CI

- [ ] Branch protection, restore point/tag на наследения baseline, зависимостите са инвентаризирани.
- [ ] Ротация на потенциално компрометирани DB/JWT credentials и премахване на secrets от tracked configuration.
- [ ] Fix на endpoint authorization и ownership за create/update/delete/booking/admin.
- [ ] Upload validation, storage isolation, ограничения и malicious-file testing.
- [ ] Кохерентна JWT/Identity конфигурация; ролеви политики и integration tests.
- [ ] Миграционен път от .NET 8 към .NET 10 и Angular 18 към поддържана версия.
- [ ] Зелен build + automated test baseline; повтаряем dependency restore, без `npm audit fix` в CI.
- [ ] Публикувани безопасни local development инструкции, без demo credentials.
**Exit gate:** никакви отворени P0 auth/upload/secrets дефекти; CI зелено; няма production deploy.

## M1 — Foundations

- [ ] ADR за база данни, географски модел и storage.
- [ ] Domain modules, EF migrations, unit/integration tests.
- [ ] Observability (health/metrics/logs), structured errors, feature flags if required.
- [ ] PVE staging LXC с Nginx, systemd, TLS, backup/restore процедура.
**Exit gate:** възстановима среда и работещ minimal vertical slice.

## M2 — Directory

- [ ] Organization + Location CRUD, categories, neighborhoods, hours, contact channels.
- [ ] Claim/verification flow, ownership and moderation queue.
- [ ] Public searchable directory, maps integration and location pages.
- [ ] SEO/SSR, sitemap, mobile accessibility.
**Exit gate:** 100+ качествени пилотни обекта могат да бъдат управлявани и открити.

## M3 — Campaigns & Brochures

- [ ] Оферти, период на валидност, изтичане, статуси, audit.
- [ ] Защитени uploads, PDF preview, media lifecycle.
- [ ] Owner dashboard с публикуване и основни статистики.
**Exit gate:** одобрените кампании се виждат коректно и изтичат автоматично.

## M4 — Jobs & Moderation

- [ ] Jobs CRUD с външно кандидатстване (без CV база).
- [ ] Пълна moderation/abuse-report обработка и data correction.
- [ ] Policy review: GDPR, рекламни намаления, авторски права, DSA (ако е приложим).
**Exit gate:** проверени end-to-end owner/admin/visitor flows.

## M5 — Pilot Launch / Validation

- [ ] Backup + restore test, security review, load smoke, performance/SEO acceptance.
- [ ] Onboard на първи търговци, публичен каталог, ограничен локален маркетинг.
- [ ] Dashboard за traffic, click-to-call, directions, актуално съдържание.
- [ ] Преглед на willingness-to-pay, без автоматично включване на billing.
**Exit gate:** repeatable onboarding, висока актуалност, измерена потребителска стойност.

## M6 — Варна / национално развитие (условно)

- [ ] Стандартизирани райони/градове, import governance, duplicate detection.
- [ ] Multi-location organization management, content quality automation.
- [ ] Monetization ADR по данни от пилота; billing само след правен/финансов review.
- [ ] Capacity planning, restore RTO/RPO tests, операционен runbook.

## Definition of Done за всяка функционалност

Source + tests + reviewed DB migration + auth check + audit/logging + docs + visual/mobile QA + runtime evidence. Всеки deployment има commit SHA, версия, backup reference, verify output и rollback path. Без `main` auto-deploy.

## Product Growth Direction — 2026-11-13 (not an MVP release approval)

See [verified local discovery, merchant growth and Varna pilot plan](PRODUCT_GROWTH_PILOT_20261113.md). Emphasize verified/fresh businesses, offers, brochures, jobs, mobile discovery, QR deep links and privacy-safe owner action analytics. Events/opt-in notifications and clearly labeled promotions are separate post-pilot possibilities. Do **not** add checkout, booking, accounting, warehouse, trade/POS or fixed subscription fees to M0–M5. Existing security, claim and PostgreSQL approval gates remain mandatory.
