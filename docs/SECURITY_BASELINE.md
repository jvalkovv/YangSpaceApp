# Legacy Security Baseline — P0 HOLD

**Scope:** static code review на `main` от 2026-11-08, commit `5986972d3ea53f733c05a1ae139a9826a5fd177b`. Не е проведен pentest, local build, runtime verification или deploy.

**Общ статус:** NOT PRODUCTION READY. Конкретните рискове трябва да се потвърдят и отстранят преди публично използване; не използвайте съществуващите demo акаунти.

## P0 — Endpoint and resource authorization

- `YangSpaceApp.Server/Controllers/ServicesController.cs`: create/edit/delete/book routes не налагат консистентно `[Authorize]` и owner access.
- `YangSpaceApp.Server/Data/Services/ServicesService.cs`: update/delete се извършват по ID без проверка за право над съответния обект.
- `YangSpaceApp.Server/Controllers/BookingController.cs`: промяна на статус/общо изброяване имат нужда от resource-level authorization.
- Резултат: възможни неправомерни операции върху чуждо съдържание. **Fix:** server-side policies + membership/ownership checks + отрицателни integration tests.

## P0 — Unsafe file handling

- `ImageController.cs` exposes upload/delete без изричен auth.
- `ImageService.cs` uses supplied filename under a public path, without robust canonicalization, content signature validation, size limits, scan and quarantine.
- **Fix:** authenticated upload, generated names, verified MIME+magic bytes, content transformation, size limits, private quarantine, scoped permissions and safe download.

## P0 — Secrets and authentication coherence

- `YangSpaceApp.Server/appsettings.json` в публичната история съдържа DB credentials и JWT signing secret.
- `README.md` и `Data/SeedData/Seed.cs` в legacy history съдържат demo credentials. Премахването от текущия README не изтрива Git историята.
- `Program.cs` изисква JWT issuer/audience; `UserService.cs` не ги генерира изрично.
- `JwtHelper.cs` валидира с отключени issuer/audience проверки, докато други endpoints разчитат на ръчно извличане на токен.
- **Fix:** rotation of all potentially usable secrets, secrets outside Git, standard JWT bearer authentication and policies, login tests, expired/revoked-token tests, clock skew + issuer/audience settings.

## P1 — Build/test and data correctness

- Angular source references missing component stylesheet paths (`.scss` while only `.css` exists for checked components).
- Booking service methods `GetBookingByIdAsync` and `GetUserBookingsAsync` currently return null.
- Frontend/backend route mismatches and localStorage token handling need remediation.
- No EF migrations directory or server-side test project visible in current tree.
- Legacy workflow used Windows runner, mutable `npm audit fix` and direct Azure deployment; new workflow is CI-only/documentation gate until modernization.
- Main branch is based on .NET 8 / Angular 18; upgrade and vulnerability scan required.

## Exit gates

- [ ] Verify no reachable unauthenticated state-changing endpoints.
- [ ] Every owner request has server-side membership authorization and negative tests.
- [ ] Upload hardening and malware/content tests pass.
- [ ] Secrets rotated/removed from effective runtime and CI; incident exposure assessed.
- [ ] Full automated backend/frontend tests and security scan green.
- [ ] DB migration and rollback procedures validated.
- [ ] Human review before enabling any production deployment.

## Evidence policy

For each P0 fix: issue/PR link, commit SHA, test command, sanitized output, affected endpoint list, threat model note. Security findings are based on repository code; live exploitability must be assessed in an isolated test environment.
