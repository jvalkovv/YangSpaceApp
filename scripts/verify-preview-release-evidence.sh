#!/usr/bin/env bash
# S0-06 private-preview BUILD-ONLY approval gate. Linux/GNU userspace.
# Must be called only by the approved, pinned build script; no activation.
set -Eeuo pipefail
umask 077

die() { printf 'RELEASE_BLOCKED: %s\n' "$*" >&2; exit 3; }
SHA="${1:-}"
SOURCE_REF="${2:-}"
[[ "$SHA" =~ ^[0-9a-f]{40}$ ]] || die 'full 40-hex commit SHA required'
case "$SOURCE_REF" in
  main|feat/yangspace-v2-foundation-20261108) ;;
  *) die 'source ref not allowlisted' ;;
esac

REPO="${YANGSPACE_REPO:-/srv/yangspace/repo}"
[[ -d "$REPO/.git" ]] || die 'trusted local Git checkout missing'
REPO="$(realpath -e -- "$REPO")"

SOURCE="${YANGSPACE_SOURCE_QA_MANIFEST:-}"
API="${YANGSPACE_API_QA_MANIFEST:-}"
APPROVAL="${YANGSPACE_RELEASE_APPROVAL_FILE:-}"
for name in SOURCE API APPROVAL; do
  path="${!name}"
  [[ -n "$path" && "$path" = /* && -f "$path" && -r "$path" && ! -L "$path" ]] ||
    die "$name: absolute readable regular file (not symlink) required"
  path="$(realpath -e -- "$path")" || die "$name: unresolved file"
  case "$path" in "$REPO"/*) die "$name: evidence inside Git checkout is untrusted" ;; esac
  printf -v "$name" '%s' "$path"
done
[[ "$SOURCE" != "$API" && "$SOURCE" != "$APPROVAL" && "$API" != "$APPROVAL" ]] ||
  die 'evidence paths must be distinct'

# Approval is a protected operator-owned file, not a Git-tracked build artifact.
# File ownership/ACLs and manifest hashes bind what was reviewed, but this is
# NOT a cryptographic signature of execution or an independent trusted CI attestation.
mode="$(stat -c '%a' -- "$APPROVAL")"
[[ "$mode" =~ ^[0-7]{3,4}$ ]] || die 'invalid approval file permissions'
(( (8#$mode & 022) == 0 )) || die 'approval file must not be group/world writable'
owner="$(stat -c '%u' -- "$APPROVAL")"
[[ "$owner" == "$(id -u)" || "$owner" == 0 ]] ||
  die 'approval file must be owned by the operator or root'

require_line() {
  local path="$1" expected="$2" key="${2%%=*}" matched matched_crlf total
  # Windows manifests may have CRLF; compare exact content for both line
  # endings, reject duplicate keys, preserve raw bytes for SHA-256 binding.
  matched="$(grep -Fxc -- "$expected" "$path" || true)"
  matched_crlf="$(grep -Fxc -- "$expected"$'\r' "$path" || true)"
  total="$(grep -c -- "^${key}=" "$path" || true)"
  [[ "$total" == 1 ]] && (( matched + matched_crlf == 1 )) ||
    die "expected unique $key value in ${path##*/}"
}
for manifest in "$SOURCE" "$API"; do
  require_line "$manifest" "COMMIT_SHA=$SHA"
  require_line "$manifest" "PVE_DEPLOYMENT=NONE"
done
require_line "$SOURCE" 'SOURCE_VALIDATION=PASS'
require_line "$SOURCE" 'QA_KIND=MANUAL_LOCAL_CLEAN_SNAPSHOT'
require_line "$SOURCE" 'SRC=GIT_ARCHIVE'
require_line "$SOURCE" 'WEB_LOCK_NPM_CI_AUDIT_TYPECHECK_BUILD=PASS'
require_line "$SOURCE" 'WEB_BROWSER=PASS'
require_line "$SOURCE" 'DOTNET_RESTORE_BUILD=PASS'
require_line "$API" 'REAL_API_INTEGRATION=PASS'
require_line "$API" 'DB_SCOPE=DISPOSABLE_OS_TEMP_SQLITE'
require_line "$API" 'AUTH_AND_LIFECYCLE=PASS'
require_line "$API" 'PROCESS_RESTART_PERSISTENCE=PASS'
require_line "$API" 'BOOTSTRAP_PRIVILEGE_ESCALATION_DENIED=PASS'
require_line "$API" 'BOOTSTRAP_NONADMIN_ROLE_RETAINED=PASS'

source_hash="$(sha256sum -- "$SOURCE" | cut -d' ' -f1)"
api_hash="$(sha256sum -- "$API" | cut -d' ' -f1)"
require_line "$APPROVAL" "COMMIT_SHA=$SHA"
require_line "$APPROVAL" "SOURCE_REF=$SOURCE_REF"
require_line "$APPROVAL" "SOURCE_QA_SHA256=$source_hash"
require_line "$APPROVAL" "API_QA_SHA256=$api_hash"
require_line "$APPROVAL" 'APPROVAL=BUILD_ONLY'
reviewer="$(sed -n 's/^REVIEWER_ID=//p' "$APPROVAL")"
[[ "$reviewer" =~ ^[a-zA-Z0-9._@-]{3,80}$ ]] ||
  die 'unique nonempty REVIEWER_ID required in protected approval'

printf 'RELEASE_EVIDENCE=PASS\n'
printf 'SOURCE_QA_SHA256=%s\nAPI_QA_SHA256=%s\n' "$source_hash" "$api_hash"
printf 'APPROVAL_SCOPE=BUILD_ONLY\n'
