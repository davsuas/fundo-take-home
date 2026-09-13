# CLAUDE.md

Context for Claude Code working in this repository.

## What this repo is

A take-home test for **Fundo LLC** (Full-Stack Engineer, .NET + Next.js). The brief lives in
`docs/Take-Home Test Fundo-LLC.md` — it is the source of truth for requirements. Read it before
changing anything.

The deliverable is a **loan application flow**: a Next.js form → a backend **rule engine** decides
approve/deny → approved applications are persisted **transactionally** (customer + application +
outbox event) → a **background worker** pushes the same data to an **external HTTP service** (a
mock we build). Returning customers (same SSN) **update** existing records locally and in the
external service.

The reviewer is Claude Code running Fundo's internal review skills, checking clean architecture,
DDD, SOLID and naming. Documentation must let a reviewer validate decisions without asking us.

> The brief states explicitly: **"We care about simplicity and design. Over-engineering is a
> negative point."** Every abstraction must earn its place, and `solution/ARCHITECTURE.md` must say why.

## Repository layout

```
docs/       The brief
solution/   The submission — all work happens here
CLAUDE.md   This file
README.md   Points the reviewer at solution/
```

`solution/` is self-contained: its own `docker-compose.yml`, `Makefile`, `.env.example`, `README.md`,
`ARCHITECTURE.md`, tests and host ports (frontend 5330, api 5130, external service 5230).

## Topology (solution/)

postgres → `migrate` (one-shot) → `api` (request-only) + `worker` (runs `OutboxDispatcher` only)
→ `frontend`, plus `external-service`. The API and worker share `Fundo.Loans.Infrastructure` and
the database; the worker exists so external-service latency or failures never touch requests.

## Pinned stack (do not downgrade)

| Part | Version |
|---|---|
| .NET / ASP.NET Core | 10.0 (LTS) — `sdk:10.0`, `aspnet:10.0` (api), `runtime:10.0` (worker) |
| EF Core + Npgsql provider | 10.0.12 / 10.0.3 |
| PostgreSQL | 18 (`postgres:18-alpine`) |
| Next.js / React | 16.3.5 / 19.3.0 — use `src/proxy.ts`, not the deprecated `middleware.ts` |
| Tailwind CSS | 4.3.3 (+ hand-written shadcn/ui-style primitives) |
| Zod / react-hook-form / @hookform/resolvers | 4.6.2 / 7.88.0 / 5.2.2 |
| ESLint (flat config) / typescript-eslint / jsdom | 10.10.0 / 8.70.0 / 30.0.1 — not `eslint-config-next`, whose bundled plugins stop at ESLint 9 (all 9.x releases are deprecated) |
| Node (external service + frontend) | 24 (`node:24-alpine`) |
| Serilog.AspNetCore / Serilog.Extensions.Hosting | 10.0.0 |
| xUnit v3 / NSubstitute / Testcontainers | 4.0.0 / 6.2.0 / 4.15.0 |

## Engineering principles

- **SOLID** — one reason to change per class; add a deny rule = one new class + one
  `AddDenyRule<T>()` line, never edit the engine or an existing rule.
- **KISS / YAGNI** — no MediatR, no generic repository, no CQRS, no message broker, no
  OpenTelemetry, no rate limiting, no load/E2E suites. Repositories exist only for the two
  aggregates the use case loads. If the brief does not need it, it does not exist.
- **Clean architecture / DDD** — `Api/Worker → Infrastructure → Application → Domain`. Domain has
  zero framework references; Application references no infrastructure (both guarded by
  `ArchitectureTests`). Value objects guard invariants. Ports (`IUnitOfWork`,
  `IExternalCustomerRegistry`, …) live in `Application/Abstractions`. Endpoints only map.
- **Results, not exceptions, for expected outcomes** — invalid input and denials are
  `SubmitLoanApplicationResult` values; exceptions are for real failures
  (`ConcurrencyConflictException` signals a lost unique-key race and is retried once).
- **Tests that matter** — rule engine, returning-customer path, endpoint, transaction rollback,
  concurrent same-SSN submits, dispatcher claiming/retries, against real PostgreSQL via
  Testcontainers. Tests live in `backend/tests`, `frontend/tests`, `external-service/tests`.
- **Idempotency** — submit is keyed by SSN; outbox delivery is at-least-once to an idempotent
  `PUT /api/v1/customers/{id}` with `Idempotency-Key`; the receiver ignores events older than
  what it has (`occurredAtUtc`).
- **Security, kept proportionate** — SSN stored only as HMAC hash + last 4; least-privilege DB role
  for api/worker, schema changes only via committed migrations run by `migrate`; hardened
  non-root read-only containers; nonce-based CSP on the frontend; secrets only via env.

## Conventions

- C#: file-scoped namespaces, `sealed` by default, `record` for DTOs/value objects, nullable
  enabled, warnings-as-errors, central package management (`Directory.Packages.props`),
  `TimeProvider` for time.
- Naming says intent (`SubmitLoanApplicationHandler`, `RestrictedStateRule`,
  `HttpExternalCustomerRegistry`, `OutboxDispatcher`) — no `Manager`/`Helper`/`Util`/`Service` suffixes.
- TypeScript: strict, no `any`; the browser never calls the API — only the Server Action does.
- Every command is a `make` target in `solution/`; `make check` must pass before submitting.

## Domain rules and test data

- Deny if `address.state == "NY"` → `RESTRICTED_STATE`.
- Deny if the SSN is blacklisted → `BLACKLISTED_SSN`. Seeded: `111-11-1111`, `222-22-2222`, `333-33-3333`.
- First matching rule wins; denied applications persist **nothing** and publish **no** event.
- Approved sample: SSN `555-55-5555`, state `CA`. Submitting it again updates the same customer
  and application and sends an `update` to the external service.

## Working agreements

- Do not add a dependency, layer or container without writing down why in `solution/ARCHITECTURE.md`.
- Keep `solution/README.md` copy-paste runnable: `make up`, then open the frontend.
- Keep docs short — the brief says one good page beats five vague ones.
