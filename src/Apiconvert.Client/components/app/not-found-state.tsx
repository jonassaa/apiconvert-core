import Link from "next/link";
import { Button } from "@/components/ui/button";

type NotFoundStateProps = {
  title?: string;
  message?: string;
  actionLabel?: string;
  actionHref?: string;
};

export function NotFoundState({
  title = "Organization not found.",
  message = "This organization may have been deleted or you no longer have access.",
  actionLabel = "Back to organizations",
  actionHref = "/org",
}: NotFoundStateProps) {
  return (
    <div className="space-y-4">
      <p className="text-lg font-semibold">{title}</p>
      <p className="text-sm text-muted-foreground">{message}</p>
      <Button asChild variant="secondary">
        <Link href={actionHref}>{actionLabel}</Link>
      </Button>
    </div>
  );
}
