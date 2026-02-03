"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiFetch, getApiBaseUrl } from "@/lib/api-client";
import { createClient } from "@/lib/supabase/client";
import { SubmitButton } from "@/components/app/SubmitButton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const formatDate = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString();
};

type InviteDetails = {
  id: string;
  orgId: string;
  orgName: string;
  email: string;
  role: string;
  expiresAt: string;
  acceptedAt: string | null;
};

export default function InvitePage({ params }: { params: { token: string } }) {
  const router = useRouter();
  const [invite, setInvite] = useState<InviteDetails | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);

  const token = params.token;

  useEffect(() => {
    let isActive = true;

    async function loadInvite() {
      try {
        const baseUrl = getApiBaseUrl();
        if (!baseUrl) {
          throw new Error("API base URL is not configured.");
        }

        const response = await fetch(`${baseUrl}/api/invites/${token}`);
        if (!response.ok) {
          throw new Error("Invite not found or expired.");
        }
        const data = (await response.json()) as { invite: InviteDetails };
        if (!isActive) return;
        setInvite(data.invite);
        setError(null);
      } catch (err) {
        if (!isActive) return;
        const message = err instanceof Error ? err.message : "Invite not found.";
        setError(message);
      } finally {
        if (isActive) setLoading(false);
      }
    }

    loadInvite();
    return () => {
      isActive = false;
    };
  }, [token]);

  const isExpired = useMemo(() => {
    if (!invite?.expiresAt) return false;
    return new Date(invite.expiresAt).getTime() < Date.now();
  }, [invite]);

  async function handleAcceptInvite(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;
    setPending(true);
    setError(null);

    try {
      const supabase = createClient();
      const { data } = await supabase.auth.getUser();
      if (!data.user) {
        router.replace("/login");
        return;
      }

      const response = await apiFetch<{ orgId: string }>(`/api/invites/${token}/accept`, {
        method: "POST",
      });
      router.replace(`/org/${response.orgId}/dashboard`);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to accept invite.";
      setError(message);
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="page-shell flex items-center justify-center px-6 py-16">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-2xl">Join organization</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm text-muted-foreground">
          {loading ? <p>Loading invite...</p> : null}
          {!loading && invite ? (
            <>
              <p>
                You have been invited to{" "}
                <span className="font-medium text-foreground">
                  {invite.orgName || "an organization"}
                </span>{" "}
                as {invite.role}.
              </p>
              <p>Invitee: {invite.email}</p>
              <p>Expires: {formatDate(invite.expiresAt)}</p>
              {invite.acceptedAt ? (
                <p>This invite has already been accepted.</p>
              ) : isExpired ? (
                <p>This invite has expired.</p>
              ) : (
                <form onSubmit={handleAcceptInvite}>
                  <SubmitButton type="submit" pending={pending} pendingLabel="Accepting...">
                    Accept invite
                  </SubmitButton>
                </form>
              )}
            </>
          ) : null}
          {!loading && !invite ? <p>{error ?? "Invite not found or expired."}</p> : null}
          {error && invite ? <p className="text-destructive">{error}</p> : null}
        </CardContent>
      </Card>
    </div>
  );
}
