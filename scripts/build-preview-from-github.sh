#!/usr/bin/env bash
# Build a pinned, reviewable release from GitHub. NO auto activation or restart.
set -Eeuo pipefail
umask 027
SHA="${1:-}"
SOURCE_REF="${2:-main}"
# BUILD ONLY on a trusted Linux/GNU host. On Git Bash/NTFS, stat/chmod
# cannot prove the Unix ACL boundary required for protected approval files.
# The separate synthetic policy regression remains runnable on Windows.
case "$(uname -s)" in
  Linux) ;;
  *) echo "RELEASE_BLOCKED: Linux runtime required for protected approval enforcement" >&2; exit 3 ;;
esac
# Build-only operation from allowlisted refs. No GitHub Actions.
case "$SOURCE_REF" in
  main|feat/yangspace-v2-foundation-20261108) ;;
  *) echo "RELEASE_BLOCKED: unapproved source ref" >&2; exit 3 ;;
esac
REPO="${YANGSPACE_REPO:-/srv/yangspace/repo}"
RELEASES="${YANGSPACE_RELEASES:-/srv/yangspace/releases}"

if [[ ! "$SHA" =~ ^[0-9a-f]{40}$ ]]; then
  echo "Usage: $0 <full 40-hex reviewed commit SHA>" >&2; exit 2
fi
command -v git >/dev/null && command -v dotnet >/dev/null && command -v node >/dev/null && command -v npm >/dev/null
[[ -d "$REPO/.git" ]] || { echo "Not a Git clone: $REPO" >&2; exit 2; }
node -e 'const [major,minor,patch]=process.versions.node.split(".").map(Number);const supported=(major===22&&(minor>22||(minor===22&&patch>=3)))||(major===24&&minor>=15)||(major>=26);if(!supported)process.exit(1)' || { echo "Node runtime does not meet Angular 22 requirement" >&2; exit 2; }
# Lock the remote to the project repository; allow HTTPS and canonical SSH forms.
ORIGIN_URL="$(git -C "$REPO" remote get-url origin)"
case "$ORIGIN_URL" in
  https://github.com/jvalkovv/YangSpaceApp|https://github.com/jvalkovv/YangSpaceApp.git|git@github.com:jvalkovv/YangSpaceApp|git@github.com:jvalkovv/YangSpaceApp.git|ssh://git@github.com/jvalkovv/YangSpaceApp.git) ;;
  *) echo "RELEASE_BLOCKED: unapproved Git remote origin" >&2; exit 3 ;;
esac
git -C "$REPO" fetch --no-tags origin "refs/heads/$SOURCE_REF:refs/remotes/origin/$SOURCE_REF"
git -C "$REPO" cat-file -e "$SHA^{commit}"
TIP="$(git -C "$REPO" rev-parse "refs/remotes/origin/$SOURCE_REF")"
[[ "$TIP" == "$SHA" ]] || {
  echo "RELEASE_BLOCKED: SHA is not exact remote ref tip ($SOURCE_REF)" >&2; exit 3;
}
# The script that runs and its evidence validator must be byte-for-byte
# identical to the approved commit, not locally edited or from another branch.
SCRIPT_PATH="$(realpath -e -- "${BASH_SOURCE[0]}")"
[[ "$SCRIPT_PATH" == "$(realpath -e -- "$REPO/scripts/build-preview-from-github.sh")" ]] || {
  echo "RELEASE_BLOCKED: build script is not the reviewed repository script" >&2; exit 3;
}
for REL in scripts/build-preview-from-github.sh scripts/verify-preview-release-evidence.sh; do
  EXPECTED="$(git -C "$REPO" show "$SHA:$REL" | sha256sum | cut -d' ' -f1)"
  ACTUAL="$(sha256sum "$REPO/$REL" | cut -d' ' -f1)"
  [[ "$EXPECTED" == "$ACTUAL" ]] || {
    echo "RELEASE_BLOCKED: executing unreviewed release gate ($REL)" >&2; exit 3;
  }
done
# External evidence + reviewer approval are required before any build command.
bash "$REPO/scripts/verify-preview-release-evidence.sh" "$SHA" "$SOURCE_REF"

mkdir -p "$RELEASES"
# No two operators may build/publish a candidate concurrently.
command -v flock >/dev/null || { echo "RELEASE_BLOCKED: flock unavailable" >&2; exit 3; }
exec 9>"$RELEASES/.build.lock"
flock -n 9 || { echo "RELEASE_BLOCKED: another candidate build holds the lock" >&2; exit 3; }
TARGET="$RELEASES/$SHA"
[[ ! -e "$TARGET" ]] || { echo "Release already exists: $TARGET" >&2; exit 4; }
STAGE="$(mktemp -d "$RELEASES/.build.XXXXXXXX")"
WORKTREE_ADDED=0
cleanup() {
  if [[ $WORKTREE_ADDED -eq 1 ]]; then git -C "$REPO" worktree remove --force "$STAGE/source" >/dev/null 2>&1 || true; fi
  rm -rf "$STAGE"
}
trap cleanup EXIT
git -C "$REPO" worktree add --detach "$STAGE/source" "$SHA"
WORKTREE_ADDED=1
mkdir -p "$STAGE/release/api" "$STAGE/release/web"
dotnet publish "$STAGE/source/apps/api/YangSpace.Api.csproj" -c Release -o "$STAGE/release/api"
(
  cd "$STAGE/source/apps/web"
  # Fail closed: release artifacts require a reviewed, source-tracked lockfile.
  [[ -s package-lock.json ]] || {
    echo "RELEASE_BLOCKED: apps/web/package-lock.json missing; run S0-02 and commit reviewed file." >&2
    exit 10
  }
  git -C "$STAGE/source" ls-files --error-unmatch apps/web/package-lock.json >/dev/null || {
    echo "RELEASE_BLOCKED: npm lockfile exists but is not tracked by the reviewed commit." >&2
    exit 10
  }
  node scripts/verify-lock.mjs
  npm ci --no-audit --no-fund
  npm run typecheck
  npm run build
)
cp -a "$STAGE/source/apps/web/dist/yangspace-web/browser" "$STAGE/release/web/browser"
test -s "$STAGE/release/api/YangSpace.Api.dll"
test -s "$STAGE/release/web/browser/index.html"
printf '%s\n' "$SHA" > "$STAGE/release/commit.sha"
# Traceability for this immutable, build-only artifact: no user credentials.
printf 'COMMIT_SHA=%s\nSOURCE_REF=%s\nSOURCE_QA_SHA256=%s\nAPI_QA_SHA256=%s\nAPPROVAL_FILE_SHA256=%s\nAPPROVAL_SCOPE=BUILD_ONLY\n' \
  "$SHA" "$SOURCE_REF" \
  "$(sha256sum "$YANGSPACE_SOURCE_QA_MANIFEST" | cut -d' ' -f1)" \
  "$(sha256sum "$YANGSPACE_API_QA_MANIFEST" | cut -d' ' -f1)" \
  "$(sha256sum "$YANGSPACE_RELEASE_APPROVAL_FILE" | cut -d' ' -f1)" \
  > "$STAGE/release/release-evidence.txt"
(
  cd "$STAGE/release"
  find api web -type f -print0 | sort -z | xargs -0 sha256sum > files.sha256
)
# Repeat the approval check just before publishing the immutable build dir.
bash "$REPO/scripts/verify-preview-release-evidence.sh" "$SHA" "$SOURCE_REF"
mv "$STAGE/release" "$TARGET"
echo "BUILT=$TARGET"
echo "ACTIVATED=NO"
echo "NEXT=Operator approves backup, access, release and verify separately. No auto activation."
