# PVE / LXC Operations Baseline

## Deployment decision

- **Хост:** съществуващ Proxmox VE (PVE), без фиксиран CT ID преди inventory.
- **Runtime:** отделен Linux LXC за YangSpace 2.0 (Debian 13 или Ubuntu 24.04 LTS след compatibility review).
- **Services:** Nginx (reverse proxy/TLS), ASP.NET Core API под systemd, Angular SSR/статичен frontend според окончателната реализация, DB и background worker.
- **Без:** Azure, Windows IIS, Jenkins, Kubernetes, Docker за production.
- **GitHub:** source/PR; **без GitHub Actions**. QA е локална, с exact SHA logs; deployment — отделна контролирана операция.

## LXC topology за MVP

1. Host/PVE: compute, snapshots, backup orchestration, network.
2. Isolated LXC: Linux users/service accounts, firewall, applications, reverse proxy.
3. Persistence: DB persistent storage, media storage и конфигурация извън release артефактите.
4. External off-site backup target: DB dumps/consistent backups + media + protected configs.
5. DNS/TLS: публичен домейн след одобрена DNS, certificate renewal monitor, no direct database exposure.

Няма нужда от нов LXC за всеки модул. Решение за отделяне на DB в собствен контейнер се взема по показатели за load, isolation, backup/recovery и operational risk.

## Hardening baseline

- Unprivileged LXC ако dependency compatibility позволява; минимални capabilities и достъп.
- Непривилегировани systemd service accounts с ограничени директории.
- Firewall: само необходимите inbound HTTPS/SSH management routes; DB/worker не са публични.
- Secrets в protected host-local files или секретен store, с permission management и rotation; никога в Git/QA logs.
- Upload quarantine, type/size validation, antivirus/scanning where feasible, separate storage.
- Automatic security patch policy, pinned application dependencies, audit log и rate limits.
- Non-production test environment отделно от live данни, без копия на чувствителни данни.

## Release procedure (план, НЕ изпълнени команди)

1. Проверка на source review, ръчните source/API PASS manifests и точен commit SHA.
2. Consistent database backup + media snapshot и off-site copy; verify успешност.
3. Maintenance gate при breaking DB change; review of migrations и forward/backward compatibility.
4. Release to versioned directory; config stays outside build output.
5. Stop/drain only needed services, apply migrations with logging, restart systemd units.
6. Smoke: API readiness, public listing, owner/admin auth, media, search, campaign expiry.
7. Check metrics, error rates, logs; запис на deployment/verification evidence.
8. При failure: код rollback + съвместима миграция; когато е нужно — controlled DB restore с одобрен recovery procedure.

**Не използвайте `pct`, `systemctl` или файлови пътища от този документ като готов production скрипт.** CT ID, OS, база, DNS, storage и backup jobs още не са инвентаризирани.

## Monitoring & recovery objectives

- Metrics: 5xx, request latency, DB health, disk usage, CPU/RAM, expired content worker lag, failed uploads.
- Logs: application, Nginx, worker, deploy/verify — отделни директории и retention.
- Backups: daily DB + media, off-site protection, restore drill преди go-live и периодично.
- RPO/RTO: предложена начална цел RPO <= 24 часа и RTO <= 4 часа **само след техническа проверка и одобрение**; не са гаранция.
- Проверки: TLS renewal, backup freshness, data freshness, broken links, indexation and queue health.

## Capacity planning

Single LXC е достатъчна стартова хипотеза, не обещание за национален обхват. Измерват се реални p95, CPU, memory, search latency, DB I/O и размер на PDF/media. Разделяне на компоненти става при доказан bottleneck.
