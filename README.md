# YangSpace 2.0

**Градът около теб.** Първо квартал във Варна, после цяла Варна и България.

> **STATUS: FUNCTIONAL PREVIEW FOUNDATION — NOT PRODUCTION READY.** Новият .NET + Angular код е в `apps/`. Съдържа публичен каталог, страници на обекти и изолирана preview администрация. Реалните обекти, verified ownership, production persistence, SEO/SSR, jobs lifecycle и billing не са завършени. Инсталация на публичен адрес е забранена до security / QA gates.

## Технологии и hosting

- **Backend:** ASP.NET Core **.NET 10 LTS**, EF Core, SQLite само за локален/изолиран demo preview.
- **Frontend:** **Angular 22**, TypeScript 6, mobile-first, без UI framework.
- **Host:** съществуващ **Proxmox VE (PVE)**, един отделен Linux **LXC** за preview; Nginx + systemd.
- **GitHub:** source, PR и CI. LXC е Git clone/fetch client; **няма автоматичен deploy при push**.
- **Изключени:** Azure, IIS, Jenkins, Kubernetes, Docker като deployment изискване.
- **Production database:** остава архитектурно решение (PostgreSQL или SQL Server). Preview SQLite **не** е миграционна основа за live данни.

## Проектна структура

```text
apps/
  api/           .NET 10 REST API, EF Core, preview identity
  web/           Angular 22 public directory + preview CMS
infra/pve/        Nginx/systemd templates и LXC deployment runbook
scripts/          Local API smoke и pinned GitHub build
docs/             Product, security, architecture, domain, roadmap
YangSpace.slnx    .NET solution
```

Наследената версия 2024 (стар booking app, .NET 8/Angular 18, IIS/Azure) е запазена чрез Git history и `archive/yangspace-v1-2024` branch; не участва в новата build/deployment конфигурация.

## Стартиране локално за Live Preview

**Изисквания:** .NET SDK 10, Node.js **22.22.3+** (или официално съвместима Node 24/26), npm.

### Terminal A — .NET API

```bash
dotnet restore apps/api/YangSpace.Api.csproj
dotnet run --project apps/api/YangSpace.Api.csproj --launch-profile http
```

- API: `http://127.0.0.1:5090`
- Readiness: `http://127.0.0.1:5090/health/ready`
- Public list: `http://127.0.0.1:5090/api/v1/locations`
- Development-only preview data се създават в `yangspace-preview.db` в working directory.

### Terminal B — Angular

```bash
cd apps/web
npm install --no-audit --no-fund
npm start
```

Отвори **http://localhost:4200**. Angular proxy пренасочва `/api` и `/health` към .NET API. Страницата има 6 **измислени demo обекта**, маркирани като примерни. Няма фиктивни телефонни номера или реални адреси.

### Preview CMS

- За вход в `/admin` се ползва **само локален bootstrap admin**, създаден чрез `Bootstrap__AdminEmail` и `Bootstrap__AdminPassword` в environment / user secrets. Няма default парола.
- Задай стойностите **преди първия старт на API**; не ги записвай в Git. Паролата трябва да отговаря на Identity policy.
- Preview API включва `/api/auth/login` и admin-only location/offer/brochure/job creation и hide. Access token е само в паметта на Angular и се губи при refresh.
- **Няма публичен owner signup, upload или billing.** Identity registration routes са достъпни само в изолирана preview среда; новорегистрирани потребители нямат admin role.
- Публикуването от preview CMS е само за демо данни; не внасяйте реална лична информация.

> В момента няма надеждно заключен lockfile; `npm install` е временна bootstrap стъпка. За production трябва да се генерира/commit-не `package-lock.json` и CI да мине на `npm ci`.

## API

| Route | Access |
|---|---|
| `GET /health/live`, `GET /health/ready` | Public |
| `GET /api/v1/meta` | Public |
| `GET /api/v1/locations` | Public |
| `GET /api/v1/locations/{slug}` | Public |
| `POST /api/auth/login` | Preview only |
| `GET/POST/PUT /api/v1/admin/locations` | Preview + PlatformAdmin |
| `POST /api/v1/admin/locations/{id}/hide` | Preview + PlatformAdmin |
| `POST /api/v1/admin/locations/{id}/offers|brochures|jobs` | Preview + PlatformAdmin |

## PVE / LXC plan

1. Подготвяме отделен CT и инвентаризираме DNS, TLS, storage, backups.
2. Правим Git clone в контейнера. `origin` сочи GitHub, но няма webhook auto-deploy.
3. От конкретен проверен commit SHA пускаме `scripts/build-preview-from-github.sh`; той **само build-ва** версия под `/srv/yangspace/releases`.
4. След backup и QA операторът активира версията чрез симлинк и systemd reload (runbook).
5. Изолирана preview среда, защитена от публично индексиране; production release само след P0/MVP acceptance.

[**PVE инструкции**](infra/pve/README.md) · [**Security gates**](docs/SECURITY_BASELINE.md)

## Product scope

Публичен каталог по квартали и категории, профили, активни оферти, брошури, работни позиции; търговците ще управляват собствените си обекти след claim/ownership workflow в следващ milestone. Точните тарифи са **TBD**, billing не е част от preview.

Документи: [Product](docs/PRODUCT.md) · [Architecture](docs/ARCHITECTURE.md) · [Domain](docs/DOMAIN_MODEL.md) · [Roadmap](docs/MVP_ROADMAP.md) · [Validation](docs/VALIDATION.md) · [Decisions](docs/DECISIONS.md)

## Release policy

Работим чрез GitHub PR, CI и документирани commit IDs. Без смесване на preview и live data. GitHub сам по себе си не публикува върху PVE. Не твърдим production readiness без build/test/runtime evidence.

**Лиценз:** преди публично разпространение трябва да бъде добавен и потвърден коректен LICENSE файл.
