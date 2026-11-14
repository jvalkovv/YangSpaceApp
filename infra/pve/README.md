# YangSpace — PVE / LXC Preview Runbook (NOT EXECUTED)

Целта е **отделен LXC в съществуващото PVE**, без Azure/IIS/Jenkins. Процесът се изпълнява след локален smoke/visual QA и избиране на CT ID. Не е извършен deployment в PVE.

## Преди provisioning

- Избери свободен CT ID, hostname, IP, storage и DNS. Не използвай на сляпо вече заети CT IDs.
- Debian 13/Ubuntu 24.04 LTS, unprivileged LXC когато е съвместимо.
- Ресурси (начална хипотеза): 2 vCPU, 3–4 GB RAM, 20+ GB storage; измерваме и коригираме.
- Осигури достъп до apt/npm/NuGet/GitHub от LXC, TLS пред публичен достъп и firewall.
- **Preview access ограничен** (например Cloudflare Access/VPN/ACL), защото има demo admin и временно `EnsureCreated` SQLite.
- Създай service account `yangspace` и отделен deploy/operator account; без `npm install` като root.
- Инсталирай .NET 10 SDK/ASP.NET runtime, Node **22.22.3+ (22.x) / 24.15.0+ (24.x) / >=26.0.0**, npm, Git, Nginx и SQLite backup tooling чрез одобрения OS package план.

## File layout

```
/srv/yangspace/repo          # checked-out GitHub repo, deploy operator owns
/srv/yangspace/releases/     # immutable SHA-named builds
/srv/yangspace/current       # symlink към активната версия
/var/lib/yangspace/          # preview SQLite / persistent media
/etc/yangspace/preview.env   # secrets, mode 0640, never tracked
/var/log/yangspace/          # API + deploy + verification logs
```

`/etc/yangspace/preview.env` се създава от [preview.env.example](preview.env.example) и се пази локално. Ако за bootstrap се задава admin парола, задай я временно чрез защитен secret процес; не я оставяй в history, screenshot или deployment log.

## GitHub connection (pull, **not** auto-deploy)

Клонирай веднъж, **като deploy operator**:

```bash
git clone https://github.com/jvalkovv/YangSpaceApp.git /srv/yangspace/repo
git -C /srv/yangspace/repo remote -v
git -C /srv/yangspace/repo fetch --prune origin main
git -C /srv/yangspace/repo rev-parse origin/main
```

Проверява се конкретен SHA със source/API PASS evidence от локалните операторски проверки; няма `git pull && restart` cron, webhook или директен deployment от push. Ако репото стане private — read-only deploy key / GitHub App, **не** personal access token в URL.

## Build на versioned release

```bash
YANGSPACE_SOURCE_QA_MANIFEST=/secure/source-manifest.txt \\
YANGSPACE_API_QA_MANIFEST=/secure/api-manifest.txt \\
YANGSPACE_RELEASE_APPROVAL_FILE=/secure/approval.manifest \
bash /srv/yangspace/repo/scripts/build-preview-from-github.sh <FULL_40_CHAR_COMMIT_SHA> feat/yangspace-v2-foundation-20261108
```

Скриптът допуска само allowlisted ref (`main` или активния v2 feature branch) и изисква SHA да е **точният remote tip**, плюс два ръчно проверени PASS manifests (source и API QA) за същия SHA. Само build-ва в отделна директория, **не активира нищо**.

## Manual activation след backup и review

1. Спри всякакви конкурентни операторски deployment процеси.
2. За SQLite preview backup чрез `sqlite3 /var/lib/yangspace/preview.db '.backup /secure/backup/path/...db'` от акаунт с правилни права; съхрани и off-host копие. Ако БД не съществува, това е първи preview deploy.
3. Провери release SHA, build logs и наличието на `api/YangSpace.Api.dll` и `web/browser/index.html`.
4. Под контрола на deployment оператора: атомарно превключи `current` symlink към валидирания release, след което `systemctl restart yangspace-preview.service`.
5. Валидирай `/health/ready`, каталог, detail, admin role и publish/hide; следи systemd/Nginx logs.
6. При регресия върни предишния release symlink; **DB rollback се прави само с отделна проверена процедура**, защото schema/data може да са се променили.
7. Документирай commit SHA, backup reference, operator, start/end, verification и rollback evidence.

Операторските switch/restart команди **не са автоматизирани** умишлено: липсват потвърдени CT ID, DNS, storage, TLS и backup topology.

## Nginx и systemd

- [nginx-preview.conf](nginx-preview.conf) е **пример** за HTTP backend в изолирана мрежа, предназначен за TLS-terminating gateway; не го излагай директно в интернет.
- [yangspace-preview.service](yangspace-preview.service) е шаблон; адаптирай действителния `dotnet` binary path, service account и workspace.
- `ConnectionStrings__Main=Data Source=/var/lib/yangspace/preview.db` е само за preview.
- `ASPNETCORE_ENVIRONMENT=Staging` + `Preview__Enabled=true` са изисквания за preview bootstrap.
- Публичният frontend остава с `noindex` докато не приключат SEO/SSR и проверката на данните.

## Monitoring и recovery

- Health endpoints и Nginx status, systemd restart count, 5xx, p95 latency, SQLite errors/lock waits, disk capacity.
- Разделни logs за build, deploy/verify, API, Nginx.
- Редовни SQLite online backups + media backup + offsite copy + restore drill преди production.
- Fail closed: при липсващи secrets/schema/container permissions — не стартираме release.

### S0-06 Release approval boundary

The build script now also requires a separate manually reviewed **operator approval** file with SHA-256 of both source/API QA manifests and exact SHA/ref, located outside the Git checkout. `WEB_BROWSER=SKIPPED` and missing bootstrap-denial evidence are rejected. The manifest and approval files must be transferred to protected Linux paths; the approval file should be owned by the deploy operator or root with mode 0600. Check [PVE release runbook](../../docs/POC_PVE_RELEASE_RUNBOOK.md) for the exact fields and private-preview HOLD policy. `test-preview-release-evidence.sh` only exercises synthetic policy fixtures, not the real deployment. No GitHub Actions or automatic deployment.
