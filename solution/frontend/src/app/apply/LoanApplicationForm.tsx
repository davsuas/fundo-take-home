"use client";

import { startTransition, useActionState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { submitLoanApplication, type SubmitFormState } from "@/app/apply/actions";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import {
  US_STATES,
  loanApplicationSchema,
  type LoanApplicationInput,
  type LoanApplicationValues,
} from "@/lib/validation/loan-application";

const initialState: SubmitFormState = {};

export function LoanApplicationForm() {
  const [state, formAction, isPending] = useActionState(submitLoanApplication, initialState);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoanApplicationInput, unknown, LoanApplicationValues>({
    resolver: zodResolver(loanApplicationSchema),
    mode: "onBlur",
  });

  // With JavaScript, react-hook-form validates first and only a valid form dispatches the Server
  // Action (which validates again on the server). Without JavaScript, `action` still posts the
  // form straight to the Server Action.
  const onSubmit = handleSubmit((_values, event) => {
    const form = event?.target as HTMLFormElement;
    startTransition(() => formAction(new FormData(form)));
  });

  function errorFor(field: keyof LoanApplicationInput): string | undefined {
    return errors[field]?.message ?? state.fieldErrors?.[field];
  }

  return (
    <form action={formAction} onSubmit={onSubmit} noValidate className="flex flex-col gap-8">
      {state.formError ? <Alert variant="destructive">{state.formError}</Alert> : null}

      <fieldset className="flex flex-col gap-4">
        <legend className="text-sm font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          Applicant
        </legend>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field id="firstName" label="First name" error={errorFor("firstName")}>
            <Input
              id="firstName"
              invalid={Boolean(errorFor("firstName"))}
              aria-describedby={errorFor("firstName") ? "firstName-error" : undefined}
              {...register("firstName")}
            />
          </Field>
          <Field id="lastName" label="Last name" error={errorFor("lastName")}>
            <Input
              id="lastName"
              invalid={Boolean(errorFor("lastName"))}
              aria-describedby={errorFor("lastName") ? "lastName-error" : undefined}
              {...register("lastName")}
            />
          </Field>
          <Field id="companyName" label="Company name" error={errorFor("companyName")}>
            <Input
              id="companyName"
              invalid={Boolean(errorFor("companyName"))}
              aria-describedby={errorFor("companyName") ? "companyName-error" : undefined}
              {...register("companyName")}
            />
          </Field>
          <Field id="ssn" label="SSN" error={errorFor("ssn")}>
            <Input
              id="ssn"
              inputMode="numeric"
              placeholder="XXX-XX-XXXX"
              invalid={Boolean(errorFor("ssn"))}
              aria-describedby={errorFor("ssn") ? "ssn-error" : undefined}
              {...register("ssn")}
            />
          </Field>
        </div>
      </fieldset>

      <fieldset className="flex flex-col gap-4">
        <legend className="text-sm font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          Address
        </legend>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field id="street" label="Street" error={errorFor("street")}>
            <Input
              id="street"
              invalid={Boolean(errorFor("street"))}
              aria-describedby={errorFor("street") ? "street-error" : undefined}
              {...register("street")}
            />
          </Field>
          <Field id="city" label="City" error={errorFor("city")}>
            <Input
              id="city"
              invalid={Boolean(errorFor("city"))}
              aria-describedby={errorFor("city") ? "city-error" : undefined}
              {...register("city")}
            />
          </Field>
          <Field id="state" label="State" error={errorFor("state")}>
            <Select
              id="state"
              invalid={Boolean(errorFor("state"))}
              aria-describedby={errorFor("state") ? "state-error" : undefined}
              defaultValue=""
              {...register("state")}
            >
              <option value="" disabled>
                Select a state
              </option>
              {US_STATES.map((state) => (
                <option key={state} value={state}>
                  {state}
                </option>
              ))}
            </Select>
          </Field>
          <Field id="postalCode" label="Postal code" error={errorFor("postalCode")}>
            <Input
              id="postalCode"
              inputMode="numeric"
              invalid={Boolean(errorFor("postalCode"))}
              aria-describedby={errorFor("postalCode") ? "postalCode-error" : undefined}
              {...register("postalCode")}
            />
          </Field>
        </div>
      </fieldset>

      <fieldset className="flex flex-col gap-4">
        <legend className="text-sm font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          Loan
        </legend>
        <Field id="requestedAmount" label="Requested amount (USD)" error={errorFor("requestedAmount")}>
          <Input
            id="requestedAmount"
            type="number"
            min={0.01}
            max={1_000_000}
            step={0.01}
            invalid={Boolean(errorFor("requestedAmount"))}
            aria-describedby={errorFor("requestedAmount") ? "requestedAmount-error" : undefined}
            {...register("requestedAmount")}
          />
        </Field>
      </fieldset>

      <Button type="submit" size="lg" disabled={isPending} className="w-full">
        {isPending ? "Submitting…" : "Submit application"}
      </Button>
    </form>
  );
}
