"use client";

import { useEffect, useState } from "react";
import { CheckCircle2, X } from "lucide-react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

export function FlashToast({
  message,
  paramKey = "success",
}: {
  message?: string;
  paramKey?: string;
}) {
  const [open, setOpen] = useState(Boolean(message));
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  useEffect(() => {
    setOpen(Boolean(message));
  }, [message]);

  useEffect(() => {
    if (!message) return;
    const timer = setTimeout(() => setOpen(false), 3500);
    return () => clearTimeout(timer);
  }, [message]);

  useEffect(() => {
    if (open || !message) return;
    const params = new URLSearchParams(searchParams.toString());
    if (!params.has(paramKey)) return;
    params.delete(paramKey);
    const next = params.toString();
    router.replace(next ? `${pathname}?${next}` : pathname, { scroll: false });
  }, [open, message, paramKey, pathname, router, searchParams]);

  if (!message || !open) return null;

  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed bottom-6 right-6 z-50 w-[320px] rounded-lg border border-emerald-200 bg-emerald-50 text-emerald-900 shadow-lg dark:border-emerald-900/50 dark:bg-emerald-950/40 dark:text-emerald-100"
    >
      <div className="flex items-start gap-3 p-3 text-sm">
        <CheckCircle2 className="mt-0.5 h-4 w-4" />
        <p className="flex-1">{message}</p>
        <button
          type="button"
          onClick={() => setOpen(false)}
          className="rounded p-1 text-emerald-900/60 transition hover:text-emerald-900 dark:text-emerald-100/60 dark:hover:text-emerald-100"
          aria-label="Dismiss"
        >
          <X className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
