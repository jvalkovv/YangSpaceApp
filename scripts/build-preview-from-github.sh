#!/usr/bin/env bash
# Build a pinned, reviewable release from GitHub. NO auto activation or restart.
set -Eeuo pipefail
umask 027
SHA="${1:-}"
REPO="${YANGSPACE_REPO:-/srv/yangspace/repo}"
RELEASES="${YANGSPACE_RELEASES:-/srv/yangspace/releases}"

if [[ ! "$SHA" =~ ^[0-9a-f]{40}$ ]]; then
  echo "Usage: $0 <full 40-hex reviewed commit SHA>" >&2; exit 2
fi
command -v git >/dev/null && command -v dotnet >/dev/null && command -v node >/dev/null && command -v npm >/dev/null
[[ -d "$REPO/.git" ]] || { echo "Not a Git clone: $REPO" >&2; exit 2; }
node -e 'const [major,minor,patch]=process.versions.node.split(".").map(Number);const supported=(major===22&&(minor>22||(minor===22&&patch>=3)))||(major===24&&minor>=15)||(major>=26);if(!supported)process.exit(1)' || { echo "Node runtime does not meet Angular 22 requirement" >&2; exit 2; }
git -C "$REPO" fetch --prune origin main
git -C "$REPO" cat-file -e "$SHA^{commit}"
git -C "$REPO" merge-base --is-ancestor "$SHA" origin/main || { echo "SHA not on origin/main" >&2; exit 3; }
mkdir -p "$RELEASES"
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
mv "$STAGE/release" "$TARGET"
echo "BUILT=$TARGET"
echo "ACTIVATED=NO"
echo "Next: verify backup + logs; operator manually switches current symlink and restarts systemd."
