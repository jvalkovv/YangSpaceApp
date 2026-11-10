# YangSpace CT164 — dedicated application LXC, operator-run provisioning (2026-11-10)

**Status:** PREPARED, NOT EXECUTED. The user authorized beginning CT164 provisioning. No Proxmox action occurs from this document or GitHub commit. **Do not claim a running container until real PVE operator evidence is returned.**

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
