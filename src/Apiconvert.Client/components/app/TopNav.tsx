"use client";

import Link from "next/link";
import { UserMenu } from "@/components/app/UserMenu";

type OrgOption = {
  id: string;
  name: string;
  slug: string;
};

export function TopNav({
  orgs,
  activeOrgId,
  activeOrgName,
  userEmail,
  userName,
  userAvatarUrl,
}: {
  orgs: OrgOption[];
  activeOrgId: string | null;
  activeOrgName: string | null;
  userEmail: string | null;
  userName: string | null;
  userAvatarUrl: string | null;
}) {
  return (
    <header className="flex items-center justify-between gap-4 border-b border-border/60 bg-white/70 px-6 py-4 backdrop-blur dark:bg-zinc-900/70">
      <div className="flex items-center gap-3">
        <Link
          href="/"
          className="inline-flex h-9 items-center rounded-2xl border border-border/70 bg-white px-4 text-[11px] font-semibold uppercase tracking-[0.35em] text-muted-foreground dark:bg-zinc-900"
        >
          apiconvert
        </Link>
        {activeOrgName ? (
          <span className="text-sm font-medium text-foreground/80">
            {activeOrgName}
          </span>
        ) : null}
      </div>
      <UserMenu
        userEmail={userEmail}
        userName={userName}
        userAvatarUrl={userAvatarUrl}
        orgs={orgs}
        activeOrgId={activeOrgId}
      />
    </header>
  );
}
