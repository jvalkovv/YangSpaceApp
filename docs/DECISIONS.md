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
| ADR-007 | База данни: оставаме с SQL Server или мигрираме към PostgreSQL | Open |
| ADR-008 | Media storage: file-backed vs S3-compatible object storage | Open |
| ADR-009 | Търговски профил безплатен; платени опции/цени след pilot | Pricing TBD |
| ADR-010 | No automated deploy from `main`; CI on GitHub, audited PVE release | Accepted |
| ADR-011 | Native mobile app, booking, in-platform CV collection, payments out of MVP | Accepted |

## Change protocol

За всяко изменение: `ADR-NNN`, причина, алтернативи, security/privacy/ops impact, owner, дата и одобрение. Не променяме съществуващия `main` application runtime без целево PR и regression evidence.
