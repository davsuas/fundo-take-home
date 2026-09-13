import { z } from "zod";

/**
 * One schema drives both react-hook-form's client-side validation and the Server Action's
 * re-parse of the submitted FormData. The server never trusts the client — see actions.ts.
 */
export const loanApplicationSchema = z.object({
  firstName: z.string().trim().min(1, "First name is required").max(100),
  lastName: z.string().trim().min(1, "Last name is required").max(100),
  street: z.string().trim().min(1, "Street is required").max(200),
  city: z.string().trim().min(1, "City is required").max(100),
  state: z
    .string()
    .trim()
    .length(2, "Select a state")
    .transform((value) => value.toUpperCase()),
  postalCode: z.string().trim().regex(/^\d{5}$/, "Postal code must be 5 digits"),
  companyName: z.string().trim().min(1, "Company name is required").max(200),
  requestedAmount: z.coerce
    .number({ error: "Enter a valid amount" })
    .positive("Amount must be greater than 0")
    .max(1_000_000, "Amount must not exceed $1,000,000"),
  ssn: z
    .string()
    .trim()
    .transform((value) => value.replace(/\D/g, ""))
    .refine((digits) => digits.length === 9, "SSN must be 9 digits"),
});

export type LoanApplicationInput = z.input<typeof loanApplicationSchema>;
export type LoanApplicationValues = z.output<typeof loanApplicationSchema>;

export const US_STATES = [
  "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
  "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
  "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
  "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
  "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
] as const;
