import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { LoanApplicationForm } from "@/app/apply/LoanApplicationForm";

export const metadata = {
  title: "Apply for a loan",
};

export default function ApplyPage() {
  return (
    <main className="mx-auto flex max-w-2xl flex-col gap-6 px-4 py-10">
      <Card>
        <CardHeader>
          <CardTitle>Loan application</CardTitle>
          <CardDescription>
            Fill in your details below. We&apos;ll let you know right away whether your
            application is approved.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <LoanApplicationForm />
        </CardContent>
      </Card>
    </main>
  );
}
