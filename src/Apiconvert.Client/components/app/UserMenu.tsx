"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { createClient } from "@/lib/supabase/client";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import Image from "next/image";
import { ThemeToggle } from "@/components/app/ThemeToggle";

type OrgOption = {
  id: string;
  name: string;
  slug: string;
};

function getInitial(name: string) {
  return name.trim().charAt(0).toUpperCase();
}

export function UserMenu({
  userEmail,
  userName,
  userAvatarUrl,
  orgs,
  activeOrgId,
}: {
  userEmail: string | null;
  userName: string | null;
  userAvatarUrl: string | null;
  orgs?: OrgOption[];
  activeOrgId?: string | null;
}) {
  const router = useRouter();
  const pathname = usePathname() ?? "";
  const [mounted, setMounted] = useState(false);
  const [signOutPending, setSignOutPending] = useState(false);
  const displayName = userName ?? userEmail;
  const avatarLabel = displayName ?? "Account";
  const showAvatar = Boolean(displayName);
  const showOrgs = Boolean(orgs?.length);
  const selectedOrgId = orgs?.some((org) => org.id === activeOrgId)
    ? activeOrgId ?? ""
    : orgs?.[0]?.id ?? "";

  function handleOrgChange(value: string) {
    if (!value || value === selectedOrgId) return;
    const baseMatch = pathname.match(/^\/org\/[^/]+/);
    const base = baseMatch ? pathname.replace(/^\/org\/[^/]+/, `/org/${value}`) : "";
    router.push(base || `/org/${value}/dashboard`);
  }

  useEffect(() => {
    setMounted(true);
  }, []);

  async function handleSignOut() {
    if (signOutPending) return;
    setSignOutPending(true);
    try {
      const supabase = createClient();
      await supabase.auth.signOut();
      router.replace("/login");
    } finally {
      setSignOutPending(false);
    }
  }

  const menuButton = (
    <Button variant="outline" size="sm">
      <span className="flex items-center gap-2">
        {showAvatar ? (
          <span className="flex h-7 w-7 items-center justify-center overflow-hidden rounded-full border border-border/70 bg-muted text-xs font-semibold text-muted-foreground">
            {userAvatarUrl ? (
              <Image
                src={userAvatarUrl}
                alt={avatarLabel}
                width={28}
                height={28}
                className="h-full w-full object-cover"
                unoptimized
              />
            ) : (
              getInitial(avatarLabel)
            )}
          </span>
        ) : null}
        <span className="max-w-[160px] truncate text-left text-sm">
          {displayName ?? "Account"}
        </span>
      </span>
    </Button>
  );

  if (!mounted) {
    return menuButton;
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{menuButton}</DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuLabel>Favorites</DropdownMenuLabel>
        {showOrgs ? (
          <DropdownMenuRadioGroup
            value={selectedOrgId}
            onValueChange={handleOrgChange}
          >
            {orgs?.map((org) => (
              <DropdownMenuRadioItem key={org.id} value={org.id}>
                {org.name}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        ) : (
          <DropdownMenuItem disabled>No favorite organizations</DropdownMenuItem>
        )}
        <DropdownMenuItem asChild>
          <Link href="/org">Organizations</Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuLabel>Account</DropdownMenuLabel>
        {userEmail ? (
          <DropdownMenuItem onSelect={(event) => event.preventDefault()}>
            <button
              type="button"
              className="w-full text-left text-sm"
              onClick={handleSignOut}
              disabled={signOutPending}
            >
              Sign out
            </button>
          </DropdownMenuItem>
        ) : null}
        <ThemeToggle />
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
