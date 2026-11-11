# Architecture & Product Decision Log

Тук различаваме **приети решения**, **предложения** и **отворени въпроси**. Промените по архитектурата трябва да имат отделен ADR с дата, контекст, алтернативи и последствия.

| ID | Решение | Статус |
|---|---|---|
| ADR-001 | YangSpace 2.0 е квартален бизнес справочник + оферти + брошури + локални работни обяви | Accepted |
| ADR-002 | Пилотен квартал във Варна → Варна → България, само след validation gates | Accepted |
| ADR-003 | Backend: C# / ASP.NET Core, целево .NET 10 LTS | Accepted direction |
| ADR-004 | Frontend: Angular, target maintained release; SEO-first | Accepted direction |
| ADR-005 | Hosting: existing Proxmox VE, isolated Linux LXC; no Azure/IIS/Jenkins | Accepted |
| ADR-006 | Modular Monolith вместо микросървиси | Proposed baseline |
| ADR-007 | База данни: SQL Server vs PostgreSQL | Superseded by ADR-019 (production target) |
| ADR-008 | Media storage: file-backed vs S3-compatible object storage | Open |
| ADR-009 | Търговски профил безплатен; платени опции/цени след pilot | Pricing TBD |
| ADR-010 | No automatic deploy; GitHub-hosted CI clause superseded by ADR-018 | Superseded in part |
| ADR-011 | Native mobile app, booking, in-platform CV collection, payments out of MVP | Accepted |

## Change protocol

За всяко изменение: `ADR-NNN`, причина, алтернативи, security/privacy/ops impact, owner, дата и одобрение. Не променяме съществуващия `main` application runtime без целево PR и regression evidence.

## Proposed follow-up decisions — not silently approved

| ID | Topic | Status | Decision evidence required |
|---|---|---|---|
| ADR-012 | Private PVE build from allowlisted Draft PR tip or main | Implemented in source, runtime pending | Exact remote-tip SHA and matching manual source/API PASS manifests |
| ADR-013 | Production DB PostgreSQL vs SQL Server | **Direction resolved by ADR-019; rollout remains HOLD** | Target PostgreSQL; review provider-specific EF migrations, backup/restore, security and operator evidence |
| ADR-014 | Angular public SSR/prerender approach | OPEN | SEO indexability and deployment cost/test evidence |
| ADR-015 | Preview/private topology vs public-pilot DB isolation | OPEN | PVE inventory, persistence/backup, fault isolation and recovery tests |
| ADR-016 | Media provider and upload safety workflow | OPEN | File rights, quarantine, scan, backup and safe serving |
| ADR-017 | Pricing/subscription and billing | DEFERRED | Pilot conversion, business willingness-to-pay and support economics |
| ADR-018 | **Never use GitHub Actions**; GitHub only source/PR; manual pinned-SHA QA and PVE operator release | **Accepted (owner directive)** | [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md) |
| ADR-019 | PostgreSQL target for verified multi-tenant production; SQLite demo remains isolated | **Accepted direction; activation HOLD** | [ADR-019](ADR-019-POSTGRESQL-PRODUCTION-TARGET.md) |

**Canonical current project status and gate order:** [SSOT.md](SSOT.md). **Detailed planning:** [POC_MASTER_PLAN.md](POC_MASTER_PLAN.md). No existing accepted architecture decisions were overwritten by this planning commit.

