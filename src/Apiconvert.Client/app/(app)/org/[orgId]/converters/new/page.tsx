"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { apiFetch } from "@/lib/api-client";
import { normalizeConverterNameForUrl } from "@/lib/converters";
import { SubmitButton } from "@/components/app/SubmitButton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { SelectField } from "@/components/app/SelectField";
import { FieldLabel } from "@/components/app/FieldLabel";
import { TooltipIcon } from "@/components/app/TooltipIcon";

function toNumber(value: FormDataEntryValue | null) {
  if (value === null || value === "") return undefined;
  const numberValue = Number(value);
  return Number.isNaN(numberValue) ? undefined : numberValue;
}

function toStringValue(value: FormDataEntryValue | null) {
  const text = String(value ?? "").trim();
  return text.length ? text : undefined;
}

export default function NewConverterPage() {
  const params = useParams<{ orgId: string }>();
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (pending) return;
    setPending(true);
    setError(null);

    const formData = new FormData(event.currentTarget);
    const payload = {
      name: toStringValue(formData.get("name")),
      inboundPath: toStringValue(formData.get("inbound_path")),
      forwardUrl: toStringValue(formData.get("forward_url")),
      forwardMethod: toStringValue(formData.get("forward_method")),
      forwardHeadersJson: toStringValue(formData.get("forward_headers_json")),
      enabled: formData.get("enabled") === "on",
      logRequestsEnabled: formData.get("log_requests_enabled") === "on",
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
      logRetentionDays: toNumber(formData.get("log_retention_days")),
      logBodyMaxKb: toNumber(formData.get("log_body_max_kb")),
      logHeadersMaxKb: toNumber(formData.get("log_headers_max_kb")),
      logRedactSensitiveHeaders:
        formData.get("log_redact_sensitive_headers") === "on",
      inboundResponseMode: toStringValue(formData.get("inbound_response_mode")),
    };

    try {
      await apiFetch(`/api/orgs/${params.orgId}/converters`, {
        method: "POST",
        body: payload,
      });
      const normalizedName = normalizeConverterNameForUrl(payload.name ?? "");
      router.replace(
        `/org/${params.orgId}/converters/${normalizedName}?success=${encodeURIComponent(
          "Converter created"
        )}`
      );
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to create converter";
      setError(message);
    } finally {
      setPending(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <p className="page-kicker">Converters</p>
        <h1 className="page-title text-3xl">Create converter</h1>
        <p className="page-lede">
          Define an inbound endpoint and where transformed payloads should land.
        </p>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>Converter settings</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6 pb-16">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="name"
                  label="Name"
                  tooltip="Human-readable name shown in dashboards and used in URLs."
                />
                <Input id="name" name="name" required />
              </div>
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="inbound_path"
                  label="Inbound path"
                  tooltip="Path segment appended after the organization route."
                />
                <Input id="inbound_path" name="inbound_path" required />
              </div>
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="forward_url"
                  label="Forward URL"
                  tooltip="Destination that receives the transformed payload."
                />
                <Input id="forward_url" name="forward_url" type="url" required />
              </div>
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="forward_method"
                  label="Forward method"
                  tooltip="HTTP method used when forwarding."
                />
                <SelectField
                  id="forward_method"
                  name="forward_method"
                  defaultValue="POST"
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
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="inbound_response_mode"
                  label="Inbound response"
                  tooltip="Passthrough forwards the response; ACK returns a minimal success payload."
                />
                <SelectField
                  id="inbound_response_mode"
                  name="inbound_response_mode"
                  defaultValue="passthrough"
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
                tooltip="JSON object merged into outbound headers."
              />
              <Textarea
                id="forward_headers_json"
                name="forward_headers_json"
                rows={4}
                placeholder='{"Authorization":"Bearer ..."}'
              />
            </div>
            <div className="space-y-4">
              <div>
                <h3 className="text-sm font-semibold">Authentication</h3>
                <p className="text-xs text-muted-foreground">
                  Configure inbound requirements and outbound Authorization headers.
                </p>
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="inbound_auth_mode"
                    label="Inbound auth"
                    tooltip="Require authentication on inbound requests."
                  />
                  <SelectField
                    id="inbound_auth_mode"
                    name="inbound_auth_mode"
                    defaultValue="none"
                    options={[
                      { value: "none", label: "No auth" },
                      { value: "bearer", label: "Bearer token" },
                      { value: "basic", label: "Basic auth" },
                      { value: "header", label: "Custom header" },
                    ]}
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="inbound_auth_token"
                    label="Inbound token"
                    tooltip="Bearer token for Authorization header."
                  />
                  <Input
                    id="inbound_auth_token"
                    name="inbound_auth_token"
                    type="password"
                    placeholder="Set a bearer token"
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="inbound_auth_username"
                    label="Inbound username"
                    tooltip="Basic auth username for inbound requests."
                  />
                  <Input
                    id="inbound_auth_username"
                    name="inbound_auth_username"
                    autoComplete="off"
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="inbound_auth_password"
                    label="Inbound password"
                    tooltip="Basic auth password for inbound requests."
                  />
                  <Input
                    id="inbound_auth_password"
                    name="inbound_auth_password"
                    type="password"
                    autoComplete="off"
                    placeholder="Set a password"
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="inbound_auth_header_name"
                    label="Inbound header name"
                    tooltip="Custom header name required on inbound requests."
                  />
                  <Input
                    id="inbound_auth_header_name"
                    name="inbound_auth_header_name"
                    placeholder="X-Auth-Token"
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="inbound_auth_header_value"
                    label="Inbound header value"
                    tooltip="Custom header value required on inbound requests."
                  />
                  <Input
                    id="inbound_auth_header_value"
                    name="inbound_auth_header_value"
                    type="password"
                    placeholder="Set a header value"
                  />
                </div>
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="outbound_auth_mode"
                    label="Outbound auth"
                    tooltip="Send Authorization header when forwarding."
                  />
                  <SelectField
                    id="outbound_auth_mode"
                    name="outbound_auth_mode"
                    defaultValue="none"
                    options={[
                      { value: "none", label: "No auth" },
                      { value: "bearer", label: "Bearer token" },
                      { value: "basic", label: "Basic auth" },
                    ]}
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="outbound_auth_token"
                    label="Outbound token"
                    tooltip="Bearer token for outbound Authorization header."
                  />
                  <Input
                    id="outbound_auth_token"
                    name="outbound_auth_token"
                    type="password"
                    placeholder="Set a token"
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="outbound_auth_username"
                    label="Outbound username"
                    tooltip="Basic auth username for outbound requests."
                  />
                  <Input
                    id="outbound_auth_username"
                    name="outbound_auth_username"
                    autoComplete="off"
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="outbound_auth_password"
                    label="Outbound password"
                    tooltip="Basic auth password for outbound requests."
                  />
                  <Input
                    id="outbound_auth_password"
                    name="outbound_auth_password"
                    type="password"
                    autoComplete="off"
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="outbound_custom_header_name"
                    label="Outbound header name"
                    tooltip="Custom header name for outbound requests."
                  />
                  <Input
                    id="outbound_custom_header_name"
                    name="outbound_custom_header_name"
                    placeholder="X-Partner-Key"
                  />
                </div>
                <div className="space-y-2">
                  <FieldLabel
                    htmlFor="outbound_custom_header_value"
                    label="Outbound header value"
                    tooltip="Custom header value for outbound requests."
                  />
                  <Input
                    id="outbound_custom_header_value"
                    name="outbound_custom_header_value"
                    placeholder="Set a header value"
                  />
                </div>
              </div>
            </div>
            <div className="flex flex-wrap gap-6">
              <div className="flex items-center gap-2 text-sm">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    name="enabled"
                    defaultChecked
                    className="h-4 w-4 rounded border-border"
                  />
                  Enabled
                </label>
                <TooltipIcon text="Toggle inbound traffic on or off." />
              </div>
              <div className="flex items-center gap-2 text-sm">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    name="log_requests_enabled"
                    defaultChecked
                    className="h-4 w-4 rounded border-border"
                  />
                  Log requests
                </label>
                <TooltipIcon text="Store inbound/forward metadata for debugging." />
              </div>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="log_retention_days"
                  label="Log retention (days)"
                  tooltip="How long to keep logs before cleanup."
                />
                <Input
                  id="log_retention_days"
                  name="log_retention_days"
                  type="number"
                  min={1}
                  max={365}
                  defaultValue={30}
                />
              </div>
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="log_body_max_kb"
                  label="Max body size (KB)"
                  tooltip="Maximum body size captured from requests."
                />
                <Input
                  id="log_body_max_kb"
                  name="log_body_max_kb"
                  type="number"
                  min={1}
                  max={256}
                  defaultValue={32}
                />
              </div>
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="log_headers_max_kb"
                  label="Max headers size (KB)"
                  tooltip="Maximum header size captured from requests."
                />
                <Input
                  id="log_headers_max_kb"
                  name="log_headers_max_kb"
                  type="number"
                  min={1}
                  max={64}
                  defaultValue={8}
                />
              </div>
              <div className="flex items-center gap-2 text-sm">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    name="log_redact_sensitive_headers"
                    defaultChecked
                    className="h-4 w-4 rounded border-border"
                  />
                  Redact sensitive headers
                </label>
                <TooltipIcon text="Mask auth tokens and similar headers in logs." />
              </div>
            </div>
            {error ? (
              <p className="text-sm text-destructive">{error}</p>
            ) : null}
            <div className="sticky bottom-0 z-10 -mx-6 mt-2 flex items-center justify-end border-t border-border bg-card/95 px-6 py-3 backdrop-blur supports-[backdrop-filter]:bg-card/80">
              <SubmitButton type="submit" pendingLabel="Creating..." pending={pending}>
                Create converter
              </SubmitButton>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
