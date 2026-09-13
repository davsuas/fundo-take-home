import Link from "next/link";
import { Alert } from "@/components/ui/alert";
import { buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export const metadata = {
  title: "Application denied",
};

const REASON_COPY: Record<string, string> = {
  RESTRICTED_STATE: "We are not currently able to offer loans to applicants in this state.",
  BLACKLISTED_SSN: "This application cannot be approved with the information provided.",
};

type SearchParams = Promise<{ ruleCode?: string; reason?: string }>;

export default async function DeniedPage({ searchParams }: { searchParams: SearchParams }) {
  const { ruleCode, reason } = await searchParams;
  const message = (ruleCode && REASON_COPY[ruleCode]) ?? reason ?? "Your application could not be approved.";

  return (
    <main className="mx-auto flex max-w-2xl flex-col gap-6 px-4 py-10">
      <Card>
        <CardHeader>
          <CardTitle>Application not approved</CardTitle>
          <CardDescription>We were unable to approve this loan application.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <Alert variant="destructive">{message}</Alert>

          <Link href="/apply" className={buttonVariants({ variant: "secondary", className: "w-fit" })}>
            Start over
          </Link>
        </CardContent>
      </Card>
    </main>
  );
}
