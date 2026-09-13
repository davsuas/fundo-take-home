import Link from "next/link";
import { Alert } from "@/components/ui/alert";
import { buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export const metadata = {
  title: "Application approved",
};

type SearchParams = Promise<{ applicationId?: string; customerId?: string; returning?: string }>;

export default async function ApprovedPage({ searchParams }: { searchParams: SearchParams }) {
  const { applicationId, customerId, returning } = await searchParams;
  const isReturning = returning === "true";

  return (
    <main className="mx-auto flex max-w-2xl flex-col gap-6 px-4 py-10">
      <Card>
        <CardHeader>
          <CardTitle>Application approved</CardTitle>
          <CardDescription>Your loan application has been approved.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <Alert variant="success">
            {isReturning
              ? "We updated your existing application with the new details you submitted."
              : "A new application was created for you."}
          </Alert>

          <dl className="grid grid-cols-1 gap-2 text-sm sm:grid-cols-2">
            {applicationId ? (
              <div>
                <dt className="text-slate-500 dark:text-slate-400">Application ID</dt>
                <dd className="font-mono">{applicationId}</dd>
              </div>
            ) : null}
            {customerId ? (
              <div>
                <dt className="text-slate-500 dark:text-slate-400">Customer ID</dt>
                <dd className="font-mono">{customerId}</dd>
              </div>
            ) : null}
          </dl>

          <Link href="/apply" className={buttonVariants({ variant: "secondary", className: "w-fit" })}>
            Start over
          </Link>
        </CardContent>
      </Card>
    </main>
  );
}
