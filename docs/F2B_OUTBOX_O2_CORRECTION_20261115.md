# F2b O2 — Outbox migration and enqueue concurrency correction

## Scope

This correction is based on source commit `f5e12e2187c4e1efcf867d3385cf69f68a4b9c5b`.
It does not enable public registration, dispatch, SMTP, a worker, production migrations,
or access to CT164.

## Root causes

1. The offline migration generator used the pre-F2a SQLite baseline
   `20261111125709_PublicationFreshnessTimestamps`. The correct SQLite parent is
   `20261114183515_F2aOrganizationClaimFoundation`. PostgreSQL remains based on
   `20261111203833_InitialPostgreSqlSchema`.
2. The enqueue sender used a read-then-insert check without a database uniqueness
   constraint. Parallel requests could both observe no Pending row and insert
   duplicates. The previous non-unique index did not enforce the coalescing contract.

## Corrective design

- Add a provider-neutral filtered unique index
  `UX_IdentityEmailOutbox_PendingUserPurpose` on `(UserId, Purpose)`
  where `Status = 'Pending'`. SQLite and PostgreSQL both support this partial index.
- Keep Leased/Sent/DeadLetter/Suppressed history outside the unique predicate.
- Refresh an expired Pending row in place with a revision-guarded conditional update.
- On a competing insert, catch only the exact provider-specific unique violation,
  detach the failed entity, and re-read. Do not swallow constraint failures inside
  a caller-owned transaction because PostgreSQL marks that transaction aborted.
- Bound retries; fail closed if concurrent state transitions do not stabilize.
- Generate SQLite SQL from the F2a organization/claim migration and PostgreSQL SQL
  from its reviewed initial baseline.
- Add synthetic SQLite tests for active coalescing, expired-row reuse, unique-index
  enforcement, leased-history coexistence, and parallel enqueue attempts.

## Verification gates

| Gate | Status |
|---|---|
| Source correction | Implemented on corrective branch |
| PowerShell syntax / .NET build | Pending local exact-SHA QA |
| SQLite synthetic concurrency | Pending local exact-SHA QA |
| SQLite migration generation | Pending |
| PostgreSQL migration generation | Pending |
| PostgreSQL disposable apply / concurrency | Pending |
| CT164 / production DB | Not accessed |
| Public registration / dispatch | Remain disabled |
| Release | HOLD |

## Acceptance

O2 is not complete until the generated migrations are reviewed, both providers apply
successfully to disposable databases, the unique index is verified in each provider,
and the independent postflight evidence is clean. No production database apply is
part of O2.
