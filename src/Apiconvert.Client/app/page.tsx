"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { SubmitButton } from "@/components/app/SubmitButton";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DemoMapper } from "@/components/app/DemoMapper";
import { UserMenu } from "@/components/app/UserMenu";

type OrgListItem = {
  id: string;
};

type UserSummary = {
  email: string | null;
  name: string | null;
  avatarUrl: string | null;
};

export default function HomePage() {
  const [user, setUser] = useState<UserSummary | null>(null);
  const [dashboardHref, setDashboardHref] = useState("/org");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const origin = useMemo(() => {
    if (typeof window !== "undefined") {
      return window.location.origin;
    }
    return process.env.NEXT_PUBLIC_SITE_URL || "http://localhost:3123";
  }, []);

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

  async function handleGithubSignIn(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    setPending(true);
    setError(null);

    try {
      const supabase = createClient();
      const { data, error: signInError } = await supabase.auth.signInWithOAuth({
        provider: "github",
        options: {
          redirectTo: `${origin}/auth/callback`,
        },
      });

      if (signInError) {
        setError(signInError.message);
        return;
      }

      if (data.url) {
        window.location.assign(data.url);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to sign in.";
      setError(message);
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="page-shell">
      <header className="sticky top-0 z-10 border-b border-border/60 bg-white/70 backdrop-blur dark:bg-zinc-900/70">
        <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 px-6 py-4">
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
              <>
                <Button asChild size="sm">
                  <Link href={dashboardHref}>Go to dashboard</Link>
                </Button>
              </>
            ) : (
              <>
                <Button size="sm" asChild>
                  <Link href="/login">Sign in</Link>
                </Button>
              </>
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

      <main className="mx-auto w-full max-w-6xl space-y-20 px-6 py-16">
        <section className="grid gap-12 md:grid-cols-2 md:items-center">
          <div className="space-y-6 animate-fade-up">
            <div className="inline-flex items-center gap-3 rounded-full border border-border/70 bg-white/70 px-4 py-2 text-xs font-semibold uppercase tracking-[0.35em] text-muted-foreground dark:bg-zinc-900/70">
              apiconvert for IT teams
              <span className="inline-flex h-2 w-2 rounded-full bg-amber-400 shadow-[0_0_12px_rgba(251,191,36,0.6)]" />
            </div>
            <h1 className="text-4xl font-semibold leading-tight md:text-5xl">
              <span className="block text-5xl font-medium md:text-6xl">
                Ship clean integrations
              </span>
              without rewriting core APIs.
            </h1>
            <p className="text-base text-muted-foreground">
              apiconvert lets IT teams accept partner payloads, map them to an
              internal schema, and forward clean requests with authentication and
              logging built in. Update mappings without redeploying services.
            </p>
            <div className="flex flex-wrap items-center gap-3">
              {user ? (
                <Button asChild className="h-12 px-6 text-base shadow-lg shadow-black/10">
                  <Link href={dashboardHref}>Go to dashboard</Link>
                </Button>
              ) : (
                <form onSubmit={handleGithubSignIn}>
                  <SubmitButton
                    type="submit"
                    pending={pending}
                    pendingLabel="Signing in..."
                    className="h-12 px-6 text-base shadow-lg shadow-black/10"
                  >
                    Start with GitHub
                  </SubmitButton>
                </form>
              )}
              <div className="flex flex-col gap-1 text-xs text-muted-foreground">
                <Button variant="outline" asChild className="h-11 px-5">
                  <Link href="#examples">See mapping examples</Link>
                </Button>
              </div>
            </div>
            {error ? <p className="text-sm text-destructive">{error}</p> : null}
            <div className="grid gap-3 text-sm text-muted-foreground md:grid-cols-2">
              <div className="rounded-2xl border border-border/70 bg-white/80 p-4 shadow-[0_10px_25px_-20px_rgba(15,15,15,0.5)] dark:bg-zinc-900/70">
                Keep vendor payloads out of production code.
              </div>
              <div className="rounded-2xl border border-border/70 bg-white/80 p-4 shadow-[0_10px_25px_-20px_rgba(15,15,15,0.5)] dark:bg-zinc-900/70">
                Track delivery, errors, and response latency per converter.
              </div>
            </div>
          </div>
          <div className="relative animate-fade-up-delayed">
            <div className="absolute -inset-6 rounded-[36px] border border-dashed border-border/70 bg-[radial-gradient(circle_at_top,_rgba(255,255,255,0.5),_transparent_65%)] dark:bg-[radial-gradient(circle_at_top,_rgba(255,255,255,0.08),_transparent_65%)]" />
            <div className="rounded-3xl border border-border/70 bg-white/85 p-6 shadow-[0_30px_60px_-40px_rgba(15,15,15,0.6)] backdrop-blur dark:bg-zinc-900/70">
              <DemoMapper />
            </div>
          </div>
        </section>

        <section
          id="problem"
          className="space-y-6 scroll-mt-24 rounded-[32px] border border-border/60 bg-white/70 p-8 shadow-[0_30px_70px_-55px_rgba(15,15,15,0.6)] backdrop-blur dark:bg-zinc-900/60 md:p-10"
        >
          <div className="space-y-2">
            <p className="page-kicker">The IT pain we solve</p>
            <h2 className="text-3xl font-semibold">
              Every partner payload looks different.
            </h2>
            <p className="text-sm text-muted-foreground">
              Every partner ships a different payload. Every payload demands a new
              patch. apiconvert centralizes that work without another bespoke
              integration sprint.
            </p>
          </div>
          <div className="grid gap-6 md:grid-cols-2">
            <Card className="border border-border/70 bg-white/90 shadow-[0_20px_40px_-30px_rgba(15,15,15,0.6)] dark:bg-zinc-900/70">
              <CardHeader>
                <CardTitle>Before</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm text-muted-foreground">
                <p>Ticket queues fill up with &ldquo;partner JSON changed&rdquo; alerts.</p>
                <p>Integration code lives in multiple services with no audit trail.</p>
                <p>Ops can&apos;t see which inbound requests fail or why.</p>
              </CardContent>
            </Card>
            <Card className="border border-border/70 bg-white/90 shadow-[0_20px_40px_-30px_rgba(15,15,15,0.6)] dark:bg-zinc-900/70">
              <CardHeader>
                <CardTitle>After</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm text-muted-foreground">
                <p>Mappings are editable without redeploying internal services.</p>
                <p>
                  Incoming payloads are sanitized, tracked, and shipped to internal
                  APIs with confidence.
                </p>
                <p>
                  IT teams have a shared dashboard for deliveries, errors, and
                  authentication.
                </p>
              </CardContent>
            </Card>
          </div>
        </section>

        <section id="examples" className="space-y-10 scroll-mt-24">
          <div className="space-y-3">
            <p className="page-kicker">Mapping examples</p>
            <h2 className="text-3xl font-semibold">Build mappings visually.</h2>
            <p className="text-sm text-muted-foreground">
              Start with example payloads, map fields, and preview the outgoing
              JSON. Keep a record of every request with context.
            </p>
          </div>
          <div className="grid gap-6 md:grid-cols-2">
            <Card className="border border-border/70 bg-white/90 shadow-[0_20px_40px_-30px_rgba(15,15,15,0.6)] dark:bg-zinc-900/70">
              <CardHeader>
                <CardTitle>Field mapping</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm text-muted-foreground">
                <p>Map partner fields to your internal schema.</p>
                <p>Map nested fields (e.g. customer.email → user.contact.email).</p>
                <p>Map with defaults and transforms.</p>
              </CardContent>
            </Card>
            <Card className="border border-border/70 bg-white/90 shadow-[0_20px_40px_-30px_rgba(15,15,15,0.6)] dark:bg-zinc-900/70">
              <CardHeader>
                <CardTitle>Array mapping</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm text-muted-foreground">
                <p>Map arrays with nested paths.</p>
                <p>Map to a clean array of objects with select fields.</p>
                <p>Keep logs for every mapped request.</p>
              </CardContent>
            </Card>
          </div>
        </section>

        <section className="space-y-6 rounded-[32px] border border-border/60 bg-white/70 p-8 shadow-[0_30px_70px_-55px_rgba(15,15,15,0.6)] backdrop-blur dark:bg-zinc-900/60 md:p-10">
          <div className="space-y-2">
            <p className="page-kicker">Delivery control</p>
            <h2 className="text-3xl font-semibold">Forward to internal APIs.</h2>
            <p className="text-sm text-muted-foreground">
              Configure outbound URLs, headers, and authentication. apiconvert
              forwards validated payloads and records each response.
            </p>
          </div>
          <div className="grid gap-6 md:grid-cols-3">
            <Card className="border border-border/70 bg-white/90 shadow-[0_20px_40px_-30px_rgba(15,15,15,0.6)] dark:bg-zinc-900/70">
              <CardHeader>
                <CardTitle>Forwarding rules</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-muted-foreground">
                <p>POST, PUT, PATCH methods supported.</p>
                <p>Custom header forwarding per converter.</p>
                <p>Resilient retries with visibility.</p>
              </CardContent>
            </Card>
            <Card className="border border-border/70 bg-white/90 shadow-[0_20px_40px_-30px_rgba(15,15,15,0.6)] dark:bg-zinc-900/70">
              <CardHeader>
                <CardTitle>Inbound security</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-muted-foreground">
                <p>Per-converter auth tokens and headers.</p>
                <p>Shared secrets or custom header checks.</p>
                <p>Log redaction for sensitive values.</p>
              </CardContent>
            </Card>
            <Card className="border border-border/70 bg-white/90 shadow-[0_20px_40px_-30px_rgba(15,15,15,0.6)] dark:bg-zinc-900/70">
              <CardHeader>
                <CardTitle>Response tracking</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-muted-foreground">
                <p>Response code, latency, and payload logs.</p>
                <p>Replay failures with context.</p>
                <p>Filter logs by converter or org.</p>
              </CardContent>
            </Card>
          </div>
        </section>
      </main>
    </div>
  );
}
