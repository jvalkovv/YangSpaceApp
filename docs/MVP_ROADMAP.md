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
7. **In-conversation progress is mandatory, monitoring optional:** during an active implementation/review, provide concise Bulgarian progress reports before/after meaningful stages (source change, diff, test, finding, blocker, PR transition). Never wait for a scheduled hourly watcher to report progress. State `completed / evidence / next` with exact SHA and distinguish local static checks, operator-provided QA, real runtime and deployment. Do not imply the assistant will independently continue between messages, and do not add a new monitoring schedule unless requested.
8. **Review open PRs for 100%-scoped completion at each closure checkpoint:** for each open PR record its target branch, exact head, agreed DoD, completed vs missing QA, review, runtime/security and required owner approval. Only fully accepted scoped changes move to READY → MERGED → verify CLOSED; a merely compiling or generated migration is not "100%" when real DB/recovery or authorization tests are still required. Do not close Draft PRs for visual hygiene; if the scope is genuinely abandoned/superseded, close unmerged with rationale and linked replacement. After an accepted merge, refresh PR inventory and SSOT on the new base.


### 2026-11-12 08:15 Europe/Sofia — F2a-2 merged; F2a-1 parent revalidation required

**Scoped milestone: F2a-2 read-only bearer authorization COMPLETE.** [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) was marked ready after source review and operator evidence postflight, then squash-merged into the F2a-1 feature branch (not V2 or \`main\`).

- Tested code SHA: \`f579c70db9cc0986d2f1df8047158587f0e87eef\`.
- Documentation-only follow-up HEAD: \`bc0a77ea6b6bc794a2d9d6aefcbe3c12936a329e\`; GitHub compare confirms one modified Markdown file only, no code/test changes after the tested SHA.
- Squash merge / new F2a-1 branch HEAD: \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\`.
- Operator Windows exact-SHA restore, Release build with warnings-as-errors, tenant regression and real Identity bearer TestServer all PASS on \`f579c70...\`. Operator read-only postflight returned \`EVIDENCE_POSTFLIGHT=PASS\`: manifest, four runner/stdout/stderr stage groups, 23 expected postflight HTTP markers and SHA-256 receipts all verified. Manifest SHA-256: \`336D8A9DCB4C69483981A2A5124A5123E8AD9044C14A1619199439E2E121F366\`. Detailed hashes and local evidence path are recorded in [F2a-2 QA evidence](https://github.com/jvalkovv/YangSpaceApp/blob/feat/yangspace-f2a1-domain-foundation-20261111/docs/F2A2_MERCHANT_BEARER_READ_QA_20261112.md).
- Accepted scope is disposable SQLite + ASP.NET Core TestServer only, with actual Identity-issued bearer tokens and the documented 401/404, tenant isolation, membership revocation, default-off, no-write and DTO allowlist checks. No PostgreSQL bearer runtime or production exposure is claimed.

### Current PR inventory (2026-11-12 08:15 Europe/Sofia)

| PR | Target | Exact current head | State / decision |
| --- | --- | --- | --- |
| [#19 — F2a-2 bearer-only merchant read](https://github.com/jvalkovv/YangSpaceApp/pull/19) | F2a-1 feature branch | merge \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` | **MERGED / CLOSED** |
| [#17 — F2a-1 domain + tenant foundation, now includes #19](https://github.com/jvalkovv/YangSpaceApp/pull/17) | V2 feature branch | \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` | **OPEN / DRAFT / HOLD** |
| [#1 — V2 parent / pilot](https://github.com/jvalkovv/YangSpaceApp/pull/1) | legacy \`main\` | current parent branch head; see PR | **OPEN / DRAFT / HOLD** |

**Why #17 remains HOLD:** the previous F2a-1 disposable PostgreSQL migration/constraints/synthetic backup-restore PASS belongs to exact source \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`, not automatically to the combined head \`17d08530...\`. First re-run exact-SHA source/build and disposable PostgreSQL migration/constraint/recovery regression against the combined head. Then complete PostgreSQL bearer HTTP and parallel EF writers, reviewer-only claim/membership lifecycle, atomic application business+audit fault-injection, least-privilege DB role, off-host recovery and owner release sign-off. Do not merge #17 or #1 yet.

### Next execution order

1. **F2a-1 + F2a-2 combined-head regression:** clean exact-SHA checkout of \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\`; source/build and disposable PostgreSQL regression. Keep the old PASS attached to its historical SHA until the new combined-head run passes.
2. **[F2a-3 contract/design — Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) before code:** reviewer identity/role policy, no self-approval, claim state machine, Owner membership grant/revoke, transaction boundary, actor/reason audit allowlist, concurrency/unique-conflict behavior, fault-injection matrix and recovery semantics. Keep claim mutation separate from the already-merged read-only endpoint.
3. **F2a-3 implementation:** only after the contract is recorded in Issue #4/ADR and reviewed; exact-SHA PostgreSQL tests must prove claim + org/location ownership + membership + audit atomicity, with rollback on every injected failure.
4. **F2b:** content mutations and revision/409 semantics only after F2a authorization and audit acceptance.

**PR closure rule applied:** #19 is the only newly completed PR and is merged/closed. #17 and #1 remain open because their own scoped security/release DoD is incomplete. No PR was manually closed unmerged. No GitHub Actions, CT164/CT152 access, live DB mutation, migration, feature activation or deployment.

### 2026-11-12 08:02 Europe/Sofia — F2a-2 isolated Identity bearer HTTP acceptance

**Operator exact-SHA PASS.**

- **Exact tested SHA:** \`f579c70db9cc0986d2f1df8047158587f0e87eef\`, PR #19, stacked on F2a-1 base \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`.
- Operator Windows output: runner syntax PASS; restore PASS; Release build with warnings-as-errors PASS; existing tenant regression PASS; real Identity bearer HTTP TestServer PASS; final \`F2A2_MERCHANT_BEARER_HTTP=PASS\`.
- Evidence directory: \`D:\\GitHub\\YangSpaceApp-F2a2-Bearer-QA\\logs\\yangspace\\f2a2-merchant-http\\f579c70db9cc-20261112-075949-ea648860\`. Runner checks its required marker set and writes a manifest only after successful stages. **The actual local manifest and log bytes have not yet been independently received/reviewed.**
- **Accepted scope:** isolated synthetic SQLite/TestServer only: real Identity-issued bearer tokens; default-off and QA-only environment checks; 401 anonymous/invalid/cookie; owner/editor positive access; cross-tenant, unknown, unclaimed, suspended and revoked 404; forged actor and PlatformAdmin-without-membership denied; registration/cookie login/write route denied; strict five-field DTO and no business/audit side effects.
- **Explicit boundary:** \`LIVE_POSTGRESQL_BEARER_HTTP=NOT_TESTED\`; PostgreSQL bearer and parallel EF writers, reviewer-only claim grant/revoke, atomic business+audit application transaction, least-privilege production role, off-host recovery and deployment remain HOLD.
- **Source review:** exact PR #19 source reviewed (Program fail-closed gate, Bearer-only policy, single joined membership/resource query, allowlisted DTO, test startup scope and exact-SHA runner); no blocking defect found in this bounded read-only slice. Independent human review and operator evidence-file postflight remain pending.
- **PR inventory:** #19 remains DRAFT/HOLD pending independent evidence postflight/review/merge checkpoint; #17 DRAFT/HOLD pending real provider authorization, concurrency and audited mutation gates; #1 DRAFT/HOLD pending program release gates. **0 open PRs eligible for closure at this checkpoint.** Do not close unmerged or claim full F2a/production acceptance.
- **Next execution:** read-only postflight of the local evidence manifest and stage logs; then independent review/owner merge decision for #19. Next product/security slice is reviewer-only claim verification and membership grant/revoke with atomic audit and DB fault-injection tests, specified before implementation. Keep it separate from this read-only route.
- No GitHub Actions, CT164/CT152 access, live DB changes, migration, feature activation or deployment.

### 2026-11-12 07:49 Europe/Sofia — F2a-2 HTTP TestServer startup recovery

- **Operator exact-SHA verification:** `ceea4723ccc5d2fc7612e63ec3ff4239b37ea0b8`, Windows isolated worktree, `f2a2-restore`, `f2a2-build`, `f2a2-tenant-regression` **PASS**. `f2a2-bearer-http` **FAIL** before HTTP assertions; `Program.cs:46` rejects missing `ConnectionStrings:Main`. Operator log `logs/yangspace/f2a2-merchant-http/ceea4723ccc5-20261112-074923-0ce50a73` (local-only; underlying bytes not independently received).
- **Root cause:** `WebApplication.CreateBuilder()` reads/validates connection config before `WebApplicationFactory.WithWebHostBuilder.ConfigureAppConfiguration` late test injection. Preserve the API's fail-closed startup validation.
- **New source fix:** [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) `f579c70db9cc0986d2f1df8047158587f0e87eef`, single QA-fixture file (+68 lines). Pre-host scoped process-only SQLite QA configuration; restores prior environment after sequential test; Production/Development fail-closed probes retained. No API/runtime schema/deployment changes.
- **Exact-new-SHA acceptance:** NOT RUN; require Windows runner's restore, Release build, tenant isolation and actual HTTP Identity bearer matrix plus manifest. No hypothetical PASS. #19, #17 and #1 all remain Draft/HOLD; **zero open PRs closure-eligible** at this checkpoint. CT164/CT152 and live DB untouched.

### 2026-11-12 07:39 Europe/Sofia — F2a-2 build failure / recovery and PR closure review

**Mandatory reporting policy above remains in force** (merged via PR #18): progress after each meaningful change, evidence-bound PASS/HOLD, and closing only scoped 100%-accepted PRs through approved merge. This checkpoint is source-only; no GitHub Actions, CT164/CT152 changes, live DB migration or merchant feature activation.

| Open PR / target | Exact current candidate | Evidence / unfinished gates | Decision |
| --- | --- | --- | --- |
| [#19 — QA-only merchant Identity bearer read](https://github.com/jvalkovv/YangSpaceApp/pull/19) → F2a-1 branch | `ceea4723ccc5d2fc7612e63ec3ff4239b37ea0b8` | Operator's previous `62505864...` Windows restore PASS, build FAIL `CS0136` for duplicate test locals `owner`/`editor`; new one-file, four-line-pair QA fixture rename committed. **New exact-SHA build and actual bearer HTTP TestServer NOT RUN**. | **DRAFT / HOLD**; repeat exact-head local runner, independently review manifest; no merge |
| [#17 — F2a-1 domain/PG foundation](https://github.com/jvalkovv/YangSpaceApp/pull/17) → V2 foundation | `1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001` | Disposable PostgreSQL migration/constraints/sequential CAS/audit rollback/pg_dump→pg_restore and SHA/cleanup checks PASS **for this exact source**. Reviewer claim-grants, real PG bearer, live EF parallel writes and production hardening pending. | **DRAFT / HOLD**; completed schema QA does not close entire PR |
| [#1 — V2 program](https://github.com/jvalkovv/YangSpaceApp/pull/1) → legacy main | Parent feature HEAD at time of review `0b67c5edcd4d2da3b5f9ee565e93f6a4fa18ad0e`; roadmap-only update will advance this | Production/pilot acceptance, tenant security, application-audit atomicity, operations and off-host recovery unaccepted. | **DRAFT / HOLD**; no merge to main |

**No open PR is 100%-accepted at this checkpoint; 0 PRs eligible for closure.** Historical merged PRs remain closed. Next gate is `Test-F2a-Merchant-Bearer-Local.ps1` using the newly fetched #19 commit, requiring restore/build (warnings-as-errors), tenant regression and genuine Identity bearer HTTP contract. Runner evidence path must be included in the next report. If the next test fails, preserve its stage output and return to RCA; no security test bypass.

### 2026-11-11 22:50 Europe/Sofia — progress and open-PR reconciliation

| PR | State | Scoped completion gate | Next |
| --- | --- | --- | --- |
| [#17 — F2a Organization/Claim/Tenant](https://github.com/jvalkovv/YangSpaceApp/pull/17) | **DRAFT / OPEN / HOLD** at `c96ff2e0ab76a2fc72713ff4e5516b5f40beb11d` | Source restore/build + isolated SQLite tenant/duplicate claim/revision tests passed; EF PostgreSQL SQL generation passed, latest ZIP static checksum/schema review passed. **Real disposable PostgreSQL migration, FK/UNIQUE enforcement, recovery and bearer API negative tests NOT ACCEPTED** | Commit independently reviewed EF-generated files without applying, then disposable PG DB/restore/tenant test evidence; recheck exact new PR SHA |
| [#1 — YangSpace V2 vs legacy main](https://github.com/jvalkovv/YangSpaceApp/pull/1) | **DRAFT / OPEN / HOLD** | Preview foundation exists, but true merchant ownership, audit, production DB, ingress and off-host recovery/pilot sign-off NOT ACCEPTED | Do not merge into `main` until all G-PILOT/release gates pass |
| [#16 — ADR-019 PostgreSQL direction](https://github.com/jvalkovv/YangSpaceApp/pull/16) | **MERGED / CLOSED** | Docs-only scope complete, no runtime activation asserted | Archive |
| [#13 — freshness](https://github.com/jvalkovv/YangSpaceApp/pull/13), [#14 — timestamps](https://github.com/jvalkovv/YangSpaceApp/pull/14) | **MERGED / CLOSED** | Exact candidate source/QA accepted for those bounded changes, not deployed | Archive |

**Proof boundary:** This roadmap snapshot does **not** approve PR #17 merge or database deployment. The latest uploaded PostgreSQL ZIP has matching operator/independent SHA-256 `f4ff533c34c11806b27a3572e7af1de31e26c507f0d18ca1ecc0933ddce5ca9e` and two correct filtered unique indexes plus `Locations.Revision`; still no applied PostgreSQL instance/restore. No CT164/CT152 changes. PR inventory is a dated snapshot, not live monitoring.

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
