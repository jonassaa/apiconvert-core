import { Info } from "lucide-react";
import { cn } from "@/lib/utils";

export function TooltipIcon({
  text,
  className,
}: {
  text: string;
  className?: string;
}) {
  return (
    <span className={cn("group relative inline-flex", className)}>
      <button
        type="button"
        aria-label={text}
        className="inline-flex h-5 w-5 items-center justify-center text-muted-foreground transition group-hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <Info className="h-3.5 w-3.5" />
      </button>
      <span
        role="tooltip"
        className="pointer-events-none absolute left-1/2 top-full z-50 mt-2 w-56 -translate-x-1/2 rounded-md bg-foreground px-2 py-1 text-xs text-background opacity-0 shadow-md transition group-hover:opacity-100 group-focus-within:opacity-100"
      >
        {text}
      </span>
    </span>
  );
}
