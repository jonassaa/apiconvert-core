"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { FlashToast } from "@/components/app/FlashToast";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { DeleteConverterButton } from "@/components/app/DeleteConverterButton";
import { AutoSubmitForm } from "@/components/app/AutoSubmitForm";
import { SelectField } from "@/components/app/SelectField";
import { normalizeConverterNameForUrl } from "@/lib/converters";
import { NotFoundState } from "@/components/app/not-found-state";

type ConverterSummary = {
  id: string;
  name: string;
  enabled: boolean;
  logRequestsEnabled: boolean;
  forwardUrl: string;
};

export default function ConvertersPage() {
  const params = useParams<{ orgId: string }>();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [converters, setConverters] = useState<ConverterSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deletePending, setDeletePending] = useState<Record<string, boolean>>({});

  const searchTerm = searchParams.get("q")?.trim() ?? "";
  const enabledValue = searchParams.get("enabled") || "";
  const loggingValue = searchParams.get("logging") || "";

  const queryString = useMemo(() => {
    const params = new URLSearchParams();
    if (searchTerm) params.set("search", searchTerm);
    if (enabledValue) params.set("enabled", enabledValue);
    if (loggingValue) params.set("logging", loggingValue);
    const qs = params.toString();
    return qs ? `?${qs}` : "";
  }, [enabledValue, loggingValue, searchTerm]);

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

        const response = await apiFetch<{ converters: ConverterSummary[] }>(
          `/api/orgs/${params.orgId}/converters${queryString}`
        );
        if (!isActive) return;
        setConverters(response.converters ?? []);
        setError(null);
      } catch (err) {
        if (!isActive) return;
        const message = err instanceof Error ? err.message : "Failed to load converters";
        setError(message);
      } finally {
        if (isActive) setLoading(false);
      }
    }

    load();
    return () => {
      isActive = false;
    };
  }, [params.orgId, queryString, router]);

  const handleFilterSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const nextSearch = String(formData.get("q") || "").trim();
    const nextEnabled = String(formData.get("enabled") || "");
    const nextLogging = String(formData.get("logging") || "");
    const nextParams = new URLSearchParams();
    if (nextSearch) nextParams.set("q", nextSearch);
    if (nextEnabled) nextParams.set("enabled", nextEnabled);
    if (nextLogging) nextParams.set("logging", nextLogging);
    const qs = nextParams.toString();
    setLoading(true);
    router.replace(
      `/org/${params.orgId}/converters${qs ? `?${qs}` : ""}`
    );
  };

  const handleDelete = async (converterId: string) => {
    if (deletePending[converterId]) return;
    setDeletePending((prev) => ({ ...prev, [converterId]: true }));
    setError(null);
    try {
      await apiFetch(`/api/orgs/${params.orgId}/converters/${converterId}`, {
        method: "DELETE",
      });
      setConverters((prev) => prev.filter((item) => item.id !== converterId));
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to delete converter";
      setError(message);
    } finally {
      setDeletePending((prev) => ({ ...prev, [converterId]: false }));
    }
  };

  const normalizedError = (error ?? "").toLowerCase();
  const isNotFound =
    normalizedError.includes("not found") ||
    normalizedError.includes("not a member of this organization");

  return (
    <div className="space-y-6">
      <FlashToast message={searchParams.get("success") ?? undefined} />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="page-kicker">Converters</p>
          <h1 className="page-title text-3xl">Converters</h1>
          <p className="page-lede">
            Manage inbound endpoints and forward destinations.
          </p>
        </div>
        <Button asChild>
          <Link href={`/org/${params.orgId}/converters/new`}>Create converter</Link>
        </Button>
      </div>

      {isNotFound ? (
        <NotFoundState />
      ) : (
        <Card>
        <CardHeader className="space-y-2">
          <CardTitle>Search & filters</CardTitle>
          <AutoSubmitForm
            className="flex flex-wrap gap-3"
            onSubmit={handleFilterSubmit}
          >
            <Input
              name="q"
              placeholder="Search name or URL"
              defaultValue={searchTerm}
              className="w-60"
            />
            <SelectField
              name="enabled"
              defaultValue={enabledValue || "__all__"}
              triggerClassName="w-[160px]"
              options={[
                { value: "__all__", label: "All statuses", formValue: "" },
                { value: "true", label: "Enabled" },
                { value: "false", label: "Disabled" },
              ]}
            />
            <SelectField
              name="logging"
              defaultValue={loggingValue || "__all__"}
              triggerClassName="w-[160px]"
              options={[
                { value: "__all__", label: "All logging", formValue: "" },
                { value: "true", label: "Logging on" },
                { value: "false", label: "Logging off" },
              ]}
            />
            <Button asChild variant="ghost">
              <Link href={`/org/${params.orgId}/converters`}>Clear</Link>
            </Button>
          </AutoSubmitForm>
        </CardHeader>
        <CardContent>
          {error ? (
            <p className="mb-3 text-sm text-destructive">{error}</p>
          ) : null}
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Logging</TableHead>
                <TableHead>Forward URL</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-sm text-muted-foreground">
                    Loading converters...
                  </TableCell>
                </TableRow>
              ) : converters.length ? (
                converters.map((converter) => (
                  <TableRow key={converter.id}>
                    <TableCell>
                      <Link
                        className="font-medium underline"
                        href={`/org/${params.orgId}/converters/${normalizeConverterNameForUrl(
                          converter.name
                        )}`}
                      >
                        {converter.name}
                      </Link>
                    </TableCell>
                    <TableCell>
                      <Badge variant={converter.enabled ? "default" : "outline"}>
                        {converter.enabled ? "Enabled" : "Disabled"}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant={
                          converter.logRequestsEnabled ? "secondary" : "outline"
                        }
                      >
                        {converter.logRequestsEnabled ? "On" : "Off"}
                      </Badge>
                    </TableCell>
                    <TableCell className="max-w-[240px] truncate text-sm text-muted-foreground">
                      {converter.forwardUrl}
                    </TableCell>
                    <TableCell className="text-right">
                      <DeleteConverterButton
                        onConfirm={() => handleDelete(converter.id)}
                      />
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={5} className="text-sm text-muted-foreground">
                    No converters found yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
      )}
    </div>
  );
}
