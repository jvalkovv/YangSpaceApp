#!/usr/bin/env python3
"""Manual API integration test on an isolated OS-temp sandbox and loopback API.

Requires explicit disposable sandbox marker, matching DB, Staging-only mode,
random bootstrap admin and a loopback-only URL. No third-party dependencies.
Never run against a live or merchant database.
"""
from __future__ import annotations

import datetime as dt
import json
import os
import pathlib
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import uuid

BASE = os.environ.get("YANGSPACE_TEST_BASE_URL", "http://127.0.0.1:5090")
UTC = dt.timezone.utc


class ContractFailure(RuntimeError):
    pass


def require_isolated_ci() -> pathlib.Path:
    """Fail closed unless the target is a disposable test sandbox in OS TEMP."""
    db_raw = os.environ.get("YANGSPACE_TEST_DB_PATH", "")
    state_raw = os.environ.get("YANGSPACE_TEST_STATE", "")
    sandbox_raw = os.environ.get("YANGSPACE_TEST_SANDBOX", "")
    if not db_raw or not state_raw or not sandbox_raw:
        raise ContractFailure("REFUSED: missing isolated DB/state/sandbox paths")
    db = pathlib.Path(db_raw)
    state = pathlib.Path(state_raw)
    sandbox = pathlib.Path(sandbox_raw)
    parsed = urllib.parse.urlsplit(BASE)
    if (
        parsed.scheme != "http"
        or parsed.hostname != "127.0.0.1"
        or parsed.username is not None
        or parsed.password is not None
        or parsed.path not in ("", "/")
        or parsed.query or parsed.fragment
        or parsed.port is None or parsed.port <= 1024 or parsed.port > 65535
        or os.environ.get("YANGSPACE_TEST_ISOLATED_DB") != "YES"
        or os.environ.get("ASPNETCORE_ENVIRONMENT") != "Staging"
        or os.environ.get("Preview__Enabled", "").lower() != "true"
        or not sandbox.is_dir()
        or not sandbox.name.startswith("yangspace-ci-")
        or sandbox.resolve().parent != pathlib.Path(tempfile.gettempdir()).resolve()
        or not (sandbox / ".yangspace-disposable-test").is_file()
        or (sandbox / ".yangspace-disposable-test").read_text(encoding="utf-8") != "YES\n"
        or not db.is_absolute()
        or db.name != "preview.db"
        or db.resolve().parent != sandbox.resolve()
        or not state.is_absolute()
        or state.name != "state.json"
        or state.resolve().parent != sandbox.resolve()
        or os.environ.get("ConnectionStrings__Main") != f"Data Source={db}"
        or not os.environ.get("Bootstrap__AdminEmail")
        or not os.environ.get("Bootstrap__AdminPassword")
    ):
        raise ContractFailure("REFUSED: isolated Staging/loopback API test guard failed")
    return state

def request(method: str, path: str, payload: object | None = None, bearer: str | None = None):
    headers = {"Accept": "application/json"}
    if bearer:
        headers["Authorization"] = "Bearer " + bearer
    data = None
    if payload is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=12) as response:
            raw = response.read()
            return response.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as error:
        raw = error.read()
        try:
            body = json.loads(raw) if raw else None
        except ValueError:
            body = None
        return error.code, body


def must(method, path, expected, payload=None, bearer=None):
    status, body = request(method, path, payload, bearer)
    ok = expected if isinstance(expected, (tuple, list, set)) else (expected,)
    if status not in ok:
        raise ContractFailure(f"{method} {path}: expected {ok}; got HTTP {status}")
    return body


def utc(days: int) -> str:
    return (dt.datetime.now(UTC) + dt.timedelta(days=days)).isoformat().replace("+00:00", "Z")


def admin_token() -> str:
    body = must("POST", "/api/auth/login", 200, {
        "email": os.environ["Bootstrap__AdminEmail"],
        "password": os.environ["Bootstrap__AdminPassword"],
    })
    token = (body or {}).get("accessToken")
    if not isinstance(token, str) or len(token) < 30:
        raise ContractFailure("Admin token missing")
    return token


def prepare(state: pathlib.Path) -> None:
    slug = "ci-integr-" + uuid.uuid4().hex[:16]
    location = {
        "slug": slug, "name": "CI test location", "neighborhood": "CI-Kvartal",
        "category": "Услуги", "address": "Fictional CI address",
        "description": "Integration test — no real data", "phone": None,
        "website": None, "imageUrl": None, "openingHours": "09:00–18:00",
        "isPublished": False,
    }

    # Unauthenticated operations must not create data, or leak admin lists.
    must("GET", "/api/v1/admin/locations", (401, 403))
    must("POST", "/api/v1/admin/locations", (401, 403), location)
    must("GET", f"/api/v1/locations/{slug}", 404)

    token = admin_token()
    must("GET", "/api/v1/admin/me", 200, bearer=token)

    # Registration in isolated preview must NOT grant privileged access.
    # Public production release must disable/gate this preview registration.
    nonadmin = "ci-reader-" + uuid.uuid4().hex[:12] + "@example.invalid"
    must("POST", "/api/auth/register", 200, {
        "email": nonadmin, "password": os.environ["Bootstrap__AdminPassword"]
    })
    member = must("POST", "/api/auth/login", 200, {
        "email": nonadmin, "password": os.environ["Bootstrap__AdminPassword"]
    })
    member_token = (member or {}).get("accessToken")
    if not isinstance(member_token, str):
        raise ContractFailure("Non-admin login token missing")
    must("GET", "/api/v1/admin/locations", 403, bearer=member_token)
    must("POST", "/api/v1/admin/locations", 403, location, member_token)
    created = must("POST", "/api/v1/admin/locations", 201, location, token)
    uid = (created or {}).get("id")
    if not isinstance(uid, str):
        raise ContractFailure("Created location ID missing")
    must("GET", f"/api/v1/locations/{slug}", 404)
    must("POST", "/api/v1/admin/locations", 409, location, token)

    invalid = dict(location)
    invalid["slug"] = slug + "-invalid"
    invalid["name"] = ""
    must("POST", "/api/v1/admin/locations", 400, invalid, token)

    location["name"] = "CI test location, published"
    location["isPublished"] = True
    must("PUT", f"/api/v1/admin/locations/{uid}", 200, location, token)
    item = must("GET", f"/api/v1/locations/{slug}", 200)
    if item.get("name") != location["name"]:
        raise ContractFailure("Updated name missing from public detail")

    offer_path = f"/api/v1/admin/locations/{uid}/offers"
    must("POST", offer_path, 201, {
        "title": "Active CI offer", "description": "Test only", "startsAtUtc": utc(-1),
        "endsAtUtc": utc(7), "isPublished": True
    }, token)
    must("POST", offer_path, 201, {
        "title": "Expired CI offer", "description": "Test only", "startsAtUtc": utc(-10),
        "endsAtUtc": utc(-1), "isPublished": True
    }, token)
    must("POST", offer_path, 201, {
        "title": "Future CI offer", "description": "Test only", "startsAtUtc": utc(1),
        "endsAtUtc": utc(9), "isPublished": True
    }, token)
    must("POST", f"/api/v1/admin/locations/{uid}/brochures", 201, {
        "title": "CI brochure", "url": "https://example.org/ci-catalogue.pdf",
        "validFromUtc": utc(-1), "validToUtc": utc(4), "isPublished": True
    }, token)
    must("POST", f"/api/v1/admin/locations/{uid}/jobs", 201, {
        "title": "CI temporary job", "description": "Test only",
        "applyUrl": "https://example.org/ci-job", "expiresAtUtc": utc(7),
        "isPublished": True
    }, token)
    item = must("GET", f"/api/v1/locations/{slug}", 200)
    titles = {o["title"] for o in item.get("offers", [])}
    if titles != {"Active CI offer"} or len(item.get("brochures", [])) != 1 or len(item.get("jobs", [])) != 1:
        raise ContractFailure(f"Published/validity lifecycle incorrect: {titles}")

    must("POST", f"/api/v1/admin/locations/{uid}/hide", 204, {}, token)
    must("GET", f"/api/v1/locations/{slug}", 404)
    listing = must("GET", "/api/v1/locations?category=" + urllib.parse.quote(location["category"]), 200)
    if any(obj["slug"] == slug for obj in listing["items"]):
        raise ContractFailure("Hidden location leaked in public search")
    state.write_text(json.dumps({"id": uid, "slug": slug}), encoding="utf-8")
    print("INTEGRATION_PREPARE=PASS (guest denial, CRUD, expiry, hide)")


def verify(state: pathlib.Path) -> None:
    if not state.is_file():
        raise ContractFailure("Test state missing: no prepare phase")
    record = json.loads(state.read_text(encoding="utf-8"))
    token = admin_token()
    records = must("GET", "/api/v1/admin/locations", 200, bearer=token)
    if not any(str(item["id"]).lower() == record["id"].lower() and item["isPublished"] is False for item in records):
        raise ContractFailure("Hidden record missing after API process restart")
    must("GET", f"/api/v1/locations/{record['slug']}", 404)
    must("GET", "/api/v1/admin/locations", (401, 403))
    print("INTEGRATION_RESTART_PERSISTENCE=PASS")


def main() -> int:
    if len(sys.argv) != 2 or sys.argv[1] not in ("prepare", "verify"):
        print("Usage: api-integration.py prepare|verify", file=sys.stderr)
        return 2
    try:
        state = require_isolated_ci()
        if sys.argv[1] == "prepare":
            prepare(state)
        else:
            verify(state)
    except (ContractFailure, urllib.error.URLError, TimeoutError, ValueError, KeyError) as error:
        print(f"INTEGRATION=FAIL {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
