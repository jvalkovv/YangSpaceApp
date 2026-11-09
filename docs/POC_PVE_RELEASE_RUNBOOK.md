# YangSpace 2.0 — Private PVE Preview / Controlled Release Runbook

**Status:** PREPARATION ONLY — no CT provisioned and no deployment performed.  
**Scope:** PVE LXC proof-of-concept. A future public pilot has stricter release gates.  
**Authority:** [POC_MASTER_PLAN.md](POC_MASTER_PLAN.md), [POC_TRACKER.md](POC_TRACKER.md), [POC_ACCEPTANCE_MATRIX.md](POC_ACCEPTANCE_MATRIX.md).

## 1. Non-negotiable boundaries

- Existing Proxmox VE infrastructure, **one dedicated application LXC** for the initial private preview unless inventory establishes a different need.
- **No Azure, IIS, Jenkins, Kubernetes, Docker requirement or push-to-deploy.**
- GitHub pull/fetch + reviewed commit SHA. An approved PR build is not the same as an untrusted arbitrary branch.
- **No public Internet exposure** of `Preview:Enabled=true` / Identity bootstrap / SQLite `EnsureCreated`. Access must be through trusted gateway/VPN/allowlist; frontend `robots.txt` is NOT security.
- No real merchant data, live credentials or CVs in preview.
- Host/service separation: code release immutable; DB/media/config outside release directories.
- PVE commands requiring CT ID, storage or network selection are **withheld** until those facts are inventoried. Do not guess a free CT ID from other projects.

## 2. Read-only inventory — operator fills this before provisioning

| Item | Evidence to collect | Status |
|---|---|---|
| Host/node identity | Hostname, PVE version/kernel, network bridge names | TODO |
| New LXC ID | Verified unallocated CT ID and naming | TODO |
| Compute | CPU/RAM baseline, resource contention headroom | TODO |
| Storage | Datastore type, free capacity, snapshots, consistent backup method | TODO |
| Networking | LXC subnet/IP, gateway, DNS, inbound ACL, private-only route | TODO |
| TLS/access | Existing reverse proxy, certificate path/renewal, VPN/access policy | TODO |
| Backup | PVE schedule, off-host target, retention, test restore location | TODO |
| Host credentials | Non-root deployment model and read-only GitHub access | TODO |
| DB policy | SQLite preview only or reviewed production DB spike; data location | TODO |
| Repository SHA | Exact reviewed commit with CI/test evidence | TODO |

Safe commands only after authorized console access: `hostname -s`, `pveversion -v`, `pct list`, `pvesm status`, `ip -br addr`, `df -h`. These do **not** choose resources or create containers. Avoid printing secrets or internal addresses in shared logs.

## 3. Target preview topology and directory layout

```text
Client browser
  -> authenticated TLS gateway / VPN / ACL
    -> PVE private LXC
      -> Nginx (serves Angular static files; /api -> 127.0.0.1:5090)
      -> yangspace-preview.service (ASP.NET Core 10, non-root)
      -> protected preview SQLite file (temporary PoC only)
      -> private logs / protected env file

GitHub -> manual fetch exact reviewed SHA -> immutable release -> operator activation
Backup -> consistent DB copy + media + config reference -> off-host -> restore drill
```

Expected layout, subject to host inventory:

```text
/srv/yangspace/repo
/srv/yangspace/releases/<sha>/{api,web,commit.sha}
/srv/yangspace/current -> releases/<sha>
/var/lib/yangspace/preview.db
/var/lib/yangspace/media/
/etc/yangspace/preview.env
/var/log/yangspace/{api,nginx,deploy,verify,restore}/
```

Service account `yangspace` reads release contents; deploy account owns git/release creation; only the API account writes to DB/media/log directories. Nginx must not serve `/etc`, `/var/lib` or source files. Systemd sandboxing/hardening verified with real runtime tests.

**Sizing initial hypothesis (not allocation):** 2 vCPU, 3–4GB RAM and 20GB+ disk for preview; revisit after measured build peak/RAM/disk use. If Angular building inside CT is too heavy, use a controlled artifact build and transfer with SHA checks rather than enlarging the CT blindly.

## 4. Approved SHA build gate

**Current blocker:** `scripts/build-preview-from-github.sh` fetches `origin/main` and requires `git merge-base --is-ancestor <sha> origin/main`. The v2 app exists in Draft PR #1, so this script cannot build that reviewed feature commit as-is.

Required S0 fix:
1. Separate **candidate ref** from **candidate SHA**, e.g. an explicit allowlisted PR branch or an approved merge to `main`.
2. Fetch the allowlisted ref and assert that the full SHA exists and is its ancestor/tip as the chosen release policy requires.
3. Require successful CI for that exact SHA, code review and signed/recorded operator approval (technical implementation determined in S0).
4. Refuse partial SHA, remote URL substitution, ref drift, missing CI or protected branch mismatch.
5. Build from detached immutable worktree into `releases/<40-char-sha>`; never execute arbitrary branch `postinstall` with production secrets.
6. After lockfile review, use `npm ci` with frozen dependencies; `dotnet restore` with audit and release profile.
7. The **build script must never change the `current` symlink, restart a service, migrate production DB or deploy itself**.

If the SHA has no verified CI, stop. No `git pull && systemctl restart` shortcut.

## 5. Provisioning sequence (only after inventory & authorization)

1. Capture read-only PVE inventory evidence and approve CT ID, unprivileged container options, OS template and private network.
2. Establish backup target and host recoverability before container creation.
3. Create isolated CT with minimum capabilities, time sync, patch policy and firewall; OS candidate Debian 13 or Ubuntu 24.04 LTS, chosen after compatibility verification.
4. Create deploy and service users, paths and permissions; install supported .NET 10 runtime/SDK as needed, compatible Node/npm for builds, Nginx and Git.
5. Install Nginx + systemd templates from `infra/pve/` **after validating on the target host**; default frontend is static build, not `ng serve`.
6. Create protected env file and secret sources locally, not in repository or shared terminal transcript.
7. Configure VPN/authenticated TLS gateway and block direct public access. Test deny rules explicitly.
8. Clone/fetch reviewed ref and build candidate into SHA-named release. Validate expected file hashes.
9. Initialize isolated disposable preview DB, smoke and admin bootstrap under protected access only. No automatic demo-user credentials.
10. Verify health + UI + create/edit/publish/hide/restore + API permission negative cases.
11. Run backup/restore/rollback drill and record evidence. Only then issue **G-PREVIEW PASS**.

Do not add a second LXC per plugin/module. For public pilot, consider a separate DB CT if isolation/restore measurements justify it, not because of a pre-set rule.

## 6. Manual release, verify and rollback

### Preflight: refuse unless all hold
- Approved full 40-character SHA matches reviewed source and exact CI.
- No uncommitted release source, no environment secret in repo, signed/offline evidence of code review.
- Valid `release/<sha>` build and hash manifest; enough disk and RAM, service account permissions checked.
- Private access policy and TLS tested; no Internet-facing open preview registration/admin.
- Healthy current release and **fresh verified** consistent DB backup, off-host copy and media/config protection.
- Migration review passed; no irreversible schema change without planned restore.

### Release
1. Operator records current `current` target and logs command outputs to separate protected deploy log.
2. Deploy immutable new release directory; code cannot write to its own release path.
3. Before any migration, validate backup; **preview SQLite schema initialization is only for isolated disposable data**.
4. Atomically switch `current` symlink; restart only YangSpace API (not entire PVE host/other services).
5. Smoke from private gateway: `/health/live`, `/health/ready`, `/api/v1/homepage`, detail route, admin role and unpublished/expired content privacy.
6. Test browser on mobile+desktop and negative non-admin requests; record timestamp/SHA/HTTP codes.
7. Monitor 5xx, p95 latency, restarts, disk use and exception logs for acceptance window.

### Rollback
- If new API cannot start or smoke fails, point `current` back to previous **verified** release; restart service and run smoke.
- If DB schema/data has changed incompatibly, **code-only rollback is not sufficient**. Follow reviewed DB restore/reconcile procedure with downtime notice and validate records.
- Never erase backup or release evidence during cleanup; label rollback outcome PASS/HOLD and root cause.

## 7. Recovery & observability gates

- Logs by component: API, Nginx, deploy, verify and restore. Capture commit SHA and request ID; **no passwords/JWT/cookies or customer personal data**.
- Dashboard/alerts: health availability, HTTP error rate, p95 response, CPU/RAM, disk/inodes, DB failures, restart count, backup freshness, TLS expiry, scheduled content expiry lag (when worker exists).
- Backup: consistent DB file-copy via SQLite backup API while preview runs, separate media/config backup, off-site copy, snapshot alone not enough. Periodically restore into scratch and run queries + startup test.
- Proposed operational goals (not guaranteed): **RPO ≤24h, RTO ≤4h** after tested procedures; adjust based on actual pilot needs.
- Simulated failures for preview: stopped API, lost DB connection, wrong release, full disk, expired TLS, unavailable remote backup. Record detect→recover evidence.

## 8. Security/release threat checklist

| Risk | Gate |
|---|---|
| Internet-exposed preview signup/admin | NO-GO until private gateway + policy tests |
| PVE root/deploy user can be abused by app | Separate least-privilege service accounts, restricted paths |
| Secrets leaked in logs/URLs/Git | Protected env and scan; rotate reused legacy secrets |
| Invalid uploads or file/path traversal | No public upload until content validation/quarantine/scan controls exist |
| Malicious commit or dependency injection | Allowlisted approved SHA, `npm ci`, CI and code review |
| Incompatible DB rollback | Forward/backward migration review and successful restore drill |
| Old/unsupported runtime dependency | Verify .NET/Angular/Node compatibility and patch servicing |
| Data retention/copyright violations | Only fictional preview; legal/product gate before pilot |

## 9. Separate public pilot release gate — G-PILOT

This runbook only authorizes an **authenticated/private preview** after G-PREVIEW. For an Internet-visible YangSpace pilot, require **G-PILOT** from [POC_ACCEPTANCE_MATRIX.md](POC_ACCEPTANCE_MATRIX.md): production DB migrations and backups, scoped business-owner authorization, moderated real publication workflows, privacy/legal/content-rights review, no preview bootstrap/EnsureCreated, SEO/indexability policy and actual monitored load. Private preview success **never automatically promotes** a candidate to public traffic. The public release needs an independent explicit GO/NO-GO decision.

**No push-to-deploy is permitted.** GitHub CI remains validation-only; operator-approved release actions are separate.

## 10. Initial operator worksheet (to fill in later, not fabricate)

```text
PVE_NODE:
CT_ID:
CT_NAME:
OS_TEMPLATE:
IP_AND_BRIDGE:
CPU_RAM_DISK:
STORAGE_ID:
GATEWAY_TLS_AND_ACL:
BACKUP_STORAGE_AND_RETENTION:
OFF_HOST_DESTINATION:
GITHUB_APPROVED_REF:
CANDIDATE_FULL_SHA:
CI_JOB_LINKS:
BUILD_LOG:
CURRENT_RELEASE:
NEW_RELEASE:
BACKUP_REFERENCE:
VERIFY_LOG:
RESTORE_DRILL:
ROLLBACK_REFERENCE:
GATE_DECISION: PASS | HOLD | NO-GO
```

**The next infra action is READ-ONLY INVENTORY, not container creation.** Source and verified CI gate first, then protected provisioning.
