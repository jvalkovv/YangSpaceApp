#!/usr/bin/env bash
# Run after the API is listening on http://127.0.0.1:5090 in Preview mode.
set -Eeuo pipefail
BASE="${YANGSPACE_BASE:-http://127.0.0.1:5090}"
curl -fsS "$BASE/health/live" >/dev/null
curl -fsS "$BASE/health/ready" >/dev/null
curl -fsS "$BASE/api/v1/homepage" | python3 -c 'import json,sys;x=json.load(sys.stdin);assert x["directory"]["total"]>=6;assert len(x["directory"]["items"])>=6;assert len(x["meta"]["neighborhoods"])>=1;assert len(x["offers"])>=1;assert len(x["brochures"])>=1;assert len(x["jobs"])>=1'
curl -fsS "$BASE/api/v1/meta" | python3 -c 'import json,sys;x=json.load(sys.stdin);assert len(x["neighborhoods"])>0'
curl -fsS "$BASE/api/v1/locations" | python3 -c 'import json,sys;x=json.load(sys.stdin);assert x["total"]>=6;assert len(x["items"])>=6'
curl -fsS "$BASE/api/v1/locations/pekarna-demo" | python3 -c 'import json,sys;x=json.load(sys.stdin);assert x["slug"]=="pekarna-demo";assert len(x["offers"])>=1'
ADMIN_STATUS="$(curl -s -o /dev/null -w '%{http_code}' "$BASE/api/v1/admin/locations")"
[[ "$ADMIN_STATUS" == "401" || "$ADMIN_STATUS" == "403" ]] || { echo "Admin endpoint exposed: HTTP $ADMIN_STATUS" >&2; exit 1; }
echo "PASS: live/ready, homepage highlights, metadata, listing, detail, admin guest protection"
