"use server";

import { redirect } from "next/navigation";
import { loanApplicationSchema } from "@/lib/validation/loan-application";

export type SubmitFormState = {
  fieldErrors?: Record<string, string>;
  formError?: string;
};

type SubmitLoanApplicationResponse = {
  outcome: "Approved" | "Denied";
  applicationId?: string | null;
  customerId?: string | null;
  isReturningCustomer?: boolean;
  ruleCode?: string | null;
  reason?: string | null;
};

/**
 * Runs on the server only — the browser never talks to the API directly, so the API stays on
 * the internal Docker network. Re-parses the raw FormData with the same schema the client used
 * (never trust the client), then calls the backend and redirects to the outcome page.
 */
export async function submitLoanApplication(
  _previousState: SubmitFormState,
  formData: FormData,
): Promise<SubmitFormState> {
  const raw = Object.fromEntries(formData.entries());
  const parsed = loanApplicationSchema.safeParse(raw);

  if (!parsed.success) {
    const fieldErrors: Record<string, string> = {};
    for (const issue of parsed.error.issues) {
      const key = issue.path[0];
      if (typeof key === "string" && !(key in fieldErrors)) {
        fieldErrors[key] = issue.message;
      }
    }
    return { fieldErrors };
  }

  const apiBaseUrl = process.env.API_BASE_URL ?? "http://api:8080";
  let response: Response;

  try {
    response = await fetch(`${apiBaseUrl}/api/v1/loan-applications`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(parsed.data),
      cache: "no-store",
    });
  } catch {
    return { formError: "Could not reach the loan service. Please try again in a moment." };
  }

  if (response.status === 400) {
    // The backend re-validates with its domain rules; its messages are shown as-is.
    const problem = (await response.json().catch(() => null)) as { errors?: Record<string, string[]> } | null;
    const messages = Object.values(problem?.errors ?? {}).flat();
    return { formError: messages.length > 0 ? messages.join(" ") : "Please check your entries and try again." };
  }

  if (!response.ok) {
    return { formError: "Something went wrong submitting your application. Please try again." };
  }

  const result = (await response.json()) as SubmitLoanApplicationResponse;
  const params = new URLSearchParams();

  if (result.outcome === "Approved") {
    if (result.applicationId) params.set("applicationId", result.applicationId);
    if (result.customerId) params.set("customerId", result.customerId);
    params.set("returning", String(Boolean(result.isReturningCustomer)));
    redirect(`/result/approved?${params.toString()}`);
    return { formError: undefined };
  }

  if (result.ruleCode) params.set("ruleCode", result.ruleCode);
  if (result.reason) params.set("reason", result.reason);
  redirect(`/result/denied?${params.toString()}`);
  return { formError: undefined };
}
