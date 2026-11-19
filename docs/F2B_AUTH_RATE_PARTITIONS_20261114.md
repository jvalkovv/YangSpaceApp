# F2b auth rate partition remediation — 2026-11-14

**Issue:** [#34](https://github.com/jvalkovv/YangSpaceApp/issues/34) (subtask of [#33](https://github.com/jvalkovv/YangSpaceApp/issues/33); master [#32](https://github.com/jvalkovv/YangSpaceApp/issues/32)).

**State:** SOURCE COMMITTED / NOT WINDOWS TESTED / NOT DEPLOYED. QA-only bounded patch stacked on F2b Draft PR #29. No app or DB flag is enabled.

## Root cause

`Program.cs` used a single `AddFixedWindowLimiter("auth")` for all login/signup endpoints, producing a **shared** 12 requests/minute budget: one noisy client could block every other client.

Replace with `AddPolicy("auth", AuthRateLimitPolicy.CreatePartition)` using `HttpContext.Connection.RemoteIpAddress`, normalizing IPv4-mapped IPv6 and ignoring raw `X-Forwarded-For`. Preserve 12/minute, zero queue and 429 status. Key unknown transport address into one fail-closed bucket.

## Crucial deployment boundary

If ASP.NET Core sees only Nginx loopback, **all public users still share the loopback bucket**. Before any real public account activation, explicitly configure the ingress to drop caller-provided forwarding headers and supply the correct client address; ASP.NET Core may trust ONLY the actual local proxy address/network through audited Forwarded Headers middleware before UseRateLimiter. Never simply trust all proxies or use a raw forwarded header as the limiter key. Real proxy/abuse HTTP tests remain a **separate P0 HOLD** in #33. The current patch alone is not a production abuse gate.

## Synthetic tests

`scripts/Test-F2b-Auth-Rate-Limiter-Local.ps1 -CommitSha <exact SHA>` runs a clean Windows exact-source .NET 10 Release restore/build and `--auth-rate-partition-only` contract. Checks distinct IPs, IPv4-mapped IPv6 equivalence, spoofed XFF ignored, null-peer fail-closed, v6, preserved cap, stable partition key, clean worktree, stage receipt logs and SHA-256 manifest.

No PostgreSQL, SMTP, real HTTP, CT164, external ports, evidence intake or production release. Historical F2a/F2b test evidence is not retested on this new SHA.
