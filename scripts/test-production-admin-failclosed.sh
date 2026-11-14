#!/usr/bin/env bash
# Negative Production startup contract for ONE approved YangSpace CT.
# Never touches the active DB, listener or deployed systemd service.
# Usage: bash scripts/test-production-admin-failclosed.sh /abs/path/YangSpace.Api.dll
set -Eeuo pipefail
umask 077

DLL="${1:-}"
[[ "$DLL" = /* && -f "$DLL" && -r "$DLL" ]] ||
  { echo 'PROD_GATE_TEST=HOLD: absolute built API DLL required' >&2; exit 2; }
DOTNET="$(command -v dotnet || true)"
[[ -n "$DOTNET" ]] || { echo 'PROD_GATE_TEST=HOLD: dotnet runtime unavailable' >&2; exit 2; }

TMP="$(mktemp -d -t yangspace-prod-guard-XXXXXXXX)"
trap 'rm -rf -- "$TMP"' EXIT
DB="Data Source=$TMP/nonexistent.db;Mode=ReadWrite"
SECRET="$(head -c 32 /dev/urandom | base64 -w0)"
[[ "${#SECRET}" == 44 ]] || { echo 'PROD_GATE_TEST=HOLD: test random secret unavailable' >&2; exit 2; }

expect_denial() {
  local name="$1" required="$2" secret="$3" preview="$4"
  local exit_code=0
  local env_args=(
    "PATH=$PATH" "HOME=$TMP" "DOTNET_CLI_HOME=$TMP"
    "ASPNETCORE_ENVIRONMENT=Production"
    "ASPNETCORE_URLS=http://127.0.0.1:0"
    "Preview__Enabled=$preview"
    "Preview__AllowRegistration=false"
    "Admin__Enabled=true"
    "AllowedHosts=yangspace.gndstore.eu"
    "ConnectionStrings__Main=$DB"
  )
  [[ -z "$secret" ]] || env_args+=("Admin__GatewaySecret=$secret")
  timeout 20s env -i "${env_args[@]}" "$DOTNET" "$DLL" >"$TMP/$name.log" 2>&1 || exit_code="$?"
  if [[ "$exit_code" == 0 || "$exit_code" == 124 ]]; then
    echo "PROD_GATE_TEST=HOLD: $name unexpectedly succeeded or timed out" >&2
    exit 1
  fi
  if ! grep -Fq -- "$required" "$TMP/$name.log"; then
    echo "PROD_GATE_TEST=HOLD: $name failed for unexpected reason" >&2
    exit 1
  fi
  printf 'PROD_GATE_CASE_PASS=%s\n' "$name"
}

expect_denial no_secret \
  'Production CMS requires a protected gateway secret' '' false
expect_denial malformed_secret \
  'Production CMS gateway secret must be valid Base64' 'not-valid-###' false
expect_denial no_database \
  'Production SQLite file is missing' "$SECRET" false
expect_denial preview_cannot_be_production_admin \
  'Production CMS requires Production environment and Preview:Enabled=false' "$SECRET" true

[[ ! -e "$TMP/nonexistent.db" ]] ||
  { echo 'PROD_GATE_TEST=HOLD: test created a database unexpectedly' >&2; exit 1; }
printf 'PRODUCTION_ADMIN_STARTUP_FAIL_CLOSED=PASS\n'
