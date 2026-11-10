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
# pct create inherits the process umask for template extraction. Using 077
# here can produce /etc with mode 0700 and break non-root services (D-Bus).
# Apply a safe standard 022 ONLY to pct create; operator evidence stays 077.
(
    umask 022
    pct create "$ID" "$VOL" \
        --hostname yangspace-app \
        --ostype debian \
        --unprivileged 1 \
        --cores 2 \
        --memory 4096 \
        --swap 1024 \
        --rootfs local-lvm:20 \
        --net0 name=eth0,bridge=vmbr0,ip=dhcp,firewall=1 \
        --onboot 0
) 2>&1 | tee "$OPS/create.log"

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

## CT164 nesting change and second boot (2026-11-10)

**Operator change completed (CT164 only):** `pct stop 164`; `pct set 164 --features nesting=1`; `pct start 164`. The operator verified:

- `features: nesting=1`, `unprivileged: 1`, `onboot: 0`.
- Before: 22 failed systemd units, including `systemd-journald` exit status `243/CREDENTIALS`. After: **2 failed units** — `dbus.service` and `dbus.socket`; `systemd-journald` and `systemd-networkd` **active**. `systemctl is-system-running` remains **degraded**.
- Network remains `192.168.1.110/24`, but default gateway **changed from `192.168.1.102` to `192.168.1.107`** across the reboot. **DHCP source and lease configuration are unknown**; do not assume the new gateway is correct or modify DHCP/router config without verification.
- No other CT or proxy/service was changed. This is a substantially improved guest start but **NOT** `GUEST_HEALTH=PASS`.

**Next diagnostic only:** inspect the DBus failure `journalctl -b -u dbus.service -u dbus.socket`, systemd unit failure details and the networkd DHCP lease/router and DHCP server identity. Ask operator if `192.168.1.107` is a known router/DHCP service; compare gateway offered to CT152. Avoid `systemctl reset-failed`, package reinstall, another CT reboot, or DB/proxy/production deployment until a root-cause conclusion is supported.

**Gates remain HOLD:** DBus, DHCP/gateway consistency, pve/data low physical free capacity / overcommit, effective gateway-origin firewall ACL, off-host backups, app deployment and public exposure.

## 2026-11-10 — D-Bus error and template extraction umask root cause

After CT164's nesting=1 restart, D-Bus still fails:
```text
dbus-daemon: Could not get password database information for UID 989: Permission denied
dbus-daemon: Unknown username "root" in message bus configuration file
dbus-daemon: Failed to open "/etc/dbus-1/system.conf": Permission denied
dbus.socket: service-start-limit-hit
```

The initial host provisioning script ran `umask 077` **before** `pct create`. Proxmox staff confirmed that CLI `pct create` can inherit the caller's restrictive umask during template extraction, resulting in guest `/etc` not being traversable/readable by non-root users. Reference: https://forum.proxmox.com/threads/creating-a-debian-or-ubuntu-lxc-with-the-pct-create-command-makes-etc-in-the-container-not-world-readable.161231/ and https://github.com/community-scripts/ProxmoxVE/issues/16745.

**RCA status:** very strong, but CT164's actual `/etc` mode not yet inspected; do not claim confirmed until `stat` shows the permissions. This is a provisioning-script defect, not necessarily a Debian 13 package defect.

The historic (already executed) operator command is now corrected **for future reproducibility**: keep `umask 077` for logs/evidence, but execute **only** `pct create` inside a `(umask 022; ...)` subshell. This is not authorization to recreate CT164 or other guests.

Proposed CT164-only fix (operator-run after checking actual file permissions):

1. Read `stat` and `namei -l` of `/etc`, `/etc/dbus-1/system.conf`, `/etc/passwd` and `/etc/group`.
2. Only if `/etc` is owned by root:root with mode `0700` or `0750`, change **that directory only** to normal `0755`. Never recursively `chmod` `/etc`; do not relax secret files or modify LXC security profiles.
3. After this correction, reset the two failed D-Bus units and start socket/service again; verify D-Bus and systemd health. If `/etc` is already `0755`, stop and diagnose nested path components/ACLs instead.
4. Independently investigate the DHCP gateway drift from `192.168.1.102` (before restart) to `192.168.1.107` (after restart) before router reservation, edge-proxy changes or application deployment.

## 2026-11-10 — CT164 guest base-system recovery: PASS

Operator executed a narrowly scoped repair inside running unprivileged CT164:

- The underlying misconfiguration was **confirmed**: `/etc` had `root:root 0700`. It was repaired to normal `0755` after UID/GID and symlink guards; **no recursive changes**.
- `/etc/dbus-1/system.conf` was not present, while the shipped default `/usr/share/dbus-1/system.conf` existed and was readable. The first diagnostic script stopped at the absent optional path **before** changing permissions.
- After `systemctl reset-failed dbus.service dbus.socket` and restarting these units, `dbus.service`, `systemd-journald.service` and `systemd-networkd.service` all reported **active**.
- `systemctl --failed`: **0 failed units**. `systemctl is-system-running --quiet`: PASS. Operator marker: `CT164_DBUS_RECOVERY=PASS`.
- DHCP: `eth0=192.168.1.110/24`, gateway `192.168.1.107` at acceptance; the user explicitly selected **DHCP without router reservation for now**. The gateway drift from `.102` remains a risk to check for outbound routing, not a blocker for base file/user preparation.
- `CT164_OS_HEALTH=PASS` on observed runtime, but **application/security/backup gates remain HOLD**. Do not interpret OS health as authorization for a public reverse proxy.

**Root cause:** the initial provisioning runbook had applied `umask 077` to `pct create`, causing the extracted guest `/etc` directory to be mode 0700. The corrected provisioning snippet now scopes `umask 022` to `pct create` only, retaining `umask 077` for operator evidence logs.

Next: basic guest package/repository preflight, no active Nginx listener or production application. Before publishing through CT152, independently validate gateway reachability, CT firewall policy, TLS and approved off-host recovery. Keep `G0/G-PREVIEW/G-PILOT=HOLD`.

## 2026-11-10 — Base OS packages: installed / confirmed

Operator supplied exact `dpkg-query -W` evidence inside CT164:

| Package | Installed version |
|---|---|
| ca-certificates | `20250419+deb13u1` |
| curl | `8.14.1-2+deb13u7` |
| git | `1:2.47.3-0+deb13u1` |
| gnupg | `2.4.7-21+deb13u1` |
| jq | `1.7.1-6+deb13u4` |
| unzip | `6.0-29+deb13u1` |

Guest `systemctl is-system-running` returned `running`. Earlier long heredoc/tee attempt produced no log and no installed packages; the operator subsequently installed the packages via shorter commands and verified them.

**Status:** `CT164_BASE_PACKAGES=PASS` based on observed installed versions; `CT164_OS_HEALTH=PASS`. No .NET application, Nginx or public proxy installed/activated by this evidence.

### Network constraint selected by operator

- The operator confirms the actual network gateway is **`192.168.1.102`** and says **`192.168.1.107` is not an actual gateway**. CT164's last reported default route was **incorrect: `default via 192.168.1.107 dev eth0`**.
- User explicitly declines changing router / DHCP reservations; retain **DHCP address** on CT164.
- Proposed controlled next action: identify which networkd configuration or DHCP lease injects `.107`, correct only CT164's default route to `.102`, verify connectivity, then make route persistent using the actual existing guest network manager configuration. No automatic host-side network or router changes. CT152 and PVE host already use `.102`.
- Gateway remediation is not yet operator-confirmed. Do not claim `CT164_NETWORK=PASS` or expose HTTP app to public traffic.

Remaining public gates: firewall policy, off-host data backups, storage pool headroom, TLS/proxy and exact-SHA deployment QA.

## 2026-11-10 — temporary gateway correction, unresolved DNS

Operator `networkctl status eth0` on CT164 returned `State: routable (unmanaged)`, `Network File: n/a`; it still displayed gateway `192.168.1.107`. No matching systemd-networkd DHCP lease attributes were printed from `/run/systemd/netif/leases/*`. **Therefore networkd is not currently managing eth0**; do not apply networkd-specific permanent rules without identifying the actual owner (ifupdown / DHCP client / PVE-generated interfaces).
 
Operator then executed a **temporary guest route override**:
```text
ip -4 route replace default via 192.168.1.102 dev eth0
default via 192.168.1.102 dev eth0
1.1.1.1 via 192.168.1.102 dev eth0 src 192.168.1.110 uid 0
```
However the attempted `curl -sSI --connect-timeout 5 --max-time 12 https://deb.debian.org/debian/` failed with **`curl: (28) Resolving timed out after 5000 milliseconds`**. The route is a Linux FIB entry, not proof of Internet connectivity. DNS and route persistence are **HOLD**. No router change or static IP reservation is desired by operator.

Next **read-only** diagnostics: inspect guest `/etc/resolv.conf` content/symlink and guest `/etc/network/interfaces*`; identify DHCP client(s); test direct IP HTTPS egress separately from DNS; record guest default route. Apply no persistent override or nameserver change until authority and source of .107 are known. No application dependency downloads yet.

## 2026-11-10 — DNS failure narrowed to offered resolver .107, direct Internet PASS

Operator follow-up on CT164:

```text
/etc/resolv.conf:
  nameserver 192.168.1.107
  nameserver 192.168.1.102
/etc/network/interfaces:
  auto eth0
  iface eth0 inet dhcp
DHCP process:
  /sbin/dhclient -pf /run/dhclient.eth0.pid -lf /var/lib/dhcp/dhclient.eth0.leases eth0
Kernel route:
  default via 192.168.1.102 dev eth0
Direct HTTPS to https://1.1.1.1:
  HTTP/2 301 (Cloudflare)
```

Interpretation:
- The guest has functional IPv4 Internet connectivity over .102; earlier `curl https://deb.debian.org/debian/` timed out on *DNS resolution* with untrusted/unreachable .107 listed as primary resolver.
- `systemd-networkd` is running but **eth0 is unmanaged**; the active DHCP client is `dhclient` launched from ifupdown. Do not use networkd unit overrides.
- The operator has explicitly confirmed `192.168.1.102` is the actual LAN gateway and `192.168.1.107` is not a gateway. The originating **DHCP server identity** is not yet known; investigate `/var/lib/dhcp/dhclient.eth0.leases` (last lease's `option dhcp-server-identifier`, `option routers`, `option domain-name-servers`) before deciding whether to reject an unintended DHCP server or override its options.
- Safe CT164-only candidate: `supersede routers 192.168.1.102;` and `supersede domain-name-servers 192.168.1.102;` in `/etc/dhcp/dhclient.conf` (documented ISC `dhclient.conf(5)`) **after auditing existing directives**, retain `iface eth0 inet dhcp`; correct active resolver with guarded backup and verify DNS+HTTPS; confirm settings survive DHCP renewal/reboot before public release. Potential Proxmox-vs-dhclient resolver write order: https://lore.proxmox.com/pve-devel/20260311112057.48653-1-f.schauer%40proxmox.com/
- Do not modify router settings or require a reservation. DHCP still means the container's address can change; CT152 upstream discovery must accommodate change or stay private.
- **Status: CT164_IP_EGRESS=PASS, CT164_DNS=HOLD, CT164_ROUTE_PERSISTENCE=HOLD; app/public release remain HOLD.**

## 2026-11-10 — DHCP source evidenced, manual DNS recovery PASS

CT164 operator presented historical `/var/lib/dhcp/dhclient.eth0.leases` with multiple leases:
```text
option dhcp-server-identifier 192.168.1.107;
option routers 192.168.1.107;
option domain-name-servers 192.168.1.107,192.168.1.102;
```
Other leases offered `.107,8.8.8.8,1.1.1.1,10.0.0.1` for DNS. The DHCP server **identifier** in accepted leases is consistently `.107`; this proves that accepted leases *claim* `.107`, not which physical device sent them. The operator confirms `.102` is the real gateway and wants no router changes/reservations.

Operator backed up `/etc/resolv.conf` to `/root/resolv.conf.before-dns-fix` and replaced its contents with `nameserver 192.168.1.102` (runtime-only):
- `getent ahostsv4 deb.debian.org`: `146.75.2.132` PASS
- `curl -4 -I https://deb.debian.org/debian/`: **HTTP/2 200** PASS.
- Route had already been corrected to `default via 192.168.1.102 dev eth0`.
- `CT164_IPV4_EGRESS=PASS`, `CT164_DNS_RUNTIME=PASS`.
- `CT164_DHCP_PERSISTENCE=HOLD`: the currently running `dhclient` can overwrite route/resolv.conf on renew, and `.107` remains the source identifier.

**Recommended next guest-only step:** protect the existing `/etc/dhcp/dhclient.conf` with a root-only backup, ensure it has no conflicting active `supersede` directives, then append `supersede routers 192.168.1.102;` and `supersede domain-name-servers 192.168.1.102;`. ISC confirms these statements cause local options to take precedence: https://kb.isc.org/docs/isc-dhcp-44-manual-pages-dhclientconf. This retains DHCP for the IP address. A deliberate CT164-only reboot should verify persistence. **Do not `reject 192.168.1.107` yet:** the operator has not proved the legitimate `.102` DHCP server will issue leases; rejecting `.107` blindly could leave the CT without an address. Network security remains a concern until unintended DHCP responder is audited, but does not require holding private OS preparation.

Potential Proxmox interaction: PVE may also write `/etc/resolv.conf` on container start. A March 2026 PVE patch specifically addresses `dhclient` overwriting container DNS preferences; this specific CT uses guest ifupdown + dhclient. Verify effective resolver after renewal/reboot. https://lore.proxmox.com/pve-devel/20260311112057.48653-1-f.schauer%40proxmox.com/

**Still HOLD:** public exposure via CT152, router-origin attack-surface evaluation, firewall ACL, off-host recovery, thin pool overcommit, app exact-SHA deployment.

## 2026-11-10 — CT164 DHCP override applied; post-reboot route not yet proven

Operator executed guarded `/etc/dhcp/dhclient.conf` update with `/root/dhclient.conf.before-yangspace` backup:
```text
# YANGSPACE_CT164_NETWORK_V1
supersede routers 192.168.1.102;
supersede domain-name-servers 192.168.1.102;
```
The guest is still DHCP-addressed; no router/CT152/host edits. Operator rebooted only CT164 and verified after restart: `/etc/resolv.conf` contains `nameserver 192.168.1.102`, `getent ahostsv4 deb.debian.org` returned `146.75.2.132`, `systemctl is-system-running` returned `running`. However the `ip -4 route` command printed **no route lines in the supplied reboot output**. A DHCP assignment timing race is possible, but *not proven*. Do not mark post-reboot IP/gateway persistence PASS until an independent fresh `ip -4 addr`, `ip -4 route`, `ip -4 route get` and HTTPS check establishes default gateway .102 and working egress.

**Gate status:** DHCP override CONFIGURED; post-reboot systemd PASS; post-reboot DNS resolution PASS; post-reboot gateway/route UNVERIFIED; public/application release HOLD.

## 2026-11-10 — CT164 DHCP-route persistence and HTTPS verified: PASS

**Operator actual post-reboot evidence** (fresh inspection, no further mutation):
```text
pct status 164 = status: running
eth0@if51 UP 192.168.1.110/24
default via 192.168.1.102 dev eth0
1.1.1.1 via 192.168.1.102 dev eth0 src 192.168.1.110
/etc/resolv.conf: nameserver 192.168.1.102
curl -4 https://deb.debian.org/debian/: HTTPS=200
systemctl is-system-running: running
```

**Accepted:** `CT164_DHCP_OVERRIDE_PERSISTENCE=PASS`, `CT164_IP_ROUTE_DNS_EGRESS=PASS`, `CT164_SYSTEMD=PASS` after a single guest-only reboot. There is still no fixed IP reservation; address may change at a later DHCP lease. The DHCP server identifier `.107` remains unexplained. A local dhclient override corrects routers/DNS but does not prevent untrusted DHCP servers from assigning addresses, and no public endpoint or CT152 proxy configuration is authorized.

**Next permitted private guest preparation:** Microsoft APT repository for Debian 13, `aspnetcore-runtime-10.0` (runtime only, not SDK), verify `dotnet --list-runtimes`, then guarded service/deploy users and release/data/config/log directories. Do not enable internal Nginx listener before reviewing ingress security; do not deploy application from unreviewed branch. Official Debian guidance: https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian?tabs=dotnet10

**Remaining gates:** PVE firewall was `disabled/running`, independent off-host backup not proven, thin pool `Data%=83.77` with `VG VFree=0`; no app released, no activated service, G0/G-PREVIEW/G-PILOT remain HOLD.

## 2026-11-10 — ASP.NET Core 10 runtime installed on CT164: PASS

The operator installed Microsoft Debian 13 repo package `packages-microsoft-prod 1.1-debian13` and `apt-get install -y --no-install-recommends aspnetcore-runtime-10.0`. Observed:
```text
Microsoft.AspNetCore.App 10.0.15 [/usr/share/dotnet/shared/Microsoft.AspNetCore.App]
Microsoft.NETCore.App 10.0.15 [/usr/share/dotnet/shared/Microsoft.NETCore.App]
aspnetcore-runtime-10.0 10.0.15-1
dotnet-runtime-10.0 10.0.15-1
systemctl is-system-running: running
CT164 rootfs: 20G, 941M used, ~18G available (5%)
```

APT also installed `dotnet-host 11.0.0-1` (host/muxer only); the application shared runtime remains .NET 10. APT reported 57 available updates and locale warnings because `en_US.UTF-8` is not generated; neither caused runtime install failure. **Do not run unreviewed `dist-upgrade` or change the host locale based on this.**

**Status:** `CT164_ASPNETCORE_RUNTIME_10=PASS`, `CT164_SYSTEMD=PASS`, `CT164_NETWORK=PASS` from previous operator evidence. No app deployed. Next preparation: non-root `yangspace` runtime identity, separate `yangdeploy` deploy user, separate read-only release-reader group, owned release/data/log/config paths. Do not give deploy identity access to root:runtime secret files or runtime data.

**Release scope:** only private OS preparation authorized. PVE firewall was `disabled/running` and storage thin pool heavily allocated; off-host backups and effective gateway ACL remain unproven. No public deployment until independent review.

## 2026-11-10 — CT164 service users and directory skeleton: operator PASS

Operator executed two guarded container-only snippets. Their completion markers were both observed:
```text
uid=999(yangspace) gid=988(yangspace) groups=988(yangspace),991(yangspace-release)
uid=1000(yangdeploy) gid=1000(yangdeploy) groups=1000(yangdeploy),991(yangspace-release)
yangdeploy L 2026-11-10 0 99999 7 -1
CT164_SERVICE_IDENTITIES=PASS
CT164_DIRECTORY_LAYOUT=PASS
```

Operator requested layout:
- `/srv/yangspace`: root:`yangspace-release`, mode `0750`
- `/srv/yangspace/repo`: `yangdeploy:yangdeploy`, mode `0700`
- `/srv/yangspace/releases`: `yangdeploy:yangspace-release`, mode `2750`
- `/var/lib/yangspace` and `media`: `yangspace:yangspace`, mode `0700`
- `/etc/yangspace`: root:`yangspace`, mode `0750`
- `/var/log/yangspace`: root:root, mode `0755`; `api`: yangspace, `deploy/verify`: yangdeploy, `restore`: root, all `0700`.

The markers assert success but **exact directory modes and ownership were not individually shown in user logs**; accept identities and creation, with a later independent `stat` / access-negative test for permission enforcement. `yangdeploy` has a locked password and no authorized SSH key/sudo role shown. No app service or listener exists from this action.

**Next integration caution:** `infra/pve/yangspace-preview.service` uses `User=yangspace` and `EnvironmentFile=/etc/yangspace/preview.env`, but this env file and the immutable `/srv/yangspace/current/api` release **do not exist yet**. Do not enable/start that service or copy an active internal `nginx-preview.conf` blindly. Nginx example listens on port 80 with no gateway authentication; first establish actual edge ACL and source-file permissions. The deploy user must create release files group-readable by `yangspace-release` (e.g. controlled `umask 027`) or setgid directory alone will not ensure runtime access.

**Current gates:** CT164 base guest/network/runtime/user setup PASS at reported scope; guest permissions deep verification pending; application release, Nginx listener, Cloudflare origin TLS, firewall/ACL, off-host backup, PVE storage risk and G0/G-PREVIEW/G-PILOT remain HOLD.

## 2026-11-10 — segregated access negative tests PASS

The operator returned exact directory ownership/mode evidence:
```text
750 root:yangspace-release /srv/yangspace
700 yangdeploy:yangdeploy /srv/yangspace/repo
2750 yangdeploy:yangspace-release /srv/yangspace/releases
700 yangspace:yangspace /var/lib/yangspace
700 yangspace:yangspace /var/lib/yangspace/media
750 root:yangspace /etc/yangspace
700 yangspace:yangspace /var/log/yangspace/api
700 yangdeploy:yangdeploy /var/log/yangspace/deploy
700 yangdeploy:yangdeploy /var/log/yangspace/verify
700 root:root /var/log/yangspace/restore
CT164_ACCESS_ISOLATION=PASS
```

Actual negative checks confirmed runtime cannot traverse `/srv/yangspace/repo` or write `/srv/yangspace/releases`, deploy identity cannot traverse `/var/lib/yangspace` or `/etc/yangspace`, and positive role access matched design. This accepts `CT164_SERVICE_IDENTITY_AND_DIRECTORY_ACL=PASS` only; it does **not** prove file-level release contents, systemd sandboxing, backup, Nginx or public ACL.

The existing repo template `infra/pve/yangspace-preview.service` was amended to bind Kestrel explicitly to `http://127.0.0.1:5090` using `ASPNETCORE_URLS`. The template remains **not installed, not enabled and not started** based on supplied evidence. It still requires a reviewed `/srv/yangspace/current/api` release and protected `/etc/yangspace/preview.env`, neither of which is yet installed. Do not generate placeholder admin credentials or bring up the API before source QA and protected release approval.

**Next permissible preparation:** stage and lint the exact committed service template in CT164 **without enable/start**, then prepare the exact-SHA release QA requirements; nginx, edge/proxy, DB creation and public rollout are HOLD.
