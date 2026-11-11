# MVP Roadmap — Gates, Not Dates

> **ADR-018:** GitHub Actions не се ползват. Проверки с exact-SHA локален runner, evidence logs и ръчен release.

> **Execution precedence (2026-11-09):** This is the original strategic milestone outline. Use [POC_MASTER_PLAN.md](POC_MASTER_PLAN.md) and [POC_TRACKER.md](POC_TRACKER.md) as the current executable plan. The original M0 legacy remediation items below describe historical risks; the old app is archived, not compiled in YangSpace 2.0. The active engineering work is dependency determinism/manual clean-SHA QA/real v2 auth and data integration, not patching old booking endpoints back into production.


**Не се поемат срокове до оценка на кода и работа по P0 дефектите.** Първо пилотен квартал, после доказано повторяемо разрастване.

## Delivery governance — mandatory progress reporting & PR closure (accepted 2026-11-11)

This is a **standing execution requirement** for all YangSpace work, including external Work runs and manual operator actions. It is not a product feature and does not substitute for tests or owner review.

1. **Report at meaningful checkpoints:** after an accepted scope decision, commit, completed test stage, PR creation/review, merge/closure, release or blocker. Include exact commit SHA, PR/issue link, environment, PASS/PARTIAL/HOLD, evidence location, next dependency and any production-vs-source drift. Do not publish empty progress updates or report unperformed work as completed.
2. **Hourly GitHub change watch:** existing YangSpace GitHub progress automation checks branch head, PRs, Issue #11 and gates. Notify on substantive changes only; a scheduled check is not a continuous build/deploy service, and progress reports do not imply asynchronous implementation.
3. **Small, independent PRs:** each PR states scope, base/head SHA, changed files, QA results, rollback/deployment consequences and Definition of Done. Do not mix unrelated CMS/infra/UI changes. Never use GitHub Actions: apply [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md) and retain separate `logs/yangspace/{verify,api-integration,deploy,restore}/` evidence as applicable.
4. **Close completed PRs only by an approved merge:** `100%` is scoped to **that PR's** agreed Definition of Done, including required exact-SHA QA, independent review and explicit owner approval. Merge into its approved target branch (prefer squash for cohesive changes), then verify GitHub marks it **merged/closed**. A manual `closed/unmerged` status is for an expressly abandoned/superseded PR and must state why. Never call a full project ready merely because all small PRs have merged.
5. **One open source of truth:** keep a current inventory of `OPEN` / `DRAFT` / `READY` / `MERGED` / `CLOSED UNMERGED` PRs in [SSOT.md](SSOT.md) and the active parent PR; reconcile the head after every merge. Full feature-to-`main` PR #1 remains open while release/pilot gates remain HOLD.
6. **No implicit deployment:** merges do not activate CT164/CT152, apply migrations, unlock admin/registration, or promote demo to production. Require a distinct runtime approval and SHA-specific smoke/rollback evidence.

### 2026-11-11 current milestone — F1a and F1b accepted / F2-F3 next

- **Public freshness F1a**: [PR #13](https://github.com/jvalkovv/YangSpaceApp/pull/13) MERGED/CLOSED, source+API QA PASS on candidate `9656518bb3d795a1e8cfaa7c3004678ee27fa6d4`, squash `54b47576ab8206d599f837cd99340c008d49f0d6`. Read-only UI change only.
- **Publication timestamps F1b**: [PR #14](https://github.com/jvalkovv/YangSpaceApp/pull/14) MERGED/CLOSED, source+API+SQLite runtime QA PASS on candidate `2634014f64fcff237b29564d0572643f2cba034a`, squash `403b281abad2fb0c8048c2763a606c3435f3e5dd`. EF-generated migration preserves old unknown timestamps as NULL; all QA on disposable DB.
- **Next engineering package: F2** — Organization/BusinessClaim/Member authorization contract → atomic per-field actor audit and optimistic concurrency with negative cross-tenant tests. **F3** owner confirmation and moderation only after F2. See [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11).
- **No public release implied:** PR #1 remains DRAFT/OPEN; G0/G-PREVIEW/G-PILOT still HOLD. CT164 active demo and DB were not changed, off-host restore and direct-origin security need independent operator acceptance. Older exact-commit engineering QA snapshots below are history, not current branch HEAD.

### Accepted exact-commit engineering QA snapshot

- Source SHA **`2baeb797b4d9ca0f5ce9d6c3d311a7b8092be7dc`**, V2 feature branch after merged [PR #9](https://github.com/jvalkovv/YangSpaceApp/pull/9) and [PR #10](https://github.com/jvalkovv/YangSpaceApp/pull/10).
- Owner-provided Windows PowerShell evidence: `SOURCE_VALIDATION=PASS` (lockfile, npm ci/audit, Angular typecheck/build, Playwright, .NET restore/build) and `REAL_API_INTEGRATION=PASS` (disposable SQLite, restart/persistence, anti-escalation checks). Both tests were pinned to the **same exact SHA**.
- Local-only, uncommitted evidence directories:
  - `D:\GitHub\YangSpaceApp\logs\yangspace\verify\2baeb797b4d9-20261111-074951-04b1acd1`
  - `D:\GitHub\YangSpaceApp\logs\yangspace\api-integration\2baeb797b4d9-20261111-075201-77648186`
- **Engineering source QA PASS** does **not** constitute off-host restore, direct-origin firewall acceptance, production merchant ownership/audit, deployment acceptance or permission to merge PR #1 into legacy `main`. Reference [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11) for freshness and edit history.

## M0 — Legacy Recovery / Security / Manual QA

- [ ] Branch protection, restore point/tag на наследения baseline, зависимостите са инвентаризирани.
- [ ] Ротация на потенциално компрометирани DB/JWT credentials и премахване на secrets от tracked configuration.
- [ ] Fix на endpoint authorization и ownership за create/update/delete/booking/admin.
- [ ] Upload validation, storage isolation, ограничения и malicious-file testing.
- [ ] Кохерентна JWT/Identity конфигурация; ролеви политики и integration tests.
- [ ] Миграционен път от .NET 8 към .NET 10 и Angular 18 към поддържана версия.
- [ ] Зелен build + automated test baseline; повтаряем dependency restore, без автоматичен `npm audit fix`.
- [ ] Публикувани безопасни local development инструкции, без demo credentials.
**Exit gate:** никакви отворени P0 auth/upload/secrets дефекти; локалните exact-SHA source/API тестове са PASS; няма production deploy.

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
