"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

export function SideNav({ orgId }: { orgId: string }) {
  const activePath = usePathname() ?? "";
  const [isNavigating, setIsNavigating] = useState(false);
  const navItems = [
    { href: `/org/${orgId}/dashboard`, label: "Dashboard" },
    { href: `/org/${orgId}/converters`, label: "Converters" },
    { href: `/org/${orgId}/logs`, label: "Logs" },
    { href: `/org/${orgId}/settings`, label: "Organization settings" },
  ];

  useEffect(() => {
    setIsNavigating(false);
  }, [activePath]);

  return (
    <aside className="flex min-h-[calc(100vh-4.5rem)] w-56 flex-col gap-2 self-stretch border-r border-border/70 bg-white/80 px-4 py-6 backdrop-blur dark:bg-zinc-900/80">
      <div className="flex items-center justify-between text-[11px] font-semibold uppercase tracking-[0.35em] text-muted-foreground">
        <span>Navigation</span>
        {isNavigating ? (
          <Loader2 className="h-3.5 w-3.5 animate-spin" />
        ) : null}
      </div>
      <nav className="flex flex-col gap-1">
        {navItems.map((item) => {
          const isActive = activePath.startsWith(item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              onClick={() => {
                if (!isActive) setIsNavigating(true);
              }}
              className={cn(
                "rounded-xl px-3 py-2 text-sm transition",
                isActive
                  ? "bg-primary text-primary-foreground shadow-sm"
                  : "text-muted-foreground/80 hover:bg-muted/70 hover:text-foreground"
              )}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
