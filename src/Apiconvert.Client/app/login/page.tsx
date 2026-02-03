"use client";

import { useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { SubmitButton } from "@/components/app/SubmitButton";

function isLocalhostOrigin(origin: string, host: string | null) {
  const target = `${origin} ${host ?? ""}`.toLowerCase();
  return target.includes("localhost") || target.includes("127.0.0.1");
}

type OrgListItem = {
  id: string;
};

export default function LoginPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(
    searchParams.get("error") ? decodeURIComponent(searchParams.get("error") as string) : null
  );
  const [pending, setPending] = useState<string | null>(null);

  const origin = useMemo(() => {
    if (typeof window !== "undefined") {
      return window.location.origin;
    }
    return process.env.NEXT_PUBLIC_SITE_URL || "http://localhost:3123";
  }, []);

  const host = useMemo(() => {
    if (typeof window !== "undefined") {
      return window.location.host;
    }
    return null;
  }, []);

  const showEmailAuth = useMemo(() => isLocalhostOrigin(origin, host), [origin, host]);

  async function redirectToOrgOrNew() {
    const response = await apiFetch<{ orgs: OrgListItem[] }>("/api/orgs");
    const orgId = response.orgs?.[0]?.id;
    router.replace(orgId ? `/org/${orgId}/dashboard` : "/org");
  }

  async function handleGithubSignIn(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    setPending("github");
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
      setPending(null);
    }
  }

  async function handleEmailSignIn(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    setPending("email");
    setError(null);

    try {
      if (!showEmailAuth) {
        setError("Email sign-in is only available on localhost.");
        return;
      }

      const formData = new FormData(event.currentTarget);
      const email = String(formData.get("email") || "").trim();
      const password = String(formData.get("password") || "");

      if (!email || !password) {
        setError("Email and password are required.");
        return;
      }

      const supabase = createClient();
      const { error: signInError } = await supabase.auth.signInWithPassword({
        email,
        password,
      });

      if (signInError) {
        setError(signInError.message);
        return;
      }

      await redirectToOrgOrNew();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to sign in.";
      setError(message);
    } finally {
      setPending(null);
    }
  }

  async function handleEmailSignUp(event: React.MouseEvent<HTMLButtonElement>) {
    event.preventDefault();
    if (pending) return;
    setPending("signup");
    setError(null);

    try {
      if (!showEmailAuth) {
        setError("Email sign-up is only available on localhost.");
        return;
      }

      const form = event.currentTarget.form;
      if (!form) return;
      const formData = new FormData(form);
      const email = String(formData.get("email") || "").trim();
      const password = String(formData.get("password") || "");

      if (!email || !password) {
        setError("Email and password are required.");
        return;
      }

      const supabase = createClient();
      const { error: signUpError } = await supabase.auth.signUp({
        email,
        password,
        options: {
          emailRedirectTo: `${origin}/auth/callback`,
        },
      });

      if (signUpError) {
        setError(signUpError.message);
        return;
      }

      await redirectToOrgOrNew();
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to sign up.";
      setError(message);
    } finally {
      setPending(null);
    }
  }

  return (
    <div className="page-shell flex items-center justify-center px-6 py-16">
      <div className="absolute inset-0 bg-[url('/grid.svg')] opacity-15" />
      <div className="relative w-full max-w-md rounded-3xl border border-border/70 bg-white/85 p-10 shadow-[0_30px_60px_-40px_rgba(15,15,15,0.6)] backdrop-blur dark:bg-zinc-900/80">
        <div className="mb-8 space-y-3">
          <p className="page-kicker">apiconvert</p>
          <h1 className="page-title text-3xl text-foreground">
            Build dependable API converters.
          </h1>
          <p className="page-lede">
            Sign in to create inbound endpoints, map payloads, and track delivery
            metrics for every organization.
          </p>
        </div>
        <div className="space-y-3">
          <form onSubmit={handleGithubSignIn}>
            <SubmitButton
              className="w-full"
              type="submit"
              pending={pending === "github"}
              pendingLabel="Signing in..."
            >
              Continue with GitHub
            </SubmitButton>
          </form>
          {showEmailAuth ? (
            <div className="space-y-3 rounded-2xl border border-border/70 bg-white/70 p-4 dark:bg-zinc-900/60">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                Email login
              </p>
              <form onSubmit={handleEmailSignIn} className="space-y-3">
                <div className="space-y-2">
                  <label
                    className="text-xs font-medium text-muted-foreground"
                    htmlFor="email"
                  >
                    Email
                  </label>
                  <input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    className="w-full rounded-xl border border-border/70 bg-white px-3 py-2 text-sm text-foreground shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/40 dark:bg-zinc-950/60"
                  />
                </div>
                <div className="space-y-2">
                  <label
                    className="text-xs font-medium text-muted-foreground"
                    htmlFor="password"
                  >
                    Password
                  </label>
                  <input
                    id="password"
                    name="password"
                    type="password"
                    autoComplete="current-password"
                    className="w-full rounded-xl border border-border/70 bg-white px-3 py-2 text-sm text-foreground shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/40 dark:bg-zinc-950/60"
                  />
                </div>
                <SubmitButton
                  className="w-full"
                  type="submit"
                  pending={pending === "email"}
                  pendingLabel="Signing in..."
                >
                  Sign in with email
                </SubmitButton>
                <SubmitButton
                  className="w-full"
                  type="button"
                  variant="ghost"
                  pending={pending === "signup"}
                  pendingLabel="Creating account..."
                  onClick={handleEmailSignUp}
                >
                  Create email account
                </SubmitButton>
              </form>
            </div>
          ) : null}
        </div>
        {error ? <p className="mt-4 text-sm text-destructive">{error}</p> : null}
      </div>
    </div>
  );
}
