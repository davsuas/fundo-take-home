import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// Mirrors real next/navigation redirect(), which throws a special error to abort rendering —
// callers of the action never see code after redirect() run.
const redirectMock = vi.hoisted(() =>
  vi.fn((url: string) => {
    throw new Error(`NEXT_REDIRECT:${url}`);
  }),
);

vi.mock("next/navigation", () => ({
  redirect: redirectMock,
}));

const { submitLoanApplication } = await import("@/app/apply/actions");

function validFormData(overrides: Record<string, string> = {}) {
  const data = new FormData();
  const base: Record<string, string> = {
    firstName: "Jane",
    lastName: "Doe",
    street: "1 Main St",
    city: "Springfield",
    state: "CA",
    postalCode: "94105",
    companyName: "Acme",
    requestedAmount: "25000",
    ssn: "555-55-5555",
    ...overrides,
  };
  for (const [key, value] of Object.entries(base)) {
    data.set(key, value);
  }
  return data;
}

describe("submitLoanApplication", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    redirectMock.mockClear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns field errors without calling the API when input fails validation", async () => {
    const state = await submitLoanApplication({}, validFormData({ ssn: "123" }));

    expect(state.fieldErrors?.ssn).toBeDefined();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("maps an Approved response to a redirect to /result/approved with the ids", async () => {
    fetchMock.mockResolvedValue(
      new Response(
        JSON.stringify({
          outcome: "Approved",
          applicationId: "app-1",
          customerId: "cust-1",
          isReturningCustomer: false,
        }),
        { status: 200 },
      ),
    );

    await expect(submitLoanApplication({}, validFormData())).rejects.toThrow("NEXT_REDIRECT");

    expect(redirectMock).toHaveBeenCalledTimes(1);
    expect(redirectMock).toHaveBeenCalledWith(
      expect.stringMatching(/^\/result\/approved\?.*applicationId=app-1.*customerId=cust-1/),
    );
  });

  it("maps a Denied response to a redirect to /result/denied with the rule code", async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ outcome: "Denied", ruleCode: "RESTRICTED_STATE", reason: "NY is restricted." }), {
        status: 200,
      }),
    );

    await expect(submitLoanApplication({}, validFormData())).rejects.toThrow("NEXT_REDIRECT");

    expect(redirectMock).toHaveBeenCalledTimes(1);
    expect(redirectMock).toHaveBeenCalledWith(expect.stringMatching(/^\/result\/denied\?.*ruleCode=RESTRICTED_STATE/));
  });

  it("returns a form error when the API is unreachable", async () => {
    fetchMock.mockRejectedValue(new Error("network down"));

    const state = await submitLoanApplication({}, validFormData());

    expect(state.formError).toMatch(/could not reach/i);
    expect(redirectMock).not.toHaveBeenCalled();
  });
});
