import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { buildApp } from "../../src/app.js";
import * as store from "../../src/store.js";

const CUSTOMER_ID = "018f2f3a-1b1b-7000-8000-000000000001";

let app;

beforeEach(async () => {
  store.clear();
  app = buildApp({ logger: false });
  await app.ready();
});

afterEach(async () => {
  await app.close();
});

const payload = ({ operation = "create", requestedAmount = 25000, occurredAtUtc = "2026-09-12T10:00:00Z", id = CUSTOMER_ID } = {}) => ({
  customer: {
    id,
    firstName: "Jane",
    lastName: "Doe",
    companyName: "Acme",
    ssnLast4: "5555",
    address: { street: "1 Main St", city: "Springfield", state: "CA", postalCode: "94105" },
  },
  application: { id: "018f2f3a-1b1b-7000-8000-000000000099", requestedAmount, currency: "USD", status: "Approved" },
  operation,
  occurredAtUtc,
});

const put = (body, { id = CUSTOMER_ID, idempotencyKey = "msg-1" } = {}) =>
  app.inject({
    method: "PUT",
    url: `/api/v1/customers/${id}`,
    headers: idempotencyKey ? { "idempotency-key": idempotencyKey } : {},
    payload: body,
  });

describe("PUT /api/v1/customers/:id", () => {
  it("creates a new customer", async () => {
    const response = await put(payload());

    expect(response.statusCode).toBe(200);
    expect(response.json()).toMatchObject({ customerId: CUSTOMER_ID, result: "created" });
  });

  it("updates a returning customer instead of creating a second record", async () => {
    await put(payload());
    const second = await put(payload({ operation: "update", requestedAmount: 40000, occurredAtUtc: "2026-09-12T11:00:00Z" }), { idempotencyKey: "msg-2" });

    expect(second.statusCode).toBe(200);
    expect(second.json().result).toBe("updated");

    const list = await app.inject({ method: "GET", url: "/api/v1/customers" });
    expect(list.json().customers).toHaveLength(1);
    expect(list.json().customers[0].application.requestedAmount).toBe(40000);
  });

  it("ignores a retried older event that arrives after a newer one", async () => {
    await put(payload({ operation: "update", requestedAmount: 40000, occurredAtUtc: "2026-09-12T11:00:00Z" }), { idempotencyKey: "msg-2" });
    const stale = await put(payload({ requestedAmount: 10000, occurredAtUtc: "2026-09-12T10:00:00Z" }));

    expect(stale.statusCode).toBe(200);
    expect(stale.json().result).toBe("ignored_stale");

    const record = await app.inject({ method: "GET", url: `/api/v1/customers/${CUSTOMER_ID}` });
    expect(record.json().application.requestedAmount).toBe(40000);
  });

  it("requires an Idempotency-Key header", async () => {
    const response = await put(payload(), { idempotencyKey: null });

    expect(response.statusCode).toBe(400);
    expect(response.json().error).toBe("idempotency_key_required");
  });

  it("rejects a body that fails schema validation", async () => {
    const response = await put({ customer: { id: CUSTOMER_ID }, application: {} });

    expect(response.statusCode).toBe(400);
    expect(response.json().error).toBe("validation_error");
  });

  it("rejects a body whose customer.id does not match the URL", async () => {
    const response = await put(payload({ id: "018f2f3a-1b1b-7000-8000-000000000999" }));

    expect(response.statusCode).toBe(400);
    expect(response.json().error).toBe("id_mismatch");
  });
});

describe("GET endpoints", () => {
  it("returns 404 for an unknown customer", async () => {
    const response = await app.inject({ method: "GET", url: "/api/v1/customers/018f2f3a-1b1b-7000-8000-000000000404" });
    expect(response.statusCode).toBe(404);
  });

  it("reports health", async () => {
    const response = await app.inject({ method: "GET", url: "/health" });
    expect(response.json()).toEqual({ status: "ok" });
  });
});
