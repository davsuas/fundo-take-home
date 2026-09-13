import { type ReactNode } from "react";
import { Label } from "@/components/ui/label";

export interface FieldProps {
  id: string;
  label: string;
  error?: string;
  children: ReactNode;
}

/** Wires a label, its control, and an error message together with the aria attributes screen readers need — one field, one accessible unit. */
export function Field({ id, label, error, children }: FieldProps) {
  const errorId = `${id}-error`;

  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {error ? (
        <p id={errorId} role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      ) : null}
    </div>
  );
}
