"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { createClient } from "@/lib/supabase/client";
import { apiFetch, getApiBaseUrl } from "@/lib/api-client";
import { FlashToast } from "@/components/app/FlashToast";
import { ConverterAuthForm } from "@/components/app/ConverterAuthForm";
import { SubmitButton } from "@/components/app/SubmitButton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { ConversionEditor } from "@/components/app/ConversionEditor";
import { SelectField } from "@/components/app/SelectField";
import { FieldLabel } from "@/components/app/FieldLabel";
import { TooltipIcon } from "@/components/app/TooltipIcon";
import { CopyableInput } from "@/components/app/copyable-input";
import { normalizeConverterNameParam } from "@/lib/converters";
import { normalizeInboundPath } from "@/lib/inbound";
import type { ConversionRules } from "@/lib/mapping/engine";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { NotFoundState } from "@/components/app/not-found-state";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

type ConverterDetail = {
  id: string;
  name: string;
  inboundPath: string;
  enabled: boolean;
  forwardUrl: string;
  forwardMethod: string | null;
  forwardHeaders: Record<string, string>;
  logRequestsEnabled: boolean;
  inboundSecretLast4: string | null;
  inboundAuthMode: string | null;
  inboundAuthHeaderName: string | null;
  inboundAuthUsername: string | null;
  inboundAuthValueLast4: string | null;
  logRetentionDays: number | null;
  logBodyMaxBytes: number | null;
  logHeadersMaxBytes: number | null;
  logRedactSensitiveHeaders: boolean | null;
  inboundResponseMode: string | null;
};

type LogSummary = {
  receivedAt: string;
  forwardStatus: number | null;
  forwardResponseMs: number | null;
  requestId: string;
};

type DetailResponse = {
  converter: ConverterDetail;
  mapping: unknown | null;
  inputSample: string | null;
  outputSample: string | null;
  logs: LogSummary[];
};

function toNumber(value: FormDataEntryValue | null) {
  if (value === null || value === "") return undefined;
  const numberValue = Number(value);
  return Number.isNaN(numberValue) ? undefined : numberValue;
}

function toStringValue(value: FormDataEntryValue | null) {
  const text = String(value ?? "").trim();
  return text.length ? text : undefined;
}

export default function ConverterDetailPage() {
  const params = useParams<{ orgId: string; name: string }>();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [detail, setDetail] = useState<DetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState<Record<string, boolean>>({});
  const [success, setSuccess] = useState<string | null>(null);

  const normalizedName = useMemo(
    () => normalizeConverterNameParam(params.name),
    [params.name]
  );

  const loadDetail = async () => {
    const response = await apiFetch<DetailResponse>(
      `/api/orgs/${params.orgId}/converters/lookup?name=${encodeURIComponent(
        normalizedName
      )}`
    );
    setDetail(response);
  };

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
        await loadDetail();
        if (!isActive) return;
        setError(null);
      } catch (err) {
        if (!isActive) return;
        const message = err instanceof Error ? err.message : "Failed to load converter";
        setError(message);
      } finally {
        if (isActive) setLoading(false);
      }
    }

    load();
    return () => {
      isActive = false;
    };
  }, [normalizedName, params.orgId, router]);

  useEffect(() => {
    const incoming = searchParams.get("success");
    if (incoming) {
      setSuccess(incoming);
    }
  }, [searchParams]);

  const inboundUrl = useMemo(() => {
    if (!detail) return "";
    const inboundPath = normalizeInboundPath(detail.converter.inboundPath);
    const apiBaseUrl = getApiBaseUrl();
    if (apiBaseUrl) {
      return `${apiBaseUrl.replace(/\/$/, "")}/api/inbound/${params.orgId}/${inboundPath}`;
    }
    if (typeof window !== "undefined") {
      return `${window.location.origin}/api/inbound/${params.orgId}/${inboundPath}`;
    }
    return "";
  }, [detail, params.orgId]);

  const inboundAuthMode = detail?.converter.inboundAuthMode ??
    (detail?.converter.inboundSecretLast4 ? "bearer" : "none");
  const inboundAuthLast4 =
    detail?.converter.inboundAuthValueLast4 ?? detail?.converter.inboundSecretLast4;

  const forwardHeaders = detail?.converter.forwardHeaders ?? {};
  const authHeaderEntry = Object.entries(forwardHeaders).find(
    ([key]) => key.toLowerCase() === "authorization"
  );
  const authHeaderValue = authHeaderEntry?.[1] ?? "";
  const outboundAuthMode = authHeaderValue.startsWith("Bearer ")
    ? "bearer"
    : authHeaderValue.startsWith("Basic ")
    ? "basic"
    : "none";
  const outboundCustomHeaderEntry = Object.entries(forwardHeaders).find(
    ([key]) => key.toLowerCase() !== "authorization"
  );
  const outboundCustomHeaderName = outboundCustomHeaderEntry?.[0] ?? "";
  const outboundCustomHeaderValue = outboundCustomHeaderEntry?.[1] ?? "";

  const handleSettingsSubmit = async (formData: FormData) => {
    if (!detail) return;
    if (pending.settings) return;
    setPending((prev) => ({ ...prev, settings: true }));
    setError(null);
    setSuccess(null);

    const payload = {
      name: toStringValue(formData.get("name")),
      inboundPath: toStringValue(formData.get("inbound_path")),
      forwardUrl: toStringValue(formData.get("forward_url")),
      forwardMethod: toStringValue(formData.get("forward_method")),
      forwardHeadersJson: toStringValue(formData.get("forward_headers_json")),
      enabled: formData.get("enabled") === "on",
      logRequestsEnabled: formData.get("log_requests_enabled") === "on",
      logRetentionDays: toNumber(formData.get("log_retention_days")),
      logBodyMaxKb: toNumber(formData.get("log_body_max_kb")),
      logHeadersMaxKb: toNumber(formData.get("log_headers_max_kb")),
      logRedactSensitiveHeaders:
        formData.get("log_redact_sensitive_headers") === "on",
      inboundResponseMode: toStringValue(formData.get("inbound_response_mode")),
    };

    try {
      await apiFetch(`/api/orgs/${params.orgId}/converters/${detail.converter.id}`, {
        method: "PATCH",
        body: payload,
      });
      await loadDetail();
      setSuccess("Settings saved");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to update converter";
      setError(message);
    } finally {
      setPending((prev) => ({ ...prev, settings: false }));
    }
  };

  const handleAuthSubmit = async (formData: FormData) => {
    if (!detail) return;
    if (pending.auth) return;
    setPending((prev) => ({ ...prev, auth: true }));
    setError(null);
    setSuccess(null);

    const payload = {
      inboundAuthMode: toStringValue(formData.get("inbound_auth_mode")),
      inboundAuthToken: toStringValue(formData.get("inbound_auth_token")),
      inboundAuthUsername: toStringValue(formData.get("inbound_auth_username")),
      inboundAuthPassword: toStringValue(formData.get("inbound_auth_password")),
      inboundAuthHeaderName: toStringValue(formData.get("inbound_auth_header_name")),
      inboundAuthHeaderValue: toStringValue(formData.get("inbound_auth_header_value")),
      outboundAuthMode: toStringValue(formData.get("outbound_auth_mode")),
      outboundAuthToken: toStringValue(formData.get("outbound_auth_token")),
      outboundAuthUsername: toStringValue(formData.get("outbound_auth_username")),
      outboundAuthPassword: toStringValue(formData.get("outbound_auth_password")),
      outboundCustomHeaderName: toStringValue(
        formData.get("outbound_custom_header_name")
      ),
      outboundCustomHeaderValue: toStringValue(
        formData.get("outbound_custom_header_value")
      ),
      forwardHeadersJson: toStringValue(formData.get("forward_headers_json")),
    };

    try {
      await apiFetch(`/api/orgs/${params.orgId}/converters/${detail.converter.id}`, {
        method: "PATCH",
        body: payload,
      });
      await loadDetail();
      setSuccess("Authentication saved");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to update authentication";
      setError(message);
    } finally {
      setPending((prev) => ({ ...prev, auth: false }));
    }
  };

  const handleSaveMapping = async (formData: FormData) => {
    if (!detail) return;
    if (pending.mapping) return;
    setPending((prev) => ({ ...prev, mapping: true }));
    setError(null);
    setSuccess(null);

    const mappingJson = String(formData.get("mapping_json") || "");
    const inputSample = toStringValue(formData.get("input_sample")) ?? null;
    const outputSample = toStringValue(formData.get("output_sample")) ?? null;

    try {
      await apiFetch(`/api/orgs/${params.orgId}/converters/${detail.converter.id}/mappings`, {
        method: "POST",
        body: {
          mappingJson,
          inputSample,
          outputSample,
        },
      });
      await loadDetail();
      setSuccess("Rules saved");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to save rules";
      setError(message);
    } finally {
      setPending((prev) => ({ ...prev, mapping: false }));
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <p className="page-kicker">Converters</p>
          <h1 className="page-title text-3xl">Loading...</h1>
        </div>
      </div>
    );
  }

  if (!detail) {
    const normalizedError = (error ?? "").toLowerCase();
    const isNotFound =
      normalizedError.includes("not found") ||
      normalizedError.includes("not a member of this organization");
    return (
      <div className="space-y-6">
        <div>
          <p className="page-kicker">Converters</p>
          <h1 className="page-title text-3xl">
            {isNotFound ? "Converter details" : "Unable to load converter"}
          </h1>
          <p className="page-lede">Unable to load converter details.</p>
        </div>
        {isNotFound ? (
          <NotFoundState
            title="Converter not found."
            message="This converter may have been deleted or you no longer have access."
            actionLabel="Back to converters"
            actionHref={`/org/${params.orgId}/converters`}
          />
        ) : error ? (
          <p className="text-sm text-destructive">{error}</p>
        ) : null}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <FlashToast message={success ?? undefined} />
      <div>
        <p className="page-kicker">Converters</p>
        <h1 className="page-title text-3xl">{detail.converter.name}</h1>
        <p className="page-lede">
          Configure forwarding, authentication, and mapping rules.
        </p>
      </div>

      {error ? (
        <p className="text-sm text-destructive">{error}</p>
      ) : null}

      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="conversion">Conversion</TabsTrigger>
          <TabsTrigger value="authentication">Authentication</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Converter settings</CardTitle>
            </CardHeader>
            <CardContent>
              <form
                className="space-y-6"
                onSubmit={(event) => {
                  event.preventDefault();
                  handleSettingsSubmit(new FormData(event.currentTarget));
                }}
              >
                <input type="hidden" name="form_context" value="settings" />
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <FieldLabel
                      htmlFor="name"
                      label="Name"
                      tooltip="Internal name for this converter."
                    />
                    <Input id="name" name="name" defaultValue={detail.converter.name} />
                  </div>
                  <div className="space-y-2">
                    <FieldLabel
                      htmlFor="inbound_path"
                      label="Inbound path"
                      tooltip="Path segment after /api/inbound/:organizationId/ to receive requests."
                    />
                    <Input
                      id="inbound_path"
                      name="inbound_path"
                      defaultValue={detail.converter.inboundPath}
                    />
                  </div>
                  <div className="space-y-2">
                    <FieldLabel
                      htmlFor="forward_url"
                      label="Forward URL"
                      tooltip="Destination URL to forward inbound requests to."
                    />
                    <Input
                      id="forward_url"
                      name="forward_url"
                      defaultValue={detail.converter.forwardUrl}
                    />
                  </div>
                  <div className="space-y-2">
                    <FieldLabel
                      htmlFor="forward_method"
                      label="Forward method"
                      tooltip="Override the HTTP method when forwarding requests."
                    />
                    <SelectField
                      id="forward_method"
                      name="forward_method"
                      defaultValue={detail.converter.forwardMethod ?? "POST"}
                      options={[
                        { value: "GET", label: "GET" },
                        { value: "POST", label: "POST" },
                        { value: "PUT", label: "PUT" },
                        { value: "PATCH", label: "PATCH" },
                        { value: "DELETE", label: "DELETE" },
                        { value: "HEAD", label: "HEAD" },
                        { value: "OPTIONS", label: "OPTIONS" },
                        { value: "TRACE", label: "TRACE" },
                        { value: "CONNECT", label: "CONNECT" },
                      ]}
                    />
                  </div>
                  <CopyableInput
                    label="Inbound URL"
                    tooltip="Generated endpoint used by clients to call this converter."
                    value={inboundUrl || "Set NEXT_PUBLIC_SITE_URL to view"}
                  />
                  <div className="space-y-2">
                    <FieldLabel
                      htmlFor="inbound_response_mode"
                      label="Inbound response"
                      tooltip="Choose what the inbound endpoint returns to callers."
                    />
                    <SelectField
                      id="inbound_response_mode"
                      name="inbound_response_mode"
                      defaultValue={detail.converter.inboundResponseMode ?? "passthrough"}
                      options={[
                        {
                          value: "passthrough",
                          label: "Passthrough forward response",
                        },
                        { value: "ack", label: "Return minimal ACK" },
                      ]}
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="forward_headers_json"
                    label="Forward headers JSON"
                    tooltip="Static headers to include when forwarding; JSON object."
                  />
                  <Textarea
                    id="forward_headers_json"
                    name="forward_headers_json"
                    rows={4}
                    defaultValue={JSON.stringify(detail.converter.forwardHeaders ?? {}, null, 2)}
                  />
                </div>
                <div className="flex flex-wrap gap-6">
                  <div className="flex items-center gap-2 text-sm">
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        name="enabled"
                        defaultChecked={detail.converter.enabled}
                        className="h-4 w-4 rounded border-border"
                      />
                      Enabled
                    </label>
                    <TooltipIcon text="Enable or disable this converter." />
                  </div>
                  <div className="flex items-center gap-2 text-sm">
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        name="log_requests_enabled"
                        defaultChecked={detail.converter.logRequestsEnabled}
                        className="h-4 w-4 rounded border-border"
                      />
                      Log requests
                    </label>
                    <TooltipIcon text="Store request logs for this converter." />
                  </div>
                </div>
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <FieldLabel
                      htmlFor="log_retention_days"
                      label="Log retention (days)"
                      tooltip="Number of days to keep logs before deletion."
                    />
                    <Input
                      id="log_retention_days"
                      name="log_retention_days"
                      type="number"
                      min={1}
                      max={365}
                      defaultValue={detail.converter.logRetentionDays ?? 30}
                    />
                  </div>
                  <div className="space-y-2">
                    <FieldLabel
                      htmlFor="log_body_max_kb"
                      label="Max body size (KB)"
                      tooltip="Maximum request/response body size to store."
                    />
                    <Input
                      id="log_body_max_kb"
                      name="log_body_max_kb"
                      type="number"
                      min={1}
                      max={256}
                      defaultValue={Math.round(
                        (detail.converter.logBodyMaxBytes ?? 32768) / 1024
                      )}
                    />
                  </div>
                  <div className="space-y-2">
                    <FieldLabel
                      htmlFor="log_headers_max_kb"
                      label="Max headers size (KB)"
                      tooltip="Maximum header size to store."
                    />
                    <Input
                      id="log_headers_max_kb"
                      name="log_headers_max_kb"
                      type="number"
                      min={1}
                      max={64}
                      defaultValue={Math.round(
                        (detail.converter.logHeadersMaxBytes ?? 8192) / 1024
                      )}
                    />
                  </div>
                  <div className="flex items-center gap-2 text-sm">
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        name="log_redact_sensitive_headers"
                        defaultChecked={detail.converter.logRedactSensitiveHeaders ?? true}
                        className="h-4 w-4 rounded border-border"
                      />
                      Redact sensitive headers
                    </label>
                    <TooltipIcon text="Mask common sensitive headers in stored logs." />
                  </div>
                </div>
                <div className="mt-4 flex items-center justify-end">
                  <SubmitButton type="submit" pendingLabel="Saving..." pending={pending.settings}>
                    Save settings
                  </SubmitButton>
                </div>
              </form>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Recent logs</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Received</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Latency</TableHead>
                    <TableHead>Request ID</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detail.logs?.length ? (
                    detail.logs.map((log) => (
                      <TableRow key={log.requestId}>
                        <TableCell className="text-sm text-muted-foreground">
                          {new Date(log.receivedAt).toLocaleString()}
                        </TableCell>
                        <TableCell>{log.forwardStatus ?? "-"}</TableCell>
                        <TableCell>
                          {log.forwardResponseMs ? `${log.forwardResponseMs}ms` : "-"}
                        </TableCell>
                        <TableCell className="font-mono text-xs">
                          {log.requestId}
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={4} className="text-sm text-muted-foreground">
                        No logs yet.
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="authentication" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Authentication</CardTitle>
            </CardHeader>
            <CardContent>
              <ConverterAuthForm
                onSubmit={handleAuthSubmit}
                inboundAuthMode={inboundAuthMode as "none" | "bearer" | "basic" | "header"}
                inboundAuthLast4={inboundAuthLast4 ?? ""}
                inboundAuthUsername={detail.converter.inboundAuthUsername ?? ""}
                inboundAuthHeaderName={detail.converter.inboundAuthHeaderName ?? ""}
                outboundAuthMode={outboundAuthMode}
                outboundAuthHeaderValue={authHeaderValue}
                outboundCustomHeaderName={outboundCustomHeaderName}
                outboundCustomHeaderValue={outboundCustomHeaderValue}
                forwardHeadersJson={JSON.stringify(detail.converter.forwardHeaders ?? {}, null, 2)}
              />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="conversion" className="space-y-6">
          <ConversionEditor
            initialRules={(detail.mapping as ConversionRules | null) ?? null}
            initialInputSample={detail.inputSample ?? null}
            initialOutputSample={detail.outputSample ?? null}
            onSubmit={handleSaveMapping}
          />
        </TabsContent>
      </Tabs>
    </div>
  );
}
