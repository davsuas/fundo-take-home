# Fundo Loans — loan application flow

> 🎥 **Demo video:** _<paste public Loom/Jam link here>_

A Next.js form sends an application to a .NET API. A rule engine approves or denies it. Approved
applications are saved in PostgreSQL in one transaction together with an outbox event. A separate
worker process delivers that event to a mock external service over HTTP. The same SSN always maps
to one customer and one application, which are updated on every new submission.

How it is built and why: [ARCHITECTURE.md](ARCHITECTURE.md).

## Run it

Prerequisites: **Docker** (with Compose v2) and **make**. Nothing else; the .NET SDK and Node run in containers.

```bash
cd solution
make up        # creates .env from .env.example, builds images, migrates the DB, starts everything
```

| What | URL |
|---|---|
| Frontend (the form) | http://localhost:5330 |
| External service — everything it received | http://localhost:5230/api/v1/customers |
| API health | http://localhost:5130/health/ready |

`make logs` follows all services, `make down` stops them, `make reset` also deletes the database.
`make help` lists every target.

The stack is `postgres` → `migrate` (one-shot) → `api` + `worker` → `frontend`, plus
`external-service`. To run one service on its own: `docker compose up --build <service>`.

## Test data

| Submit | Result |
|---|---|
| SSN `555-55-5555`, state `CA`, any amount | **Approved**: a new customer and application; the external service shows `operation: "create"` |
| The same SSN again, with a different amount or address | **Approved (returning customer)**: the same customer and application are updated; the external service shows `operation: "update"` and the new amount |
| Any SSN with state `NY` | **Denied**: denied page explaining the state is not served; nothing is saved |
| SSN `111-11-1111`, `222-22-2222` or `333-33-3333` | **Denied**: denied page; nothing is saved (these SSNs are seeded by `migrate`) |

The external service receives the event about a second after approval:

```bash
curl -s http://localhost:5230/api/v1/customers | python3 -m json.tool
```

Or call the API directly:

```bash
curl -s -X POST http://localhost:5130/api/v1/loan-applications -H 'Content-Type: application/json' \
  -d '{"firstName":"Jane","lastName":"Doe","street":"1 Main St","city":"Austin","state":"TX","postalCode":"73301","companyName":"Acme","requestedAmount":25000,"ssn":"555-55-5555"}'
```

## Run the tests

```bash
make test      # all suites below (Docker only)
make check     # lint + type check + all tests
```

| Suite | Command | Covers |
|---|---|---|
| Domain unit | `make test-backend` | Value objects (`Ssn`, `Money`, `Address`, `PersonName`), entities, outbox backoff, Domain has no framework references |
| Application unit | `make test-backend` | Rule engine and each rule; the handler's new / returning / denied / invalid / commit-failure / concurrent-submit paths; Application references no infrastructure |
| Integration (Testcontainers, real PostgreSQL 18) | `make test-backend` | The endpoint end to end (approve, NY, blacklist, returning customer, 8 concurrent same-SSN submits, 400); transaction commit and rollback; unique indexes; the dispatcher (two concurrent workers deliver each event exactly once, retries, claim lease); the HTTP contract |
| Frontend | `make test-frontend` | Shared zod schema, Server Action outcome mapping, form validation |
| External service | `make test-external` | Create vs update, stale-event guard, contract validation |

## Known limitations

- The demo video link above still has to be added.
- The mock external service keeps data in memory, so restarting it clears what it received. The
  database is the source of truth.
- No authentication, as the brief allows.
