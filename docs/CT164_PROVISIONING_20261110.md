# YangSpace CT164 — dedicated application LXC, operator-run provisioning (2026-11-10)

**Status:** CT164 OS provisioning and first boot **operator-confirmed PASS** on 2026-11-10; application installation, network ACL, backup and production release **NOT DONE**. GitHub does not execute host commands.

## Actual CT164 provisioning evidence — 2026-11-10

Operator ran the guarded host script from this runbook and returned PVE output with:
- Verified template download: `local:vztmpl/debian-13-standard_13.6-1_amd64.tar.zst` (checksum verified by `pveam`).
- `pct create 164` completed, `pct start 164` completed, `pct status 164` = **running**.
- Guest `yangspace-app` reports **Debian 13.6 (trixie)**. `pct config`: `arch=amd64`, `cores=2`, `memory=4096`, `swap=1024`, `rootfs=local-lvm:vm-164-disk-0,size=20G`, `unprivileged=1`, `onboot=0`, `ostype=debian`.
- Guest network: `eth0`, `vmbr0`, `firewall=1`, `hwaddr=BC:24:11:8D:FE:7B`, `ip=dhcp`; actual lease **192.168.1.110/24**, default gateway **192.168.1.102**. `192.168.1.164` was never assigned. A reservation at the DHCP server is pending; do not change the guest to static or change CT152 before this is proven.
- Guest systemd was `starting` when captured, so boot health remains **UNVERIFIED** until repeated inspection.
- PVE reported **LVM thin provisioning warning**: provisioned thin LV sizes total **1.03 TiB** versus a `pve/data` pool of approx. **349 GiB**, and `thin_pool_autoextend_threshold` protection is not configured. This warning is **NOT** evidence that the 20 GiB guest allocation is already exhausted, but confirms oversubscription risk. The last `pvesm status` showed `local-lvm` ~83% used with only ~58 GiB available. **Capture live data and metadata usage, VG free extents, thresholds, and growth risk before installing application dependencies or enabling onboot.** Do not alter existing thin pool/VG settings automatically.
- Evidence (operator host): `/root/yangspace-ops/ct164/{template-download,create,start,config-after-create,guest-verification}.log` when present; chat console captured full create output. The host evidence directory is **not Git-tracked**.
- All services in CT152 and CT161 left unchanged; no application listener, proxy vhost, DB initialization, production credentials or public website was deployed.

**Gate:** `CT164_OS_CREATE=PASS`, `CT164_GUEST_HEALTH=HOLD`, `CT164_NETWORK_RESERVATION=HOLD`, `CT164_STORAGE_RISK=HOLD`, `CT164_APP_RELEASE=HOLD`, `G0/G-PREVIEW/G-PILOT=HOLD`.

### Read-only follow-up

On PVE, after verifying CT164 remains running:

```bash
set -Eeuo pipefail
umask 077
test "$(hostname -s)" = pve
test "$(pct status 164)" = "status: running"
echo '=== STORAGE / THIN POOL ==='
pvesm status
lvs -a --units g -o lv_name,lv_size,data_percent,metadata_percent,lv_attr pve
vgs -o vg_name,vg_size,vg_free
echo '=== CONTAINER HEALTH ==='
pct status 164
pct config 164
pct exec 164 -- bash -c '
  echo "=== GUEST ==="
  systemctl is-system-running || true
  systemctl --failed --no-pager || true
  ip -4 -brief address show dev eth0
  ip -4 route
  cat /etc/resolv.conf
  getent ahostsv4 deb.debian.org || true
  df -h /
  free -h
  ss -lnt || true
'
echo '=== PVE FIREWALL STATUS ==='
pve-firewall status
```

Do not assume `firewall=1` is sufficient: PVE datacenter/node firewall enable flags, LXC inbound/outbound policy, CT152 gateway access and LAN isolation require explicit review and negative tests.

## Verified PVE inventory (operator logs on 2026-11-10)

- PVE node `pve`, PVE 8.4.17, kernel 6.8.12-20-pve, Ryzen 9 5900HX 16 logical CPUs, 30 GiB RAM (~22 GiB available at inventory).
- VMID **164** absent from both `pct config 164` and `qm config 164` at inventory time; check again immediately before create. `pvesh get /cluster/nextid` returned 100, which does not reserve/forbid 164.
- Existing edge proxy **CT152**, `192.168.1.152/24`, `vmbr0`, Nginx port 80/443; syntax check PASS with unrelated existing SSL-option warnings. **Do not modify CT152 during provisioning.**
- Existing central data services CT161 (database/Redis) remain untouched. **YangSpace selects SQLite for the first local/private deployment** because the EF Core 10 SQLite migration, FK policy and disposable online backup/restore harness have been accepted. No MSSQL/MariaDB migration is authorized by this provisioning step.
- `vmbr0=192.168.1.151/24`, default route `192.168.1.102`. `vmbr230=10.230.0.1/24`, bridge-ports none, used by VM230 — not selected for YangSpace.
- `192.168.1.164` returned no ping/neighbor response, **not proof of address availability**. Use DHCP on CT164, then reserve an actual lease/MAC in router before DNS and public proxy. Do not statically claim .164.
- `local-lvm`: ~58 GiB available at inventory, 83.39% utilization. `local`: ~86 GiB available. `thin-backups`: ~9.6 GiB filesystem space available, ~90% mounted filesystem use.
- All listed storage resides on the same NVMe device. Local snapshots/backups are **not** an off-host recovery plan. No application or real customer data until backup destination, retention, encryption, restore evidence and RPO/RTO are accepted.
- Template listed upstream: `debian-13-standard_13.6-1_amd64.tar.zst`. Not yet present in `pveam list local` at inventory time.

## Target (provisioning only)

| Field | Value |
|---|---|
| CTID / hostname | 164 / yangspace-app |
| OS / security | Debian 13; unprivileged LXC |
| CPU / RAM / swap | 2 cores / 4096 MB / 1024 MB |
| Root filesystem | `local-lvm:20` |
| Network | `vmbr0`, DHCP, interface firewall bit on; actual DHCP lease TBD |
| Startup | `onboot=0` until smoke/hardening/ACL pass |
| Password/SSH | No password in command line or history; manage initial guest through `pct exec`; provision restricted deploy/service identities later |
| Application / public access | **Not installed**, no Nginx vhost, no external HTTP/TLS, no SQLite DB initialization |

The existing `infra/pve/nginx-preview.conf` serves static Angular files from the application CT and reverse-proxies `/api` to loopback .NET 10. The edge CT152 is the only TLS gateway. These are complementary edge vs internal web-server roles, not a duplicate public TLS proxy.

## PVE operator — guarded template acquisition and creation

Execute from the **PVE host only**. This intentionally changes the host by downloading one template, creating exactly CT164 and starting it for OS/network inspection. It never restarts other guests, edits CT152/CT161, assigns a static address, initializes DB, or deploys YangSpace. Source logs are retained in a root-only directory. If creation fails, **do not run automatic destruction or cleanup**.

```bash
set -Eeuo pipefail
umask 077

ID=164
TEMPLATE=debian-13-standard_13.6-1_amd64.tar.zst
VOL="local:vztmpl/$TEMPLATE"
OPS=/root/yangspace-ops/ct164
mkdir -p -m 0700 "$OPS"
test "$(hostname -s)" = pve
test ! -e "/etc/pve/lxc/$ID.conf"
test ! -e "/etc/pve/qemu-server/$ID.conf"

# Avoid ambiguous reuse of an old orphaned thin volume with the same VMID.
if pvesm list local-lvm --vmid "$ID" | awk 'NR>1 {found=1} END {exit !found}'; then
    echo 'STOP: CT164_STORAGE_VOLUME_ALREADY_EXISTS'
    exit 1
fi

test -d /sys/class/net/vmbr0
FREE_KIB=$(pvesm status | awk '$1 == "local-lvm" {print $6}')
[[ "$FREE_KIB" =~ ^[0-9]+$ ]]
(( FREE_KIB >= 45*1024*1024 )) || {
    echo 'STOP: LOCAL_LVM_HEADROOM_BELOW_45_GIB'
    exit 1
}

echo '=== TEMPLATE ACQUISITION ==='
if ! pveam list local | grep -Fq "$VOL"; then
    pveam download local "$TEMPLATE" 2>&1 | tee "$OPS/template-download.log"
fi
pveam list local | grep -F "$VOL"

# Re-check the VMID immediately before creating the guest.
test ! -e "/etc/pve/lxc/$ID.conf"
test ! -e "/etc/pve/qemu-server/$ID.conf"

echo '=== CT164 CREATE ==='
pct create "$ID" "$VOL" \
    --hostname yangspace-app \
    --ostype debian \
    --unprivileged 1 \
    --cores 2 \
    --memory 4096 \
    --swap 1024 \
    --rootfs local-lvm:20 \
    --net0 name=eth0,bridge=vmbr0,ip=dhcp,firewall=1 \
    --onboot 0 2>&1 | tee "$OPS/create.log"

echo '=== CT164 START ==='
pct start "$ID" 2>&1 | tee "$OPS/start.log"

echo '=== CT164 VERIFY ==='
pct status "$ID"
pct config "$ID" | tee "$OPS/config-after-create.log"
pct exec "$ID" -- /bin/bash -c '
    set -Eeuo pipefail
    echo "GUEST_HOSTNAME=$(hostname -s)"
    cat /etc/os-release
    ip -4 -brief address show dev eth0
    ip -4 route
    systemctl is-system-running || true
' 2>&1 | tee "$OPS/guest-verification.log"

echo 'CT164_INFRA_BOOTSTRAP=CREATED_PENDING_REVIEW'
echo "EVIDENCE_DIR=$OPS"
```

**Expected but not yet proved:** `CT164=running`, Debian 13, unprivileged 1, a DHCP lease and healthy guest systemd/network. A DHCP failure is HOLD and is not resolved by taking an arbitrary static IP.

## Strict post-provision gates

1. Review CT164 actual OS, DHCP lease, gateway, DNS and MAC; assign router DHCP reservation. If the lease is outside intended range or .164 conflicts, do not force the candidate IP.
2. Establish a default-deny or measured strict allowlist **before launching private-preview bootstrap**, with only CT152 -> CT164 internal HTTP and trusted admin network paths. `firewall=1` on the interface alone **does not prove filtering**.
3. Create separate non-root `yangspace` service and deploy users; controlled systemd, immutable releases, external `/var/lib/yangspace` DB/media, protected `/etc/yangspace` secrets and per-component logs. No sample credentials.
4. Run pinned exact-SHA source/API QA and protected operator approval on the new HEAD before inactive release build; `G0=HOLD`, Draft PR #1, no automatic GitHub Actions.
5. SQLite data and recovery: the Windows disposable runtime and online backup/restore accepted at `1ce3fa3`; **Linux/CT164 operational backup retention, off-host copy, restore, observability and RPO/RTO remain HOLD**.
6. Only after measured ACL/TLS/origin, DB migration & backup approval and admin negative tests may CT152 obtain a new dedicated YangSpace vhost. No existing proxy vhost change is authorized by this step.

## Actual post-create health — 2026-11-10 (operator PVE output)

**CT164 state: INFRA CREATED / APP HOLD / GUEST DEGRADED.** No application dependencies or public Nginx vhost are approved yet.

- CT164 running, DHCP `192.168.1.110/24`, default route `192.168.1.102`, `deb.debian.org` resolves, guest root filesystem 563 MiB used of 20 GiB, guest RAM ~15 MiB at inspection.
- `systemctl is-system-running` = **degraded**, with **22 failed units** including `systemd-journald.service`, `systemd-networkd.service`, `dbus.service`, `systemd-tmpfiles-setup.service`, `tmp.mount`, `run-lock.mount`, `dev-mqueue.mount`. **Do not interpret network/DNS success as guest health.**
- **Likely root cause, not yet proven in this guest:** Debian 13's systemd needs LXC `features: nesting=1` for these services. The CT164 creation omitted `--features nesting=1`, and `pct config` lacks a `features` line. Near-identical sets of failures in a Debian 13 unprivileged LXC were traced to disabled nesting and resolved when it was enabled: https://forum.proxmox.com/threads/lxc-unprivileged-container-journal-and-other-services-failed-to-start.178714/ (Jan 2026). Confirm failure reason (`243/CREDENTIALS` or related mount/namespace denial) before a controlled **CT164-only** change; do not enable privileged mode or extra capabilities.
- Actual thin pool `pve/data`: **348.82 GiB, Data%=83.77, Meta%=3.35**, VG `pve` **VFree=0**. This is limited free physical pool space without spare VG extents; aggregate thin LV nominal allocation and autoextend warning represent overcommit risk. `pve/lv_backups_pool` maps **95.09%** of its 100 GiB thin LV, while its ext4 filesystem previously reported ~90% used; `vm-168-disk-0` has **99.81% mapped blocks** within its thin volume (not proven guest filesystem usage). Do not delete snapshots/volumes or autoextend a zero-free VG.
- `pve-firewall status` = **disabled/running**. CT164 NIC `firewall=1` does not prove enforced ingress ACL. No private/public application listener until firewall authority is reviewed.
- Evidence: `/root/yangspace-ops/ct164/post-create-health.log`; operator output contains exact PVE measurements.

### Next isolated diagnostic — read-only

```bash
set -Eeuo pipefail
test "$(hostname -s)" = pve
test "$(pct status 164)" = 'status: running'
echo '=== LXC CONFIG FEATURES ==='
pct config 164 | grep -E '^(hostname|features|unprivileged|net0|onboot):' || true
echo '=== SYSTEMD FAILURE DETAILS ==='
pct exec 164 -- bash -c '
  systemctl show systemd-journald.service -p Result -p ExecMainStatus -p ExecMainCode --no-pager || true
  systemctl status systemd-journald.service -l --no-pager || true
  systemctl status tmp.mount -l --no-pager || true
  systemctl --failed --no-pager || true
  systemd --version | head -n 2 || true
'
```

If the observed failure class matches nesting-related Debian 13 systemd breakage, the next reviewed change is **only** `pct set 164 --features nesting=1` followed by **CT164-only** stop/start in an operator maintenance step. Re-evaluate systemd, network, listeners and security isolation afterwards. Do not run these mutation commands as an automatic consequence of the read-only diagnostic.

**HOLD:** gateway ACL/TLS, DHCP reservation, independent off-host backups, thin-pool capacity risk, CI-less exact-SHA runtime QA, app release, public traffic.
