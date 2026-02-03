"use client";

import { useMemo, useState } from "react";
import { FieldLabel } from "@/components/app/FieldLabel";
import { SelectField } from "@/components/app/SelectField";
import { SubmitButton } from "@/components/app/SubmitButton";
import { Input } from "@/components/ui/input";

type AuthMode = "none" | "bearer" | "basic" | "header";

type ConverterAuthFormProps = {
  action?: (formData: FormData) => void;
  onSubmit?: (formData: FormData) => void | Promise<void>;
  inboundAuthMode: AuthMode;
  inboundAuthLast4: string;
  inboundAuthUsername: string;
  inboundAuthHeaderName: string;
  outboundAuthMode: "none" | "bearer" | "basic";
  outboundAuthHeaderValue: string;
  outboundCustomHeaderName: string;
  outboundCustomHeaderValue: string;
  forwardHeadersJson: string;
};

export function ConverterAuthForm({
  action,
  onSubmit,
  inboundAuthMode,
  inboundAuthLast4,
  inboundAuthUsername,
  inboundAuthHeaderName,
  outboundAuthMode,
  outboundAuthHeaderValue,
  outboundCustomHeaderName,
  outboundCustomHeaderValue,
  forwardHeadersJson,
}: ConverterAuthFormProps) {
  const [inboundMode, setInboundMode] = useState<AuthMode>(inboundAuthMode);
  const [outboundMode, setOutboundMode] = useState<"none" | "bearer" | "basic">(
    outboundAuthMode
  );
  const [errors, setErrors] = useState<{
    inbound?: string;
    outbound?: string;
    customHeader?: string;
  }>({});
  const [missing, setMissing] = useState<Record<string, boolean>>({});

  const existingOutboundMode = useMemo<"none" | "bearer" | "basic">(() => {
    if (outboundAuthHeaderValue.startsWith("Bearer ")) return "bearer";
    if (outboundAuthHeaderValue.startsWith("Basic ")) return "basic";
    return "none";
  }, [outboundAuthHeaderValue]);

  const inboundSummary = useMemo(() => {
    if (inboundMode === "none") return "No auth";
    if (inboundMode === "bearer") return "Bearer token";
    if (inboundMode === "basic") return "Basic auth";
    return "Custom header";
  }, [inboundMode]);

  const outboundSummary = useMemo(() => {
    if (outboundMode === "none") return "No auth";
    if (outboundMode === "bearer") return "Bearer token";
    return "Basic auth";
  }, [outboundMode]);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    const form = event.currentTarget;
    const data = new FormData(form);
    const nextMissing: Record<string, boolean> = {};
    const nextErrors: typeof errors = {};

    if (inboundMode === "bearer") {
      const token = String(data.get("inbound_auth_token") || "").trim();
      if (!token && !inboundAuthLast4) {
        nextMissing.inbound_auth_token = true;
        nextErrors.inbound = "Inbound bearer auth requires a token.";
      }
    }

    if (inboundMode === "basic") {
      const username = String(data.get("inbound_auth_username") || "").trim();
      const password = String(data.get("inbound_auth_password") || "").trim();
      if (!username && !inboundAuthUsername) {
        nextMissing.inbound_auth_username = true;
      }
      if (!password && !inboundAuthLast4) {
        nextMissing.inbound_auth_password = true;
      }
      if (nextMissing.inbound_auth_username || nextMissing.inbound_auth_password) {
        nextErrors.inbound =
          "Inbound basic auth requires a username and password.";
      }
    }

    if (inboundMode === "header") {
      const headerName = String(data.get("inbound_auth_header_name") || "").trim();
      const headerValue = String(data.get("inbound_auth_header_value") || "").trim();
      if (!headerName && !inboundAuthHeaderName) {
        nextMissing.inbound_auth_header_name = true;
      }
      if (!headerValue && !inboundAuthLast4) {
        nextMissing.inbound_auth_header_value = true;
      }
      if (
        nextMissing.inbound_auth_header_name ||
        nextMissing.inbound_auth_header_value
      ) {
        nextErrors.inbound =
          "Inbound custom header auth requires a header name and value.";
      }
    }

    if (outboundMode === "bearer") {
      const token = String(data.get("outbound_auth_token") || "").trim();
      if (!token && existingOutboundMode !== "bearer") {
        nextMissing.outbound_auth_token = true;
        nextErrors.outbound = "Outbound bearer auth requires a token.";
      }
    }

    if (outboundMode === "basic") {
      const username = String(data.get("outbound_auth_username") || "").trim();
      const password = String(data.get("outbound_auth_password") || "").trim();
      if (existingOutboundMode !== "basic") {
        if (!username) {
          nextMissing.outbound_auth_username = true;
        }
        if (!password) {
          nextMissing.outbound_auth_password = true;
        }
        if (nextMissing.outbound_auth_username || nextMissing.outbound_auth_password) {
          nextErrors.outbound =
            "Outbound basic auth requires a username and password.";
        }
      }
    }

    const customHeaderName = String(
      data.get("outbound_custom_header_name") || ""
    ).trim();
    const customHeaderValue = String(
      data.get("outbound_custom_header_value") || ""
    ).trim();
    if ((customHeaderName && !customHeaderValue) || (!customHeaderName && customHeaderValue)) {
      nextMissing.outbound_custom_header_name = !customHeaderName;
      nextMissing.outbound_custom_header_value = !customHeaderValue;
      nextErrors.customHeader =
        "Outbound custom header requires both a name and a value.";
    }

    if (Object.keys(nextErrors).length) {
      event.preventDefault();
      setErrors(nextErrors);
      setMissing(nextMissing);
    } else {
      setErrors({});
      setMissing({});
      if (onSubmit) {
        event.preventDefault();
        onSubmit(data);
      }
    }
  };

  return (
    <form
      action={onSubmit ? undefined : action}
      className="space-y-6"
      onSubmit={handleSubmit}
    >
      <input type="hidden" name="form_context" value="authentication" />
      <input type="hidden" name="forward_headers_json" value={forwardHeadersJson} />

      <details
        className="rounded-lg border border-border/60 bg-card/60 px-4 py-3"
        open
      >
        <summary className="flex cursor-pointer list-none items-center justify-between gap-3 text-sm font-medium">
          <span>Inbound authentication</span>
          <span className="text-xs text-muted-foreground">{inboundSummary}</span>
        </summary>
        <div className="mt-4 space-y-4">
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
                value={inboundMode}
                onValueChange={(value) => {
                  setInboundMode(value as AuthMode);
                  setErrors((prev) => ({ ...prev, inbound: undefined }));
                }}
                options={[
                  { value: "none", label: "No auth" },
                  { value: "bearer", label: "Bearer token" },
                  { value: "basic", label: "Basic auth" },
                  { value: "header", label: "Custom header" },
                ]}
              />
            </div>
          </div>

          {errors.inbound ? (
            <p className="text-sm text-destructive">{errors.inbound}</p>
          ) : null}

          {inboundMode === "bearer" ? (
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
                placeholder="Set a new bearer token"
                aria-invalid={missing.inbound_auth_token || undefined}
              />
              {inboundAuthLast4 ? (
                <p className="text-xs text-muted-foreground">
                  Current secret ends with {inboundAuthLast4}.
                </p>
              ) : null}
            </div>
          ) : null}

          {inboundMode === "basic" ? (
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="inbound_auth_username"
                  label="Inbound username"
                  tooltip="Basic auth username for inbound requests."
                />
                <Input
                  id="inbound_auth_username"
                  name="inbound_auth_username"
                  defaultValue={inboundAuthUsername}
                  autoComplete="off"
                  aria-invalid={missing.inbound_auth_username || undefined}
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
                  placeholder="Set a new password"
                  aria-invalid={missing.inbound_auth_password || undefined}
                />
                {inboundAuthLast4 ? (
                  <p className="text-xs text-muted-foreground">
                    Current secret ends with {inboundAuthLast4}.
                  </p>
                ) : null}
              </div>
            </div>
          ) : null}

          {inboundMode === "header" ? (
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <FieldLabel
                  htmlFor="inbound_auth_header_name"
                  label="Inbound header name"
                  tooltip="Custom header name required on inbound requests."
                />
                <Input
                  id="inbound_auth_header_name"
                  name="inbound_auth_header_name"
                  defaultValue={inboundAuthHeaderName}
                  placeholder="X-Auth-Token"
                  aria-invalid={missing.inbound_auth_header_name || undefined}
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
                  aria-invalid={missing.inbound_auth_header_value || undefined}
                />
                {inboundAuthLast4 ? (
                  <p className="text-xs text-muted-foreground">
                    Current secret ends with {inboundAuthLast4}.
                  </p>
                ) : null}
              </div>
            </div>
          ) : null}
        </div>
      </details>

      <details
        className="rounded-lg border border-border/60 bg-card/60 px-4 py-3"
        open
      >
        <summary className="flex cursor-pointer list-none items-center justify-between gap-3 text-sm font-medium">
          <span>Outbound authentication</span>
          <span className="text-xs text-muted-foreground">{outboundSummary}</span>
        </summary>
        <div className="mt-4 space-y-4">
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
                value={outboundMode}
                onValueChange={(value) => {
                  setOutboundMode(value as "none" | "bearer" | "basic");
                  setErrors((prev) => ({ ...prev, outbound: undefined }));
                }}
                options={[
                  { value: "none", label: "No auth" },
                  { value: "bearer", label: "Bearer token" },
                  { value: "basic", label: "Basic auth" },
                ]}
              />
              {outboundAuthHeaderValue ? (
                <p className="text-xs text-muted-foreground">
                  Existing Authorization header detected.
                </p>
              ) : null}
            </div>
          </div>

          {errors.outbound ? (
            <p className="text-sm text-destructive">{errors.outbound}</p>
          ) : null}

          {outboundMode === "bearer" ? (
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
                placeholder="Set a new token"
                aria-invalid={missing.outbound_auth_token || undefined}
              />
            </div>
          ) : null}

          {outboundMode === "basic" ? (
            <div className="grid gap-4 md:grid-cols-2">
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
                  aria-invalid={missing.outbound_auth_username || undefined}
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
                  aria-invalid={missing.outbound_auth_password || undefined}
                />
              </div>
            </div>
          ) : null}

          <div className="border-t border-border/60 pt-4">
            <p className="text-sm font-medium">Custom outbound header</p>
            <p className="text-xs text-muted-foreground">
              Optional header added to forwarded requests.
            </p>
          </div>

          {errors.customHeader ? (
            <p className="text-sm text-destructive">{errors.customHeader}</p>
          ) : null}

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <FieldLabel
                htmlFor="outbound_custom_header_name"
                label="Outbound header name"
                tooltip="Custom header name for outbound requests."
              />
              <Input
                id="outbound_custom_header_name"
                name="outbound_custom_header_name"
                defaultValue={outboundCustomHeaderName}
                placeholder="X-Partner-Key"
                aria-invalid={missing.outbound_custom_header_name || undefined}
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
                defaultValue={outboundCustomHeaderValue}
                placeholder="Set a header value"
                aria-invalid={missing.outbound_custom_header_value || undefined}
              />
            </div>
          </div>
        </div>
      </details>

      <div className="mt-4 flex items-center justify-end">
        <SubmitButton type="submit" pendingLabel="Saving...">
          Save authentication
        </SubmitButton>
      </div>
    </form>
  );
}
