import { createOrgAction } from "@/app/(app)/org/actions";
import { SubmitButton } from "@/components/app/SubmitButton";
import { FlashToast } from "@/components/app/FlashToast";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import Link from "next/link";

export default async function NewOrgPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string; success?: string }>;
}) {
  const resolvedSearchParams = await searchParams;
  return (
    <div className="page-container">
      <FlashToast message={resolvedSearchParams.success} />
      <div className="mx-auto w-full max-w-xl">
        <Card>
          <CardHeader>
            <CardTitle className="text-2xl">Create organization</CardTitle>
            <Button asChild variant="link" className="h-auto px-0 text-xs">
              <Link href="/org">Back to organizations</Link>
            </Button>
          </CardHeader>
          <CardContent>
            <form action={createOrgAction} className="grid gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">Organization name</Label>
                <Input id="name" name="name" required />
              </div>
              <SubmitButton type="submit" pendingLabel="Creating...">
                Create organization
              </SubmitButton>
            </form>
            {resolvedSearchParams.error ? (
              <p className="mt-4 text-sm text-destructive">
                {resolvedSearchParams.error}
              </p>
            ) : null}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
