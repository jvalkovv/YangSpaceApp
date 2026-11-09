#!/usr/bin/env bash
# Non-destructive S0-06 evidence-gate contract test.
# Uses synthetic TEMP fixtures, not operator QA evidence. NO PVE, fetch or build.
set -Eeuo pipefail
umask 077
REPO="$(git -C "$(dirname "${BASH_SOURCE[0]}")/.." rev-parse --show-toplevel)"
HELPER="$REPO/scripts/verify-preview-release-evidence.sh"
SHA="$(git -C "$REPO" rev-parse HEAD)"
REF=feat/yangspace-v2-foundation-20261108
TMP="$(mktemp -d)"
trap 'rm -rf -- "$TMP"' EXIT
export YANGSPACE_REPO="$REPO"
export YANGSPACE_SOURCE_QA_MANIFEST="$TMP/source.manifest"
export YANGSPACE_API_QA_MANIFEST="$TMP/api.manifest"
export YANGSPACE_RELEASE_APPROVAL_FILE="$TMP/approval.txt"

write_fixtures() {
  cat > "$YANGSPACE_SOURCE_QA_MANIFEST" <<EOF
COMMIT_SHA=$SHA
SOURCE_VALIDATION=PASS
QA_KIND=MANUAL_LOCAL_CLEAN_SNAPSHOT
SRC=GIT_ARCHIVE
WEB_LOCK_NPM_CI_AUDIT_TYPECHECK_BUILD=PASS
WEB_BROWSER=PASS
DOTNET_RESTORE_BUILD=PASS
PVE_DEPLOYMENT=NONE
EOF
  cat > "$YANGSPACE_API_QA_MANIFEST" <<EOF
COMMIT_SHA=$SHA
REAL_API_INTEGRATION=PASS
DB_SCOPE=DISPOSABLE_OS_TEMP_SQLITE
AUTH_AND_LIFECYCLE=PASS
PROCESS_RESTART_PERSISTENCE=PASS
BOOTSTRAP_PRIVILEGE_ESCALATION_DENIED=PASS
BOOTSTRAP_NONADMIN_ROLE_RETAINED=PASS
PVE_DEPLOYMENT=NONE
EOF
  cat > "$YANGSPACE_RELEASE_APPROVAL_FILE" <<EOF
COMMIT_SHA=$SHA
SOURCE_REF=$REF
SOURCE_QA_SHA256=$(sha256sum "$YANGSPACE_SOURCE_QA_MANIFEST" | cut -d' ' -f1)
API_QA_SHA256=$(sha256sum "$YANGSPACE_API_QA_MANIFEST" | cut -d' ' -f1)
APPROVAL=BUILD_ONLY
REVIEWER_ID=operator-test
EOF
  chmod 600 "$YANGSPACE_RELEASE_APPROVAL_FILE"
}
expect_block() {
  local test_name="$1" expected_reason="$2"
  if bash "$HELPER" "$SHA" "$REF" > "$TMP/result.txt" 2>&1; then
    printf 'POLICY_TEST_FAIL=%s unexpectedly accepted\n' "$test_name" >&2
    exit 1
  fi
  if ! grep -Fq -- "RELEASE_BLOCKED: $expected_reason" "$TMP/result.txt"; then
    printf 'POLICY_TEST_FAIL=%s rejected for the wrong reason\n' "$test_name" >&2
    cat "$TMP/result.txt" >&2
    exit 1
  fi
  printf 'POLICY_CASE_PASS=%s\n' "$test_name"
}
write_fixtures
bash "$HELPER" "$SHA" "$REF" > "$TMP/result.txt"
grep -Fqx 'RELEASE_EVIDENCE=PASS' "$TMP/result.txt"
printf 'POLICY_CASE_PASS=valid_reviewed_fixture\n'

# Windows exports manifests with CRLF. Verify this remains an exact-match PASS.
write_fixtures
sed 's/$/\r/' "$YANGSPACE_SOURCE_QA_MANIFEST" > "$TMP/source.crlf"
mv "$TMP/source.crlf" "$YANGSPACE_SOURCE_QA_MANIFEST"
new_source_hash="$(sha256sum "$YANGSPACE_SOURCE_QA_MANIFEST" | cut -d' ' -f1)"
sed -i "s/^SOURCE_QA_SHA256=.*/SOURCE_QA_SHA256=$new_source_hash/" "$YANGSPACE_RELEASE_APPROVAL_FILE"
bash "$HELPER" "$SHA" "$REF" > "$TMP/result.txt"
grep -Fqx 'RELEASE_EVIDENCE=PASS' "$TMP/result.txt"
printf 'POLICY_CASE_PASS=windows_crlf_source_manifest\n'

# Keep CRLF byte fidelity on BOTH QA manifests and on the approval record;
# the reviewer must approve the SHA-256 of the actual original bytes.
write_fixtures
for file in "$YANGSPACE_SOURCE_QA_MANIFEST" "$YANGSPACE_API_QA_MANIFEST"; do
  sed 's/$/\r/' "$file" > "$TMP/converted.crlf"
  mv "$TMP/converted.crlf" "$file"
done
source_digest="$(sha256sum "$YANGSPACE_SOURCE_QA_MANIFEST" | cut -d' ' -f1)"
api_digest="$(sha256sum "$YANGSPACE_API_QA_MANIFEST" | cut -d' ' -f1)"
sed -i -e "s/^SOURCE_QA_SHA256=.*/SOURCE_QA_SHA256=$source_digest/" \
       -e "s/^API_QA_SHA256=.*/API_QA_SHA256=$api_digest/" \
       "$YANGSPACE_RELEASE_APPROVAL_FILE"
sed 's/$/\r/' "$YANGSPACE_RELEASE_APPROVAL_FILE" > "$TMP/approval.crlf"
mv "$TMP/approval.crlf" "$YANGSPACE_RELEASE_APPROVAL_FILE"
chmod 600 "$YANGSPACE_RELEASE_APPROVAL_FILE"
bash "$HELPER" "$SHA" "$REF" > "$TMP/result.txt"
grep -Fqx 'RELEASE_EVIDENCE=PASS' "$TMP/result.txt"
printf 'POLICY_CASE_PASS=windows_crlf_all_manifests\n'

# Same key repeated must be rejected even when both copies have exact value.
write_fixtures
printf 'COMMIT_SHA=%s\n' "$SHA" >> "$YANGSPACE_SOURCE_QA_MANIFEST"
expect_block duplicated_sha_key 'expected unique COMMIT_SHA value'

# An expected line with extra characters must not be normalized or accepted.
write_fixtures
sed -i 's/SOURCE_VALIDATION=PASS/SOURCE_VALIDATION=PASS #unexpected/' \
    "$YANGSPACE_SOURCE_QA_MANIFEST"
expect_block unexpected_line_suffix 'expected unique SOURCE_VALIDATION value'


write_fixtures
sed -i 's/WEB_BROWSER=PASS/WEB_BROWSER=SKIPPED/' "$YANGSPACE_SOURCE_QA_MANIFEST"
expect_block skipped_browser 'expected unique WEB_BROWSER value'

write_fixtures
sed -i 's/BOOTSTRAP_NONADMIN_ROLE_RETAINED=PASS/BOOTSTRAP_NONADMIN_ROLE_RETAINED=HOLD/' "$YANGSPACE_API_QA_MANIFEST"
expect_block unverified_bootstrap 'expected unique BOOTSTRAP_NONADMIN_ROLE_RETAINED value'

write_fixtures
sed -i 's/REAL_API_INTEGRATION=PASS/REAL_API_INTEGRATION=HOLD/' "$YANGSPACE_API_QA_MANIFEST"
expect_block failed_api 'expected unique REAL_API_INTEGRATION value'

write_fixtures
sed -i 's/APPROVAL=BUILD_ONLY/APPROVAL=GO_LIVE/' "$YANGSPACE_RELEASE_APPROVAL_FILE"
expect_block deployment_scope_mismatch 'expected unique APPROVAL value'

write_fixtures
printf 'PVE_DEPLOYMENT=NONE\n' >> "$YANGSPACE_API_QA_MANIFEST"
expect_block duplicated_evidence_key 'expected unique PVE_DEPLOYMENT value'

write_fixtures
sed -i 's/SOURCE_REF=feat\/yangspace-v2-foundation-20261108/SOURCE_REF=main/' "$YANGSPACE_RELEASE_APPROVAL_FILE"
expect_block wrong_approved_ref 'expected unique SOURCE_REF value'

write_fixtures
chmod 666 "$YANGSPACE_RELEASE_APPROVAL_FILE"
expect_block mutable_approval_file 'approval file must not be group/world writable'

write_fixtures
export YANGSPACE_RELEASE_APPROVAL_FILE=''
expect_block missing_approval 'APPROVAL: absolute readable regular file'

export YANGSPACE_RELEASE_APPROVAL_FILE="$TMP/approval.txt"
write_fixtures
export YANGSPACE_SOURCE_QA_MANIFEST="$REPO/.git/config"
expect_block evidence_inside_checkout 'SOURCE: evidence inside Git checkout is untrusted'

printf 'PREVIEW_RELEASE_POLICY_TEST=PASS\n'
