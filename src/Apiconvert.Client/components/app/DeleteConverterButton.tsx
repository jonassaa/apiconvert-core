"use client";

import { SubmitButton } from "@/components/app/SubmitButton";

export function DeleteConverterButton({
  action,
  onConfirm,
  label = "Delete",
  confirmMessage = "Delete this converter? This cannot be undone.",
}: {
  action?: (formData: FormData) => void;
  onConfirm?: () => void | Promise<void>;
  label?: string;
  confirmMessage?: string;
}) {
  if (!action) {
    return (
      <SubmitButton
        variant="destructive"
        size="sm"
        pendingLabel="Deleting..."
        type="button"
        onClick={() => {
          if (confirm(confirmMessage)) {
            onConfirm?.();
          }
        }}
      >
        {label}
      </SubmitButton>
    );
  }

  return (
    <form
      action={action}
      onSubmit={(event) => {
        if (!confirm(confirmMessage)) {
          event.preventDefault();
        }
      }}
    >
      <SubmitButton
        variant="destructive"
        size="sm"
        pendingLabel="Deleting..."
        type="submit"
      >
        {label}
      </SubmitButton>
    </form>
  );
}
