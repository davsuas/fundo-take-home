import { describe, expect, it } from "vitest";
import { customerUpsertSchema } from "../../src/schemas/customerUpsert.js";

const validPayload = () => ({
  customer: {
    id: "018f2f3a-1b1b-7000-8000-000000000001",
    firstName: "Jane",
    lastName: "Doe",
    companyName: "Acme",
    ssnLast4: "5555",
    address: { street: "1 Main St", city: "Springfield", state: "CA", postalCode: "94105" },
  },
  application: {
    id: "018f2f3a-1b1b-7000-8000-000000000002",
    requestedAmount: 25000,
    currency: "USD",
    status: "Approved",
  },
  operation: "create",
  occurredAtUtc: "2026-09-12T10:00:00.1234567Z",
});

describe("customerUpsertSchema", () => {
  it("accepts the payload the backend sends", () => {
    expect(customerUpsertSchema.safeParse(validPayload()).success).toBe(true);
  });

  it.each([
    ["a non-uuid customer id", (p) => (p.customer.id = "not-a-uuid")],
    ["ssnLast4 that is not 4 digits", (p) => (p.customer.ssnLast4 = "555")],
    ["a state that is not 2 letters", (p) => (p.customer.address.state = "California")],
    ["a non-positive requestedAmount", (p) => (p.application.requestedAmount = 0)],
    ["an operation outside create/update", (p) => (p.operation = "delete")],
    ["a missing occurredAtUtc", (p) => delete p.occurredAtUtc],
  ])("rejects %s", (_, mutate) => {
    const payload = validPayload();
    mutate(payload);
    expect(customerUpsertSchema.safeParse(payload).success).toBe(false);
  });
});
