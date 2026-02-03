"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";

export default function AuthCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isActive = true;

    async function handleCallback() {
      try {
        const code = searchParams.get("code");
        if (!code) {
          router.replace("/");
          return;
        }

        const supabase = createClient();
        const { error: exchangeError } = await supabase.auth.exchangeCodeForSession(code);
        if (exchangeError) {
          setError(exchangeError.message);
          return;
        }

        const { data } = await supabase.auth.getUser();
        if (!data.user) {
          router.replace("/login");
          return;
        }

        const response = await apiFetch<{ orgs: { id: string }[] }>("/api/orgs");
        const orgId = response.orgs?.[0]?.id;
        router.replace(orgId ? `/org/${orgId}/dashboard` : "/org");
      } catch (err) {
        const message = err instanceof Error ? err.message : "Failed to sign in.";
        if (isActive) setError(message);
      }
    }

    handleCallback();
    return () => {
      isActive = false;
    };
  }, [router, searchParams]);

  return (
    <div className="page-shell flex items-center justify-center px-6 py-16">
      <div className="rounded-2xl border border-border/70 bg-white/80 p-6 text-sm text-muted-foreground shadow-[0_20px_40px_-30px_rgba(15,15,15,0.6)] dark:bg-zinc-900/70">
        {error ? <p className="text-destructive">{error}</p> : <p>Signing you in...</p>}
      </div>
    </div>
  );
}
