import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const submitLoanApplication = vi.hoisted(() => vi.fn(async () => ({})));
vi.mock("@/app/apply/actions", () => ({ submitLoanApplication }));

const { LoanApplicationForm } = await import("@/app/apply/LoanApplicationForm");

describe("LoanApplicationForm", () => {
  beforeEach(() => {
    submitLoanApplication.mockClear();
  });

  it("shows field errors and does not call the server when required fields are empty", async () => {
    const user = userEvent.setup();
    render(<LoanApplicationForm />);

    await user.click(screen.getByRole("button", { name: /submit application/i }));

    expect(await screen.findByText("First name is required")).toBeInTheDocument();
    expect(submitLoanApplication).not.toHaveBeenCalled();
  });

  it("shows an SSN-specific error for an invalid SSN", async () => {
    const user = userEvent.setup();
    render(<LoanApplicationForm />);

    await user.type(screen.getByLabelText("SSN"), "123");
    await user.click(screen.getByRole("button", { name: /submit application/i }));

    expect(await screen.findByText("SSN must be 9 digits")).toBeInTheDocument();
  });

  it("masks the SSN as XXX-XX-XXXX, ignoring non-digits and anything past 9 digits", async () => {
    const user = userEvent.setup();
    render(<LoanApplicationForm />);
    const ssn = screen.getByLabelText("SSN");

    await user.type(ssn, "55a5 55-5555999");

    expect(ssn).toHaveValue("555-55-5555");
  });

  it("dispatches the server action with the form data once every field is valid", async () => {
    const user = userEvent.setup();
    render(<LoanApplicationForm />);

    await user.type(screen.getByLabelText("First name"), "Jane");
    await user.type(screen.getByLabelText("Last name"), "Doe");
    await user.type(screen.getByLabelText("Company name"), "Acme");
    await user.type(screen.getByLabelText("SSN"), "555-55-5555");
    await user.type(screen.getByLabelText("Street"), "1 Main St");
    await user.type(screen.getByLabelText("City"), "Springfield");
    await user.selectOptions(screen.getByLabelText("State"), "CA");
    await user.type(screen.getByLabelText("Postal code"), "94105");
    await user.type(screen.getByLabelText("Requested amount (USD)"), "25000");
    await user.click(screen.getByRole("button", { name: /submit application/i }));

    await waitFor(() => expect(submitLoanApplication).toHaveBeenCalledTimes(1));
    const formData = (submitLoanApplication.mock.calls[0] as unknown[])[1] as FormData;
    expect(formData.get("ssn")).toBe("555-55-5555");
    expect(formData.get("state")).toBe("CA");
  });
});
