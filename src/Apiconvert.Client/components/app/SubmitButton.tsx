"use client";

import type { ComponentProps, ReactNode } from "react";
import { useFormStatus } from "react-dom";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";

export type SubmitButtonProps = ComponentProps<typeof Button> & {
  pendingLabel?: string;
  pending?: boolean;
  children: ReactNode;
};

export function SubmitButton({
  pendingLabel,
  pending: pendingOverride,
  disabled,
  children,
  type,
  ...props
}: SubmitButtonProps) {
  const { pending } = useFormStatus();
  const isPending = pendingOverride ?? pending;
  const label = isPending && pendingLabel ? pendingLabel : children;

  return (
    <Button
      {...props}
      type={type ?? "submit"}
      disabled={disabled || isPending}
      aria-busy={isPending}
    >
      {isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
      {label}
    </Button>
  );
}
