"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { UserMenu } from "@/components/app/UserMenu";

const fieldInputExample = `{
  "customer": {
    "id": "c-123",
    "email": "jane@example.com",
    "status": "active"
  }
}`;

const fieldOutputExample = `{
  "userId": "c-123",
  "contact": {
    "email": "jane@example.com"
  },
  "isActive": true
}`;

const arrayInputExample = `{
  "orders": [
    { "id": "o-1", "total": 18.25 },
    { "id": "o-2", "total": 42.1 }
  ]
}`;

const arrayOutputExample = `{
  "items": [
    { "orderId": "o-1", "amount": 18.25 },
    { "orderId": "o-2", "amount": 42.1 }
  ]
}`;

type OrgListItem = {
  id: string;
};

type UserSummary = {
  email: string | null;
  name: string | null;
  avatarUrl: string | null;
};

export default function GettingStartedPage() {
  const [user, setUser] = useState<UserSummary | null>(null);
  const [dashboardHref, setDashboardHref] = useState("/org");

  useEffect(() => {
    let isActive = true;
    const supabase = createClient();

    async function load() {
      const { data } = await supabase.auth.getUser();
      const currentUser = data.user;
      if (!currentUser) {
        if (isActive) {
          setUser(null);
          setDashboardHref("/org");
        }
        return;
      }

      const name =
        currentUser.user_metadata?.full_name ||
        currentUser.user_metadata?.name ||
        currentUser.user_metadata?.user_name ||
        currentUser.user_metadata?.preferred_username ||
        null;
      const avatarUrl =
        currentUser.user_metadata?.avatar_url || currentUser.user_metadata?.picture || null;

      if (!isActive) return;
      setUser({ email: currentUser.email ?? null, name, avatarUrl });

      try {
        const response = await apiFetch<{ orgs: OrgListItem[] }>("/api/orgs");
        if (!isActive) return;
        const orgId = response.orgs?.[0]?.id;
        setDashboardHref(orgId ? `/org/${orgId}/dashboard` : "/org");
      } catch {
        if (isActive) setDashboardHref("/org");
      }
    }

    load();
    return () => {
      isActive = false;
    };
  }, []);

  return (
    <div className="page-shell">
      <header className="sticky top-0 z-10 border-b border-border/60 bg-white/70 backdrop-blur dark:bg-zinc-900/70">
        <div className="mx-auto flex w-full max-w-5xl items-center justify-between gap-4 px-6 py-4">
          <div className="flex items-center gap-6">
            <Link
              href="/"
              className="inline-flex h-9 items-center rounded-2xl border border-border/70 bg-white px-4 text-[11px] font-semibold uppercase tracking-[0.35em] text-muted-foreground dark:bg-zinc-900"
            >
              apiconvert
            </Link>
            <nav className="flex items-center gap-4 text-sm font-medium text-muted-foreground">
              <Link href="/getting-started" className="transition hover:text-foreground">
                Getting started
              </Link>
              <Link href="/documentation" className="transition hover:text-foreground">
                Documentation
              </Link>
            </nav>
          </div>
          <div className="flex items-center gap-2">
            {user ? (
              <Button asChild size="sm">
                <Link href={dashboardHref}>Go to dashboard</Link>
              </Button>
            ) : (
              <Button size="sm" asChild>
                <Link href="/login">Sign in</Link>
              </Button>
            )}
            {user ? (
              <UserMenu
                userEmail={user.email}
                userName={user.name}
                userAvatarUrl={user.avatarUrl}
              />
            ) : null}
          </div>
        </div>
      </header>

      <main className="page-container space-y-10">
        <section className="surface-panel p-8 md:p-10 space-y-4">
          <p className="page-kicker">Getting Started</p>
          <h1 className="page-title text-4xl md:text-5xl">
            Build your first converter
          </h1>
          <p className="text-base text-muted-foreground">
            Create an inbound endpoint, map fields, and verify output with live
            previews. This guide walks through the minimum setup plus field and
            array examples.
          </p>
          <div className="flex flex-wrap gap-3">
            <Button asChild>
              <Link href="/login">Create a converter</Link>
            </Button>
            <Button variant="outline" asChild>
              <Link href="/documentation">View documentation</Link>
            </Button>
          </div>
        </section>

        <section className="grid gap-6 md:grid-cols-2">
          <Card className="surface-panel">
            <CardHeader>
              <CardTitle>Field mapping example</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4 text-sm text-muted-foreground">
              <p>Start with a JSON payload from a partner:</p>
              <pre className="overflow-x-auto rounded-xl border border-border/70 bg-white/80 p-4 text-xs text-foreground dark:bg-zinc-900/70">
                {fieldInputExample}
              </pre>
              <p>Map the fields to your internal schema:</p>
              <pre className="overflow-x-auto rounded-xl border border-border/70 bg-white/80 p-4 text-xs text-foreground dark:bg-zinc-900/70">
                {fieldOutputExample}
              </pre>
            </CardContent>
          </Card>
          <Card className="surface-panel">
            <CardHeader>
              <CardTitle>Array mapping example</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4 text-sm text-muted-foreground">
              <p>Incoming payloads often include arrays:</p>
              <pre className="overflow-x-auto rounded-xl border border-border/70 bg-white/80 p-4 text-xs text-foreground dark:bg-zinc-900/70">
                {arrayInputExample}
              </pre>
              <p>Map each array item into a new structure:</p>
              <pre className="overflow-x-auto rounded-xl border border-border/70 bg-white/80 p-4 text-xs text-foreground dark:bg-zinc-900/70">
                {arrayOutputExample}
              </pre>
            </CardContent>
          </Card>
        </section>

        <section className="grid gap-6 md:grid-cols-3">
          <Card className="surface-panel">
            <CardHeader>
              <CardTitle>Create a converter</CardTitle>
            </CardHeader>
            <CardContent className="text-sm text-muted-foreground">
              <p>
                Go to your organization dashboard and click “New converter.”
                Provide an inbound path, destination URL, and authentication.
              </p>
            </CardContent>
          </Card>
          <Card className="surface-panel">
            <CardHeader>
              <CardTitle>Map the payload</CardTitle>
            </CardHeader>
            <CardContent className="text-sm text-muted-foreground">
              <p>
                Add mapping rows to translate vendor JSON into your internal
                schema. Preview the output before saving.
              </p>
            </CardContent>
          </Card>
          <Card className="surface-panel">
            <CardHeader>
              <CardTitle>Test inbound delivery</CardTitle>
            </CardHeader>
            <CardContent className="text-sm text-muted-foreground">
              <p>
                Send a sample request to the inbound URL. Review logs and ensure
                the forward destination returns 2xx.
              </p>
            </CardContent>
          </Card>
        </section>
      </main>
    </div>
  );
}
