"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { FlashToast } from "@/components/app/FlashToast";
import { SubmitButton } from "@/components/app/SubmitButton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { SelectField } from "@/components/app/SelectField";
import { NotFoundState } from "@/components/app/not-found-state";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

type OrgSettings = {
  org: { id: string; name: string; slug: string };
  userRole: string;
  canManage: boolean;
  isOwner: boolean;
  members: {
    userId: string;
    role: string;
    name: string | null;
    email: string | null;
  }[];
  invites: {
    id: string;
    email: string;
    role: string;
    token: string;
    expiresAt: string;
    acceptedAt: string | null;
  }[];
};

export default function OrgSettingsPage() {
  const params = useParams<{ orgId: string }>();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [settings, setSettings] = useState<OrgSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [pending, setPending] = useState<Record<string, boolean>>({});
  const [roleEdits, setRoleEdits] = useState<Record<string, string>>({});

  useEffect(() => {
    let isActive = true;
    const supabase = createClient();

    async function load() {
      try {
        const { data } = await supabase.auth.getSession();
        if (!data.session) {
          router.replace("/login");
          return;
        }

        const response = await apiFetch<{ settings: OrgSettings }>(
          `/api/orgs/${params.orgId}/settings`
        );
        if (!isActive) return;
        setSettings(response.settings);
        setError(null);
      } catch (err) {
        if (!isActive) return;
        const message = err instanceof Error ? err.message : "Failed to load settings";
        setError(message);
      } finally {
        if (isActive) setLoading(false);
      }
    }

    load();
    return () => {
      isActive = false;
    };
  }, [params.orgId, router]);

  useEffect(() => {
    const incoming = searchParams.get("success");
    if (incoming) {
      setSuccess(incoming);
    }
  }, [searchParams]);

  const ownerCount = useMemo(() => {
    return settings?.members.filter((member) => member.role === "owner").length ?? 0;
  }, [settings?.members]);

  const canChangeMemberRole = (currentRole: string, nextRole: string) => {
    if (currentRole === "owner" && nextRole !== "owner" && ownerCount <= 1) {
      return false;
    }
    return true;
  };

  const canRemoveMember = (currentRole: string) => {
    if (currentRole === "owner" && ownerCount <= 1) {
      return false;
    }
    return true;
  };

  const setPendingKey = (key: string, value: boolean) => {
    setPending((prev) => ({ ...prev, [key]: value }));
  };

  const handleUpdateOrg = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!settings) return;
    if (pending.updateOrg) return;
    setPendingKey("updateOrg", true);
    setError(null);
    setSuccess(null);

    try {
      const formData = new FormData(event.currentTarget);
      const name = String(formData.get("name") || "").trim();
      if (!name) {
        setError("Name is required.");
        return;
      }

      await apiFetch(`/api/orgs/${settings.org.id}`, {
        method: "PATCH",
        body: { name },
      });
      setSettings((prev) => (prev ? { ...prev, org: { ...prev.org, name } } : prev));
      setSuccess("Organization updated");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to update organization";
      setError(message);
    } finally {
      setPendingKey("updateOrg", false);
    }
  };

  const handleInvite = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!settings) return;
    if (pending.invite) return;
    setPendingKey("invite", true);
    setError(null);
    setSuccess(null);

    try {
      const formData = new FormData(event.currentTarget);
      const email = String(formData.get("email") || "").trim();
      const role = String(formData.get("role") || "member");
      if (!email) {
        setError("Email is required.");
        return;
      }

      const response = await apiFetch<{ invite: OrgSettings["invites"][number] }>(
        `/api/orgs/${settings.org.id}/invites`,
        {
          method: "POST",
          body: { email, role },
        }
      );
      setSettings((prev) =>
        prev
          ? { ...prev, invites: [response.invite, ...prev.invites] }
          : prev
      );
      event.currentTarget.reset();
      setSuccess("Invite created");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to create invite";
      setError(message);
    } finally {
      setPendingKey("invite", false);
    }
  };

  const handleMemberRoleChange = async (memberId: string) => {
    if (!settings) return;
    const nextRole = roleEdits[memberId];
    if (!nextRole) return;
    if (pending[`member-${memberId}`]) return;
    setPendingKey(`member-${memberId}`, true);
    setError(null);
    setSuccess(null);

    try {
      await apiFetch(`/api/orgs/${settings.org.id}/members/${memberId}`, {
        method: "PATCH",
        body: { role: nextRole },
      });
      setSettings((prev) =>
        prev
          ? {
              ...prev,
              members: prev.members.map((member) =>
                member.userId === memberId ? { ...member, role: nextRole } : member
              ),
            }
          : prev
      );
      setSuccess("Member updated");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to update member";
      setError(message);
    } finally {
      setPendingKey(`member-${memberId}`, false);
    }
  };

  const handleRemoveMember = async (memberId: string) => {
    if (!settings) return;
    if (pending[`remove-${memberId}`]) return;
    setPendingKey(`remove-${memberId}`, true);
    setError(null);
    setSuccess(null);

    try {
      await apiFetch(`/api/orgs/${settings.org.id}/members/${memberId}`, {
        method: "DELETE",
      });
      setSettings((prev) =>
        prev
          ? {
              ...prev,
              members: prev.members.filter((member) => member.userId !== memberId),
            }
          : prev
      );
      setSuccess("Member removed");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to remove member";
      setError(message);
    } finally {
      setPendingKey(`remove-${memberId}`, false);
    }
  };

  const handleDeleteOrg = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!settings) return;
    if (pending.deleteOrg) return;
    setPendingKey("deleteOrg", true);
    setError(null);
    setSuccess(null);

    const formData = new FormData(event.currentTarget);
    const confirm = String(formData.get("confirm") || "").trim();
    if (confirm !== "DELETE") {
      setError("Type DELETE to confirm");
      setPendingKey("deleteOrg", false);
      return;
    }

    try {
      await apiFetch(`/api/orgs/${settings.org.id}`, { method: "DELETE" });
      router.replace("/org?success=" + encodeURIComponent("Organization deleted"));
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete organization";
      setError(message);
      setPendingKey("deleteOrg", false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <p className="page-kicker">Settings</p>
          <h1 className="page-title text-3xl">Organization settings</h1>
          <p className="page-lede">
            Manage members, invites, and organization details.
          </p>
        </div>
        <div className="rounded-2xl border border-dashed border-border/70 bg-white/60 p-6 text-sm text-muted-foreground dark:bg-zinc-950/40">
          Loading settings…
        </div>
      </div>
    );
  }

  if (!settings) {
    const normalizedError = (error ?? "").toLowerCase();
    const isNotFound =
      normalizedError.includes("not found") ||
      normalizedError.includes("not a member of this organization");
    if (isNotFound) {
      return (
        <div className="space-y-6">
          <div>
            <p className="page-kicker">Settings</p>
            <h1 className="page-title text-3xl">Organization settings</h1>
            <p className="page-lede">
              Manage members, invites, and organization details.
            </p>
          </div>
          <NotFoundState />
        </div>
      );
    }
    return (
      <div className="space-y-6">
        <div>
          <p className="page-kicker">Settings</p>
          <h1 className="page-title text-3xl">Organization settings</h1>
          <p className="page-lede">
            Manage members, invites, and organization details.
          </p>
        </div>
        <p className="text-sm text-destructive">
          {error ?? "Unable to load settings."}
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <FlashToast message={success ?? undefined} />
      <div>
        <p className="page-kicker">Settings</p>
        <h1 className="page-title text-3xl">Organization settings</h1>
        <p className="page-lede">
          Manage members, invites, and organization details.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Organization profile</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleUpdateOrg} className="grid gap-4">
            <div className="space-y-2">
              <Label htmlFor="name">Organization name</Label>
              <Input id="name" name="name" defaultValue={settings.org.name} />
            </div>
            <SubmitButton
              type="submit"
              disabled={!settings.canManage}
              pendingLabel="Saving..."
              pending={pending.updateOrg}
            >
              Save changes
            </SubmitButton>
          </form>
        </CardContent>
      </Card>

      {error ? (
        <p className="text-sm text-destructive">
          {error}
        </p>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Invite member</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleInvite} className="grid gap-4 md:grid-cols-3">
            <div className="space-y-2 md:col-span-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                name="email"
                type="email"
                required
                disabled={!settings.canManage}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="role">Role</Label>
              <SelectField
                id="role"
                name="role"
                defaultValue="member"
                disabled={!settings.canManage}
                options={[
                  { value: "member", label: "Member" },
                  { value: "admin", label: "Admin" },
                  { value: "owner", label: "Owner" },
                ]}
              />
            </div>
            <div className="md:col-span-3">
              <SubmitButton
                type="submit"
                disabled={!settings.canManage}
                pendingLabel="Creating..."
                pending={pending.invite}
              >
                Create invite
              </SubmitButton>
            </div>
          </form>
          {!settings.canManage ? (
            <p className="mt-2 text-xs text-muted-foreground">
              Only owners and admins can manage invites.
            </p>
          ) : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Members</CardTitle>
        </CardHeader>
        <CardContent className="text-sm">
          {settings.members.length ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {settings.members.map((member) => {
                  const isLastOwner = member.role === "owner" && ownerCount <= 1;
                  const displayName = member.name || member.email || "Unknown user";
                  const email = member.email ?? "—";
                  const roleValue = roleEdits[member.userId] ?? member.role;
                  return (
                    <TableRow key={`${member.userId}-${member.role}`}>
                      <TableCell className="font-medium">{displayName}</TableCell>
                      <TableCell className="text-muted-foreground">
                        {email}
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap items-center gap-2">
                          <SelectField
                            name={`role-${member.userId}`}
                            value={roleValue}
                            disabled={!settings.canManage}
                            onValueChange={(value) =>
                              setRoleEdits((prev) => ({
                                ...prev,
                                [member.userId]: value,
                              }))
                            }
                            options={[
                              { value: "member", label: "Member" },
                              { value: "admin", label: "Admin" },
                              { value: "owner", label: "Owner" },
                            ]}
                          />
                          <SubmitButton
                            type="button"
                            size="sm"
                            variant="secondary"
                            pendingLabel="Saving..."
                            pending={pending[`member-${member.userId}`]}
                            disabled={
                              !settings.canManage ||
                              !canChangeMemberRole(member.role, roleValue) ||
                              roleValue === member.role
                            }
                            onClick={() => handleMemberRoleChange(member.userId)}
                          >
                            Update
                          </SubmitButton>
                        </div>
                      </TableCell>
                      <TableCell className="text-right">
                        <SubmitButton
                          type="button"
                          size="sm"
                          variant="ghost"
                          pendingLabel="Removing..."
                          pending={pending[`remove-${member.userId}`]}
                          disabled={
                            !settings.canManage ||
                            !canRemoveMember(member.role) ||
                            isLastOwner
                          }
                          onClick={() => handleRemoveMember(member.userId)}
                        >
                          Remove
                        </SubmitButton>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          ) : (
            <p className="text-sm text-muted-foreground">
              No members yet.
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Active invites</CardTitle>
        </CardHeader>
        <CardContent className="text-sm">
          {settings.invites.length ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {settings.invites.map((invite) => (
                  <TableRow key={invite.id}>
                    <TableCell className="font-medium">{invite.email}</TableCell>
                    <TableCell className="capitalize">{invite.role}</TableCell>
                    <TableCell>
                      {invite.acceptedAt
                        ? "Accepted"
                        : new Date(invite.expiresAt).getTime() < Date.now()
                        ? "Expired"
                        : "Pending"}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          ) : (
            <p className="text-sm text-muted-foreground">No invites yet.</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Delete organization</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm text-muted-foreground">
          <p>
            Delete this organization and all related data. This action cannot be
            undone.
          </p>
          <form onSubmit={handleDeleteOrg} className="grid gap-3">
            <div className="space-y-2">
              <Label htmlFor="confirm">Type DELETE to confirm</Label>
              <Input id="confirm" name="confirm" />
            </div>
            <SubmitButton
              type="submit"
              variant="destructive"
              disabled={!settings.isOwner}
              pendingLabel="Deleting..."
              pending={pending.deleteOrg}
            >
              Delete organization
            </SubmitButton>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
