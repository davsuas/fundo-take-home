import { describe, expect, it } from "vitest";
import { formatSsn, loanApplicationSchema } from "@/lib/validation/loan-application";

const validInput = () => ({
  firstName: "Jane",
  lastName: "Doe",
  street: "1 Main St",
  city: "Springfield",
  state: "ca",
  postalCode: "94105",
  companyName: "Acme",
  requestedAmount: "25000",
  ssn: "555-55-5555",
});

describe("loanApplicationSchema", () => {
  it("accepts a valid submission and normalizes state/ssn/amount", () => {
    const result = loanApplicationSchema.safeParse(validInput());

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.state).toBe("CA");
      expect(result.data.ssn).toBe("555555555");
      expect(result.data.requestedAmount).toBe(25000);
    }
  });

  it("rejects a missing first name", () => {
    const input = { ...validInput(), firstName: "" };
    expect(loanApplicationSchema.safeParse(input).success).toBe(false);
  });

  it("rejects an ssn that is not 9 digits", () => {
    const input = { ...validInput(), ssn: "123" };
    expect(loanApplicationSchema.safeParse(input).success).toBe(false);
  });

  it("accepts an ssn without dashes", () => {
    const result = loanApplicationSchema.safeParse({ ...validInput(), ssn: "555555555" });
    expect(result.success && result.data.ssn).toBe("555555555");
  });

  it("rejects an ssn with letters or more than 9 digits", () => {
    expect(loanApplicationSchema.safeParse({ ...validInput(), ssn: "55a-55-5555" }).success).toBe(false);
    expect(loanApplicationSchema.safeParse({ ...validInput(), ssn: "555-55-55555" }).success).toBe(false);
  });

  it("rejects a postal code that is not exactly 5 digits", () => {
    const input = { ...validInput(), postalCode: "941" };
    expect(loanApplicationSchema.safeParse(input).success).toBe(false);
  });

  it("rejects an amount of zero or below", () => {
    const input = { ...validInput(), requestedAmount: "0" };
    expect(loanApplicationSchema.safeParse(input).success).toBe(false);
  });

  it("rejects an amount above 1,000,000", () => {
    const input = { ...validInput(), requestedAmount: "1000001" };
    expect(loanApplicationSchema.safeParse(input).success).toBe(false);
  });

  it("rejects a state that isn't 2 letters", () => {
    const input = { ...validInput(), state: "california" };
    expect(loanApplicationSchema.safeParse(input).success).toBe(false);
  });
});

describe("formatSsn", () => {
  it.each([
    ["", ""],
    ["555", "555"],
    ["5555", "555-5"],
    ["55555", "555-55"],
    ["555555", "555-55-5"],
    ["555555555", "555-55-5555"],
    ["555-55-5555", "555-55-5555"],
    ["555 55 5555", "555-55-5555"],
    ["5a5b5-55-5555", "555-55-5555"],
    ["5555555551234", "555-55-5555"],
  ])("formats %j as %j", (input, expected) => {
    expect(formatSsn(input)).toBe(expected);
  });
});
