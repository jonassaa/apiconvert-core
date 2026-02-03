"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { createClient } from "@/lib/supabase/client";
import { apiFetch } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { UserMenu } from "@/components/app/UserMenu";

const tocItems = [
  { id: "overview", label: "Overview" },
  { id: "core-concepts", label: "Core concepts" },
  { id: "getting-started", label: "Getting started" },
  { id: "app-flows", label: "App flows" },
  { id: "inbound-pipeline", label: "Inbound pipeline" },
  { id: "formats", label: "Formats & serialization" },
  { id: "mapping-rules", label: "Mapping rules" },
  { id: "request-handling", label: "Request handling" },
  { id: "examples", label: "End-to-end examples" },
  { id: "troubleshooting", label: "Troubleshooting" },
  { id: "testing", label: "Testing, logs, and preview" },
  { id: "faq", label: "FAQ" },
  { id: "glossary", label: "Glossary" },
];

const inboundExample = `POST https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH}
Authorization: Bearer {TOKEN}
Content-Type: application/json

{
  "customer": { "id": "c-123", "email": "jane@example.com" }
}`;

const mappingExample = `{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "json",
  "fieldMappings": [
    { "outputPath": "userId", "source": { "type": "path", "path": "customer.id" }, "defaultValue": "" },
    { "outputPath": "contact.email", "source": { "type": "path", "path": "customer.email" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}`;

const xmlAttributeExample = `<root id="123"><name>Acme</name></root>`;

const xmlParsedExample = `{
  "root": {
    "@_id": 123,
    "name": "Acme"
  }
}`;

const jsonToJsonExample = `curl -X POST \\
  https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH} \\
  -H "Authorization: Bearer {TOKEN}" \\
  -H "Content-Type: application/json" \\
  -d '{"customer":{"id":"c-123","email":"jane@example.com"}}'

Mapping rules:
${mappingExample}

Forwarded body:
{"userId":"c-123","contact":{"email":"jane@example.com"}}`;

const xmlToJsonExample = `curl -X POST \\
  https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH} \\
  -H "Authorization: Bearer {TOKEN}" \\
  -H "Content-Type: application/xml" \\
  -d '<root><customer id="c-123"><email>jane@example.com</email></customer></root>'

Mapping rules:
{
  "version": 2,
  "inputFormat": "xml",
  "outputFormat": "json",
  "fieldMappings": [
    { "outputPath": "userId", "source": { "type": "path", "path": "root.customer.@_id" }, "defaultValue": "" },
    { "outputPath": "contact.email", "source": { "type": "path", "path": "root.customer.email" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded body:
{"userId":123,"contact":{"email":"jane@example.com"}}`;

const jsonToXmlExample = `curl -X POST \\
  https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH} \\
  -H "Authorization: Bearer {TOKEN}" \\
  -H "Content-Type: application/json" \\
  -d '{"order":{"id":"o-9","total":42}}'

Mapping rules:
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "xml",
  "fieldMappings": [
    { "outputPath": "root.order.@_id", "source": { "type": "path", "path": "order.id" }, "defaultValue": "" },
    { "outputPath": "root.order.total", "source": { "type": "transform", "path": "order.total", "transform": "number" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded body:
<root><order id="o-9"><total>42</total></order></root>`;

const queryToJsonExample = `curl -X GET \\
  "https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH}?customer.id=c-123&customer.email=jane@example.com&tags=vip&tags=trial"

Mapping rules:
{
  "version": 2,
  "inputFormat": "query",
  "outputFormat": "json",
  "fieldMappings": [
    { "outputPath": "userId", "source": { "type": "path", "path": "customer.id" }, "defaultValue": "" },
    { "outputPath": "contact.email", "source": { "type": "path", "path": "customer.email" }, "defaultValue": "" },
    { "outputPath": "labels", "source": { "type": "path", "path": "tags" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded body:
{"userId":"c-123","contact":{"email":"jane@example.com"},"labels":["vip","trial"]}`;

const jsonToQueryExample = `curl -X POST \\
  https://your-app.com/api/inbound/{ORG_ID}/{INBOUND_PATH} \\
  -H "Content-Type: application/json" \\
  -d '{"customer":{"id":"c-123","email":"jane@example.com"},"tags":["vip","trial"]}'

Mapping rules:
{
  "version": 2,
  "inputFormat": "json",
  "outputFormat": "query",
  "fieldMappings": [
    { "outputPath": "customer.id", "source": { "type": "path", "path": "customer.id" }, "defaultValue": "" },
    { "outputPath": "customer.email", "source": { "type": "path", "path": "customer.email" }, "defaultValue": "" },
    { "outputPath": "tags", "source": { "type": "path", "path": "tags" }, "defaultValue": "" }
  ],
  "arrayMappings": []
}

Forwarded URL (body omitted):
https://forward.example.com/api/receive?customer.email=jane%40example.com&customer.id=c-123&tags=vip&tags=trial`;

type OrgListItem = {
  id: string;
};

type UserSummary = {
  email: string | null;
  name: string | null;
  avatarUrl: string | null;
};

export default function DocumentationPage() {
  const [user, setUser] = useState<UserSummary | null>(null);
  const [dashboardHref, setDashboardHref] = useState("/org");

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

  return (
    <div className="page-shell">
      <header className="sticky top-0 z-10 border-b border-border/60 bg-white/70 backdrop-blur dark:bg-zinc-900/70">
        <div className="mx-auto flex w-full max-w-5xl items-center justify-between gap-4 px-6 py-4">
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
              <Button asChild size="sm">
                <Link href={dashboardHref}>Go to dashboard</Link>
              </Button>
            ) : (
              <Button size="sm" asChild>
                <Link href="/login">Sign in</Link>
              </Button>
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

      <main className="page-container space-y-10">
        <section className="surface-panel p-8 md:p-10 space-y-4">
          <p className="page-kicker">Documentation</p>
          <h1 className="page-title text-4xl md:text-5xl">
            apiconvert documentation
          </h1>
          <p className="text-base text-muted-foreground">
            Everything you need to build, test, and operate converters. This page
            mirrors the repo documentation and adds the in-app flow so you can
            ship integrations without leaving the UI.
          </p>
          <div className="flex flex-wrap gap-3">
            <Button asChild>
              <Link href="/getting-started">Getting started</Link>
            </Button>
            <Button variant="outline" asChild>
              <Link href="/login">Open app</Link>
            </Button>
            <Button variant="outline" asChild>
              <Link href="#examples">Jump to examples</Link>
            </Button>
          </div>
        </section>

        <div className="grid gap-10 lg:grid-cols-[220px_minmax(0,1fr)]">
          <aside className="space-y-4">
            <div className="surface-panel sticky top-24 space-y-4 p-5">
              <p className="text-xs font-semibold uppercase tracking-[0.3em] text-muted-foreground">
                On this page
              </p>
              <nav className="space-y-2 text-sm">
                {tocItems.map((item) => (
                  <a
                    key={item.id}
                    href={`#${item.id}`}
                    className="block text-muted-foreground transition hover:text-foreground"
                  >
                    {item.label}
                  </a>
                ))}
              </nav>
            </div>
          </aside>

          <div className="space-y-12">
            <section id="overview" className="space-y-4">
              <h2 className="text-2xl font-semibold">Overview</h2>
              <p className="text-sm text-muted-foreground">
                apiconvert lets you receive partner payloads, map them to your
                internal schema, and forward transformed requests to your API.
                Converters make inbound endpoints repeatable and safe so your
                team can onboard integrations faster.
              </p>
            </section>

            <section id="core-concepts" className="space-y-4">
              <h2 className="text-2xl font-semibold">Core concepts</h2>
              <div className="grid gap-4 md:grid-cols-2">
                <Card>
                  <CardHeader>
                    <CardTitle>How the UI is organized</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>Organization: top-level container for converters and logs.</li>
                      <li>Converter: defines inbound URL, forwarding destination, and auth.</li>
                      <li>Conversion tab: configure mapping rules and format transforms.</li>
                      <li>Logs tab: inspect status, latency, and payload captures.</li>
                    </ul>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader>
                    <CardTitle>Key terms</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>Inbound URL: the public endpoint partners call.</li>
                      <li>Forwarding: outbound request apiconvert makes after mapping.</li>
                      <li>Mapping rules: field + array rules that reshape data.</li>
                      <li>Response mode: passthrough or minimal ACK.</li>
                    </ul>
                  </CardContent>
                </Card>
              </div>
            </section>

            <section id="getting-started" className="surface-panel space-y-4 p-6 md:p-8">
              <h2 className="text-2xl font-semibold">Getting started (happy path)</h2>
              <p className="text-sm text-muted-foreground">
                The fastest route to a live converter: create a converter, map
                your first payload, enable logging, and test inbound traffic.
              </p>
              <ol className="space-y-2 text-sm text-muted-foreground">
                <li>1. Create a converter with an inbound path and forward URL.</li>
                <li>2. Configure inbound authentication (or allow no auth for testing).</li>
                <li>3. Set input/output formats and add field or array rules.</li>
                <li>4. Preview output in the Conversion tab, then save rules.</li>
                <li>5. Enable logs and set retention limits.</li>
                <li>6. Send a test request to the inbound URL and confirm forwarding.</li>
              </ol>
              <div className="flex flex-wrap gap-3">
                <Button size="sm" asChild>
                  <Link href="/getting-started">Walk through the UI</Link>
                </Button>
                <Button size="sm" variant="outline" asChild>
                  <Link href="#examples">Try an example</Link>
                </Button>
              </div>
            </section>

            <section id="app-flows" className="space-y-4">
              <h2 className="text-2xl font-semibold">App flows</h2>
              <div className="grid gap-4 md:grid-cols-2">
                <Card>
                  <CardHeader>
                    <CardTitle>Org setup</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ol className="space-y-2">
                      <li>1. Sign in and create your organization.</li>
                      <li>2. Invite teammates and assign roles.</li>
                      <li>3. Use the dashboard to manage converters and logs.</li>
                    </ol>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader>
                    <CardTitle>Converter lifecycle</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ol className="space-y-2">
                      <li>1. Create converter → configure inbound auth.</li>
                      <li>2. Add mapping rules in the Conversion tab.</li>
                      <li>3. Enable logging and set retention limits.</li>
                      <li>4. Test inbound → review Logs → iterate mappings.</li>
                      <li>5. Disable or re-enable converters as needed.</li>
                    </ol>
                  </CardContent>
                </Card>
              </div>
            </section>

            <section id="inbound-pipeline" className="surface-panel space-y-4 p-6 md:p-8">
              <h2 className="text-2xl font-semibold">Inbound pipeline</h2>
              <p className="text-sm text-muted-foreground">
                Every inbound request flows through the same processing steps.
              </p>
              <ol className="space-y-2 text-sm text-muted-foreground">
                <li>1. Auth: validate bearer/basic/header/none.</li>
                <li>2. Parse: parse payload according to input format.</li>
                <li>3. Map: apply field rules and array rules.</li>
                <li>4. Format: serialize output to the selected format.</li>
                <li>5. Forward: send the transformed payload to the forward URL.</li>
                <li>6. Respond: passthrough response or minimal ACK.</li>
                <li>7. Log: capture request/response with redaction if enabled.</li>
              </ol>
            </section>

            <section id="formats" className="space-y-6">
              <h2 className="text-2xl font-semibold">Formats &amp; serialization rules</h2>
              <div className="grid gap-4">
                <Card>
                  <CardHeader>
                    <CardTitle>JSON</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>Input: parsed with JSON.parse.</li>
                      <li>Output: JSON.stringify (compact for forwarding, pretty in preview).</li>
                      <li>If no mappings are defined, output is the input JSON object (or {}).</li>
                    </ul>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader>
                    <CardTitle>XML</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>Input: parsed with fast-xml-parser.</li>
                      <li>Output: serialized with fast-xml-parser (compact for forwarding).</li>
                      <li>Attributes are preserved with the "@_" prefix.</li>
                      <li>Tag and attribute values parse to boolean/number when possible.</li>
                    </ul>
                    <p className="text-xs font-semibold uppercase text-muted-foreground">
                      Example
                    </p>
                    <div className="grid gap-3 md:grid-cols-2">
                      <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                        {xmlAttributeExample}
                      </pre>
                      <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                        {xmlParsedExample}
                      </pre>
                    </div>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader>
                    <CardTitle>Query parameters</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>Input: query strings parse into nested objects.</li>
                      <li>Output: only object roots are allowed (non-object outputs error).</li>
                      <li>Keys are sorted alphabetically at each object level.</li>
                      <li>Objects flatten into dot notation for output.</li>
                      <li>Bracket syntax supports arrays: items[0].sku.</li>
                      <li>Repeated keys become arrays (tags=one&amp;tags=two).</li>
                      <li>Arrays of objects are JSON-stringified per item.</li>
                      <li>Encoding uses URLSearchParams (standard percent-encoding).</li>
                    </ul>
                  </CardContent>
                </Card>
              </div>
            </section>

            <section id="mapping-rules" className="surface-panel space-y-6 p-6 md:p-8">
              <h2 className="text-2xl font-semibold">Mapping rules (Conversion tab)</h2>
              <p className="text-sm text-muted-foreground">
                Mapping rules are stored as ConversionRules v2 and edited in the
                Conversion tab. Combine field rules and array rules to map any
                payload shape.
              </p>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-3">
                  <h3 className="text-lg font-semibold">Field rules</h3>
                  <ul className="space-y-2 text-sm text-muted-foreground">
                    <li>Write a value to an output path.</li>
                    <li>Source types: Path, Constant, Transform, Condition.</li>
                    <li>Default values apply when source is null/undefined/empty.</li>
                    <li>Path syntax: dot notation, brackets, numeric indices.</li>
                    <li>$ or $.path reads from the root object.</li>
                  </ul>
                </div>
                <div className="space-y-3">
                  <h3 className="text-lg font-semibold">Array rules</h3>
                  <ul className="space-y-2 text-sm text-muted-foreground">
                    <li>Input path resolves to an array.</li>
                    <li>Coerce single wraps non-array values.</li>
                    <li>Item mappings run per item in the array.</li>
                  </ul>
                </div>
              </div>
              <div className="space-y-3">
                <h3 className="text-lg font-semibold">Transforms &amp; conditions</h3>
                <div className="grid gap-4 md:grid-cols-2">
                  <Card>
                    <CardHeader>
                      <CardTitle>Transforms</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3 text-sm text-muted-foreground">
                      <ul className="space-y-2">
                        <li>toLowerCase, toUpperCase, number, boolean, concat.</li>
                        <li>concat accepts comma-separated tokens and const: values.</li>
                      </ul>
                    </CardContent>
                  </Card>
                  <Card>
                    <CardHeader>
                      <CardTitle>Conditions</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3 text-sm text-muted-foreground">
                      <ul className="space-y-2">
                        <li>Operators: exists, equals, notEquals, includes, gt, lt.</li>
                        <li>Includes supports string contains and array membership.</li>
                      </ul>
                    </CardContent>
                  </Card>
                </div>
              </div>
              <div className="space-y-3">
                <p className="text-xs font-semibold uppercase text-muted-foreground">
                  Example mapping rules
                </p>
                <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                  {mappingExample}
                </pre>
              </div>
            </section>

            <section id="request-handling" className="space-y-6">
              <h2 className="text-2xl font-semibold">Request handling</h2>
              <div className="grid gap-4 md:grid-cols-2">
                <Card>
                  <CardHeader>
                    <CardTitle>Supported methods &amp; limits</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>Inbound methods: GET, POST, PUT, PATCH.</li>
                      <li>Max inbound body size: 1,000,000 bytes (413 on exceed).</li>
                      <li>Rate limit: 60 requests/min per converter (Retry-After: 60s).</li>
                      <li>Forwarding timeout: 10 seconds.</li>
                    </ul>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader>
                    <CardTitle>Auth modes</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>None, Bearer token, Basic auth, Header auth.</li>
                      <li>Bearer supports Authorization: Bearer &lt;token&gt; or x-apiconvert-token.</li>
                    </ul>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader>
                    <CardTitle>Forwarding behavior</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>Forward method defaults to inbound method if unset.</li>
                      <li>Forward headers JSON adds static headers.</li>
                      <li>Content-Type set by output format (json/xml/query).</li>
                      <li>Query output appends params to Forward URL and sends no body.</li>
                    </ul>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader>
                    <CardTitle>Response modes &amp; logging</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm text-muted-foreground">
                    <ul className="space-y-2">
                      <li>Passthrough returns forward response status/body.</li>
                      <li>Return minimal ACK: {`{ ok: true, request_id }`} with 202.</li>
                      <li>Logging captures headers/bodies with size limits.</li>
                      <li>Defaults: 32 KB body, 8 KB headers, 30-day retention.</li>
                      <li>Redacts sensitive headers (authorization, cookie, set-cookie, x-apiconvert-token).</li>
                    </ul>
                  </CardContent>
                </Card>
              </div>
            </section>

            <section id="examples" className="surface-panel space-y-6 p-6 md:p-8">
              <h2 className="text-2xl font-semibold">End-to-end examples</h2>
              <p className="text-sm text-muted-foreground">
                Replace {`{ORG_ID}`}, {`{INBOUND_PATH}`}, and {`{TOKEN}`} with your values.
              </p>
              <div className="space-y-4">
                <div>
                  <p className="text-sm font-semibold">JSON → JSON</p>
                  <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                    {jsonToJsonExample}
                  </pre>
                </div>
                <div>
                  <p className="text-sm font-semibold">XML → JSON</p>
                  <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                    {xmlToJsonExample}
                  </pre>
                </div>
                <div>
                  <p className="text-sm font-semibold">JSON → XML</p>
                  <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                    {jsonToXmlExample}
                  </pre>
                </div>
                <div>
                  <p className="text-sm font-semibold">Query → JSON</p>
                  <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                    {queryToJsonExample}
                  </pre>
                </div>
                <div>
                  <p className="text-sm font-semibold">JSON → Query</p>
                  <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                    {jsonToQueryExample}
                  </pre>
                </div>
              </div>
            </section>

            <section id="troubleshooting" className="space-y-4">
              <h2 className="text-2xl font-semibold">Troubleshooting &amp; edge cases</h2>
              <ul className="space-y-2 text-sm text-muted-foreground">
                <li>Invalid JSON/XML/query payloads return 400 with format-specific errors.</li>
                <li>Conversion rules errors return 400 with details on the invalid rule.</li>
                <li>Output format query requires an object root; other types error.</li>
                <li>Forwarding errors return 502 and include a request_id.</li>
                <li>Disabled converters return 403; missing paths return 404.</li>
              </ul>
            </section>

            <section id="testing" className="surface-panel space-y-4 p-6 md:p-8">
              <h2 className="text-2xl font-semibold">Testing, logs, and preview output</h2>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-3">
                  <h3 className="text-lg font-semibold">Test inbound requests</h3>
                  <ul className="space-y-2 text-sm text-muted-foreground">
                    <li>Use the Inbound URL displayed on the converter.</li>
                    <li>Match the configured auth mode when sending requests.</li>
                    <li>For query input, send parameters on the URL (GET recommended).</li>
                  </ul>
                  <pre className="surface-muted font-mono text-xs whitespace-pre-wrap">
                    {inboundExample}
                  </pre>
                </div>
                <div className="space-y-3">
                  <h3 className="text-lg font-semibold">Review logs and iterate</h3>
                  <ul className="space-y-2 text-sm text-muted-foreground">
                    <li>Check Logs for request_id, status, and latency.</li>
                    <li>Open the Conversion tab to preview output before saving rules.</li>
                    <li>Update mappings, then re-test to confirm forwarding.</li>
                  </ul>
                </div>
              </div>
            </section>

            <section id="faq" className="space-y-4">
              <h2 className="text-2xl font-semibold">FAQ</h2>
              <div className="space-y-3 text-sm text-muted-foreground">
                <p>
                  <span className="font-semibold text-foreground">Can I forward without mapping?</span>{" "}
                  Yes. If no mappings are defined, the output mirrors the input payload.
                </p>
                <p>
                  <span className="font-semibold text-foreground">Where do I find the inbound URL?</span>{" "}
                  It&apos;s displayed on each converter page and can be copied from the header.
                </p>
                <p>
                  <span className="font-semibold text-foreground">How do I disable a converter?</span>{" "}
                  Use the converter settings panel to toggle it off and stop inbound traffic.
                </p>
              </div>
            </section>

            <section id="glossary" className="space-y-4">
              <h2 className="text-2xl font-semibold">Glossary</h2>
              <ul className="space-y-2 text-sm text-muted-foreground">
                <li>Converter: inbound endpoint plus forward destination.</li>
                <li>Conversion tab: where mapping rules and formats are set.</li>
                <li>Inbound URL: the URL partners call to submit data.</li>
                <li>Mapping rules: field + array rules that build the output payload.</li>
                <li>Response mode: passthrough or minimal ACK responses.</li>
              </ul>
            </section>
          </div>
        </div>
      </main>
    </div>
  );
}
