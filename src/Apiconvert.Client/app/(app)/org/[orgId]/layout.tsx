"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useParams, useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { SideNav } from "@/components/app/SideNav";
import { TopNav } from "@/components/app/TopNav";
import { NotFoundState } from "@/components/app/not-found-state";

type OrgListItem = {
  id: string;
  name: string;
  slug: string;
  role: string;
  isFavorite: boolean;
};

type UserProfile = {
  email: string | null;
  name: string | null;
  avatarUrl: string | null;
};

const isUuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export default function OrgLayout({ children }: { children: ReactNode }) {
  const params = useParams<{ orgId: string }>();
  const router = useRouter();
  const [orgs, setOrgs] = useState<OrgListItem[]>([]);
  const [userProfile, setUserProfile] = useState<UserProfile>({
    email: null,
    name: null,
    avatarUrl: null,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isActive = true;
    const supabase = createClient();

    async function load() {
      try {
        const { data } = await supabase.auth.getUser();
        const user = data.user;
        if (!user) {
          router.replace("/login");
          return;
        }

        if (!isActive) return;
        const name =
          user.user_metadata?.full_name ||
          user.user_metadata?.name ||
          user.user_metadata?.user_name ||
          user.user_metadata?.preferred_username ||
          null;
        const avatarUrl =
          user.user_metadata?.avatar_url || user.user_metadata?.picture || null;

        setUserProfile({
          email: user.email ?? null,
          name,
          avatarUrl,
        });

        const response = await apiFetch<{ orgs: OrgListItem[] }>("/api/orgs");
        if (!isActive) return;
        setOrgs(response.orgs);
        setError(null);
      } catch (err) {
        if (!isActive) return;
        const message = err instanceof Error ? err.message : "Failed to load organizations";
        setError(message);
      } finally {
        if (isActive) setLoading(false);
      }
    }

    load();
    return () => {
      isActive = false;
    };
  }, [router]);

  const favoriteOrgs = useMemo(() => {
    return orgs.filter((org) => org.isFavorite);
  }, [orgs]);

  const activeOrg = useMemo(() => {
    const orgIdParam = params.orgId;
    if (isUuid.test(orgIdParam)) {
      return orgs.find((org) => org.id === orgIdParam) ?? null;
    }
    return orgs.find((org) => org.slug === orgIdParam) ?? null;
  }, [orgs, params.orgId]);

  useEffect(() => {
    if (!loading && !error) {
      const orgIdParam = params.orgId;
      if (!isUuid.test(orgIdParam) && activeOrg) {
        router.replace(`/org/${activeOrg.id}/dashboard`);
      }
    }
  }, [activeOrg, error, loading, params.orgId, router]);

  if (loading) {
    return <div className="page-shell" />;
  }

  if (!activeOrg) {
    const normalizedError = (error ?? "").toLowerCase();
    const isNotFound =
      normalizedError.includes("not found") ||
      normalizedError.includes("not a member of this organization");
    return (
      <div className="page-shell px-8 py-8">
        {isNotFound ? (
          <NotFoundState />
        ) : (
          <p className="text-sm text-destructive">
            {error ?? "Organization not found."}
          </p>
        )}
      </div>
    );
  }

  return (
    <div className="relative">
      <TopNav
        orgs={favoriteOrgs}
        activeOrgId={activeOrg.id}
        activeOrgName={activeOrg.name}
        userEmail={userProfile.email}
        userName={userProfile.name}
        userAvatarUrl={userProfile.avatarUrl}
      />
      <div className="flex min-h-[calc(100vh-4.5rem)] items-stretch">
        <SideNav orgId={activeOrg.id} />
        <main className="flex-1 px-8 py-8">{children}</main>
      </div>
    </div>
  );
}
