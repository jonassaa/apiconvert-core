"use client";

import { useRef } from "react";

export function AutoSubmitForm({
  children,
  className,
  onSubmit,
}: {
  children: React.ReactNode;
  className?: string;
  onSubmit?: (event: React.FormEvent<HTMLFormElement>) => void;
}) {
  const formRef = useRef<HTMLFormElement | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  return (
    <form
      ref={formRef}
      className={className}
      onSubmit={onSubmit}
      onChange={(event) => {
        const target = event.target as HTMLInputElement | HTMLSelectElement | null;
        if (!target) return;

        const shouldDebounce =
          target instanceof HTMLInputElement &&
          (target.type === "text" || target.type === "search");
        const delay = shouldDebounce ? 400 : 0;

        if (timeoutRef.current) {
          clearTimeout(timeoutRef.current);
        }

        timeoutRef.current = setTimeout(() => {
          formRef.current?.requestSubmit();
        }, delay);
      }}
    >
      {children}
    </form>
  );
}
