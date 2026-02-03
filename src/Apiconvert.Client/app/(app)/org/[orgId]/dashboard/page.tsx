"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams, useSearchParams, useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { normalizeConverterNameForUrl } from "@/lib/converters";
import { FlashToast } from "@/components/app/FlashToast";
import { NotFoundState } from "@/components/app/not-found-state";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

function formatNumber(value: number) {
  return Intl.NumberFormat("en-US").format(value);
}

function formatTimestamp(value: Date) {
  return value.toLocaleString();
}

type OrgDashboard = {
  metrics: {
    requests24h: number;
    requests7d: number;
    success7d: number;
    avgResponseMs: number;
  };
  recentConverters: { id: string; name: string; createdAt: string }[];
  recentMembers: { id: string; role: string; createdAt: string }[];
  recentInvites: { id: string; email: string; createdAt: string; acceptedAt: string | null }[];
  recentLogs: {
    receivedAt: string;
    requestId: string;
    forwardStatus: number | null;
    forwardResponseMs: number | null;
    converterName: string | null;
  }[];
};

type ActivityItem = {
  id: string;
  timestamp: Date;
  action: string;
  subject?: string;
  href?: string;
  meta?: string;
};

export default function DashboardPage() {
  const params = useParams<{ orgId: string }>();
  const searchParams = useSearchParams();
  const router = useRouter();
  const [dashboard, setDashboard] = useState<OrgDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

        const response = await apiFetch<{ dashboard: OrgDashboard }>(
          `/api/orgs/${params.orgId}/dashboard`
        );
        if (!isActive) return;
        setDashboard(response.dashboard);
      } catch (err) {
        if (!isActive) return;
        const message = err instanceof Error ? err.message : "Failed to load dashboard";
        const normalized = message.toLowerCase();
        const displayMessage =
          normalized.includes("not found") ||
          normalized.includes("not a member of this organization")
            ? "Organization not found."
            : message;
        setError(displayMessage);
      } finally {
        if (isActive) setLoading(false);
      }
    }

    load();
    return () => {
      isActive = false;
    };
  }, [params.orgId, router]);

  const recentActivity = useMemo<ActivityItem[]>(() => {
    if (!dashboard) return [];
    const activity: ActivityItem[] = [
      ...(dashboard.recentConverters ?? []).map((converter) => ({
        id: `converter-${converter.id}`,
        timestamp: new Date(converter.createdAt),
        action: "Converter created",
        subject: converter.name,
        href: `/org/${params.orgId}/converters/${normalizeConverterNameForUrl(
          converter.name
        )}`,
      })),
      ...(dashboard.recentMembers ?? []).map((member) => ({
        id: `member-${member.id}`,
        timestamp: new Date(member.createdAt),
        action: "Member joined",
        meta: member.role,
      })),
      ...(dashboard.recentInvites ?? []).map((invite) => ({
        id: `invite-${invite.id}`,
        timestamp: new Date(invite.acceptedAt ?? invite.createdAt),
        action: invite.acceptedAt ? "Invite accepted" : "Invite sent",
        subject: invite.email,
      })),
    ];
    return activity
      .filter((item) => !Number.isNaN(item.timestamp.getTime()))
      .sort((a, b) => b.timestamp.getTime() - a.timestamp.getTime())
      .slice(0, 10);
  }, [dashboard, params.orgId]);

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <p className="page-kicker">Dashboard</p>
          <p className="page-lede">
            Monitor your inbound traffic and forward performance.
          </p>
        </div>
        <div className="rounded-2xl border border-dashed border-border/70 bg-white/60 p-6 text-sm text-muted-foreground dark:bg-zinc-950/40">
          Loading dashboard…
        </div>
      </div>
    );
  }

  if (!dashboard) {
    const normalizedError = (error ?? "").toLowerCase();
    const isNotFound =
      normalizedError.includes("not found") ||
      normalizedError.includes("not a member of this organization");
    if (isNotFound) {
      return (
        <div className="space-y-6">
          <div>
            <p className="page-kicker">Dashboard</p>
            <p className="page-lede">
              Monitor your inbound traffic and forward performance.
            </p>
          </div>
          <NotFoundState />
        </div>
      );
    }
    return (
      <div className="space-y-6">
        <div>
          <p className="page-kicker">Dashboard</p>
          <p className="page-lede">
            Monitor your inbound traffic and forward performance.
          </p>
        </div>
        <p className="text-sm text-destructive">
          {error ?? "Unable to load dashboard."}
        </p>
      </div>
    );
  }

  const requests24 = Number(dashboard.metrics.requests24h ?? 0);
  const requests7d = Number(dashboard.metrics.requests7d ?? 0);
  const successCount = Number(dashboard.metrics.success7d ?? 0);
  const avgResponse = Math.round(Number(dashboard.metrics.avgResponseMs ?? 0));

  return (
    <div className="space-y-6">
      <FlashToast message={searchParams.get("success") ?? undefined} />
      <div>
        <p className="page-kicker">Dashboard</p>
        <p className="page-lede">
          Monitor your inbound traffic and forward performance.
        </p>
      </div>
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm text-muted-foreground">
              Requests (24h)
            </CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">
            {formatNumber(requests24)}
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm text-muted-foreground">
              Requests (7d)
            </CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">
            {formatNumber(requests7d)}
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm text-muted-foreground">
              Success rate
            </CardTitle>
          </CardHeader>
          <CardContent className="flex items-baseline gap-2 text-2xl font-semibold">
            {requests7d === 0
              ? "0%"
              : `${Math.round((successCount / requests7d) * 100)}%`}
            <Badge variant="secondary" className="text-xs">
              last 7d
            </Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm text-muted-foreground">
              Avg response
            </CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">
            {avgResponse}ms
          </CardContent>
        </Card>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>Recent activity</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {recentActivity.length ? (
            recentActivity.map((item) => (
              <div
                key={item.id}
                className="flex flex-wrap items-center justify-between gap-3 text-sm"
              >
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-medium">{item.action}</span>
                  {item.subject ? (
                    item.href ? (
                      <Link className="underline" href={item.href}>
                        {item.subject}
                      </Link>
                    ) : (
                      <span>{item.subject}</span>
                    )
                  ) : null}
                  {item.meta ? (
                    <Badge variant="secondary" className="text-xs">
                      {item.meta}
                    </Badge>
                  ) : null}
                </div>
                <span className="text-xs text-muted-foreground">
                  {formatTimestamp(item.timestamp)}
                </span>
              </div>
            ))
          ) : (
            <p className="text-sm text-muted-foreground">
              No activity yet.
            </p>
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Recent requests</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Converter</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Latency</TableHead>
                <TableHead>Received</TableHead>
                <TableHead>Request ID</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {dashboard.recentLogs.length ? (
                dashboard.recentLogs.map((log) => (
                  <TableRow key={log.requestId}>
                    <TableCell>
                      {log.converterName ?? "Unknown"}
                    </TableCell>
                    <TableCell>{log.forwardStatus ?? "-"}</TableCell>
                    <TableCell>
                      {log.forwardResponseMs ? `${log.forwardResponseMs}ms` : "-"}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {new Date(log.receivedAt).toLocaleString()}
                    </TableCell>
                    <TableCell className="font-mono text-xs">
                      {log.requestId}
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={5} className="text-sm text-muted-foreground">
                    No recent requests yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
