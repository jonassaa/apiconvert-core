"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { FlashToast } from "@/components/app/FlashToast";
import { SubmitButton } from "@/components/app/SubmitButton";
import { TopNav } from "@/components/app/TopNav";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const favoriteSortOrder = (left: boolean, right: boolean) => {
  if (left === right) return 0;
  return left ? -1 : 1;
};

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

export default function OrgOverviewPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [orgs, setOrgs] = useState<OrgListItem[]>([]);
  const [userProfile, setUserProfile] = useState<UserProfile>({
    email: null,
    name: null,
    avatarUrl: null,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [createPending, setCreatePending] = useState(false);
  const [favoritePending, setFavoritePending] = useState<Record<string, boolean>>({});

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
        const message = err instanceof Error ? err.message : "Failed to load";
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

  const orderedOrgs = useMemo(() => {
    return [...orgs].sort((a, b) => {
      const favoriteOrder = favoriteSortOrder(a.isFavorite, b.isFavorite);
      if (favoriteOrder !== 0) return favoriteOrder;
      return a.name.localeCompare(b.name);
    });
  }, [orgs]);

  const favoriteOrgs = useMemo(() => {
    return orderedOrgs.filter((org) => org.isFavorite);
  }, [orderedOrgs]);

  async function handleCreateOrg(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (createPending) return;
    setCreatePending(true);
    setError(null);
    const form = event.currentTarget;

    try {
      const formData = new FormData(form);
      const name = String(formData.get("name") || "").trim();
      if (!name) {
        setError("Organization name is required.");
        return;
      }

      const response = await apiFetch<{ org: { id: string; name: string; slug: string } }>(
        "/api/orgs",
        {
          method: "POST",
          body: { name },
        }
      );

      setOrgs((prev) => [
        ...prev,
        {
          id: response.org.id,
          name: response.org.name,
          slug: response.org.slug,
          role: "owner",
          isFavorite: false,
        },
      ]);
      form.reset();
      router.push(`/org/${response.org.id}/dashboard`);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to create organization";
      setError(message);
    } finally {
      setCreatePending(false);
    }
  }

  async function toggleFavorite(orgId: string, nextFavorite: boolean) {
    if (favoritePending[orgId]) return;
    setFavoritePending((prev) => ({ ...prev, [orgId]: true }));
    try {
      await apiFetch(`/api/orgs/${orgId}/favorites`, {
        method: nextFavorite ? "POST" : "DELETE",
      });
      setOrgs((prev) =>
        prev.map((org) =>
          org.id === orgId ? { ...org, isFavorite: nextFavorite } : org
        )
      );
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to update favorite.";
      setError(message);
    } finally {
      setFavoritePending((prev) => ({ ...prev, [orgId]: false }));
    }
  }

  return (
    <div className="space-y-8">
      <TopNav
        orgs={favoriteOrgs}
        activeOrgId={null}
        activeOrgName={null}
        userEmail={userProfile.email}
        userName={userProfile.name}
        userAvatarUrl={userProfile.avatarUrl}
      />
      <FlashToast message={searchParams.get("success") ?? undefined} />
      <main className="page-container space-y-8">
        <header className="flex flex-wrap items-start justify-between gap-4">
          <div className="space-y-2">
            <p className="page-kicker">Organizations</p>
            <h1 className="page-title text-3xl text-foreground">
              Manage your organizations
            </h1>
            <p className="page-lede">
              Favorite organizations appear in your top-right menu. You can
              create new organizations anytime or jump into a dashboard below.
            </p>
          </div>
        </header>

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,0.9fr)]">
          <Card className="border border-border/70 bg-white/80 shadow-sm dark:bg-zinc-900/70">
            <CardHeader>
              <CardTitle>Your organizations</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {loading ? (
                <div className="rounded-2xl border border-dashed border-border/70 bg-white/60 p-6 text-sm text-muted-foreground dark:bg-zinc-950/40">
                  Loading organizations…
                </div>
              ) : orderedOrgs.length ? (
                orderedOrgs.map((org) => (
                  <div
                    key={org.id}
                    className="surface-card flex flex-col gap-4 p-4"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-3">
                      <div>
                        <p className="text-base font-semibold text-foreground">
                          {org.name}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {org.slug}
                        </p>
                      </div>
                      <Badge variant="secondary" className="capitalize">
                        {org.role}
                      </Badge>
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                      <Button asChild size="sm">
                        <Link href={`/org/${org.id}/dashboard`}>
                          Open dashboard
                        </Link>
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant={org.isFavorite ? "secondary" : "outline"}
                        aria-label={
                          org.isFavorite
                            ? `Unfavorite ${org.name}`
                            : `Favorite ${org.name}`
                        }
                        onClick={() => toggleFavorite(org.id, !org.isFavorite)}
                        disabled={favoritePending[org.id]}
                      >
                        {org.isFavorite ? "Favorited" : "Mark favorite"}
                      </Button>
                    </div>
                  </div>
                ))
              ) : (
                <div className="rounded-2xl border border-dashed border-border/70 bg-white/60 p-6 text-sm text-muted-foreground dark:bg-zinc-950/40">
                  You have not created any organizations yet. Create one to
                  start building converters.
                </div>
              )}
            </CardContent>
          </Card>

          <Card className="border border-border/70 bg-white/80 shadow-sm dark:bg-zinc-900/70">
            <CardHeader>
              <CardTitle>Create a new organization</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <form onSubmit={handleCreateOrg} className="grid gap-4">
                <div className="space-y-2">
                  <Label htmlFor="name">Organization name</Label>
                  <Input id="name" name="name" required />
                </div>
                <SubmitButton type="submit" pendingLabel="Creating..." pending={createPending}>
                  Create organization
                </SubmitButton>
              </form>
              {error ? (
                <p className="text-sm text-destructive">
                  {error}
                </p>
              ) : null}
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  );
}
