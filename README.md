# Apiconvert.Core

[![NuGet Version](https://img.shields.io/nuget/v/Apiconvert.Core.svg)](https://www.nuget.org/packages/Apiconvert.Core/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Apiconvert.Core.svg)](https://www.nuget.org/packages/Apiconvert.Core/)

Core conversion and mapping engine for Apiconvert.


Apiconvert.Core is the core library for converting API descriptions and generating interoperable models. It provides a composable conversion engine, rule configuration, and contracts for generation targets.

## Quickstart

Install from NuGet:

```bash
dotnet add package Apiconvert.Core
```

Minimal example:

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Apiconvert.Core.Contracts;

// Create rules/config (example placeholders)
var rules = new ConversionRules
{
    // configure rules here
};

var engine = new ConversionEngine(rules);

// Convert input into generation models (example placeholders)
var result = engine.Convert(input);

// Use result (generation models / contracts)
```

## Features

- Deterministic conversion pipeline
- Rule-based configuration
- Contracts for generation / interop targets
- Nullable-aware, side-effect-free core paths

## Concepts

- **ConversionEngine**: Orchestrates conversion execution.
- **Rules**: Models + configuration for conversion behavior.
- **Contracts**: Shared DTOs for generators and interop.

## Usage

Typical flow:

1. Build rules/configuration.
2. Instantiate `ConversionEngine`.
3. Convert input models to generation models.
4. Consume generation models in your tooling.

## Compatibility

- Target framework: .NET (see NuGet package metadata)
- `Nullable` enabled

## Versioning

This project follows SemVer. Breaking changes are documented in release notes.

## Contributing

Build:

```bash
dotnet build Apiconvert.Core.sln
```

Test:

```bash
dotnet test Apiconvert.Core.sln
```

## License

Proprietary. See `LICENSE`.


# apiconvert

apiconvert lets teams create inbound API converters that transform JSON payloads and forward them to internal APIs, with minimal logging and metrics.

## Stack
- Next.js App Router + TypeScript
- Supabase (Postgres + Auth)
- shadcn/ui + Tailwind
- Vercel hosting

## Local setup
### Option A: Local Supabase (CLI)
1) Start the local stack:
```
npm run supabase:start
```
2) Fetch local URL + keys and set `.env.local`:
```
npm run supabase:status
```
Example `.env.local`:
```
NEXT_PUBLIC_SUPABASE_URL=http://127.0.0.1:54321
NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY=your_local_anon_key
SUPABASE_SECRET_KEY=your_local_service_role_key
NEXT_PUBLIC_SITE_URL=http://localhost:3123
```
3) Reset DB (runs migrations + seed):
```
npm run supabase:reset
```
4) Install and run:
```
npm install
npm run dev
```

### Option B: Hosted Supabase
1) Create a Supabase project.
2) Run the SQL migrations in `supabase/migrations` (including `0001_init.sql` and `0002_create_org_with_owner.sql`).
3) Configure OAuth providers in Supabase Auth:
   - GitHub
4) Create `.env.local` with:
```
NEXT_PUBLIC_SUPABASE_URL=your_supabase_url
NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY=your_publishable_key
SUPABASE_SECRET_KEY=your_secret_key
NEXT_PUBLIC_SITE_URL=http://localhost:3123
```
5) Install and run:
```
npm install
npm run dev
```

## Supabase Auth providers
Ensure the redirect URLs include:
- `http://localhost:3123/auth/callback` (local)
- `https://your-vercel-domain/auth/callback` (prod)

## Local email auth
Email/password auth is available only on localhost (the login UI hides it in production).
Use the seeded dev user (`dev@example.com` / `dev-password`) to sign in quickly.

## Deploy to Vercel
1) Push to GitHub and import into Vercel.
2) Set the same env vars in Vercel.
3) Set `NEXT_PUBLIC_SITE_URL` to your Vercel domain.
4) Deploy.

## Public landing page
`/` is a public marketing page with examples and sign-in CTAs. Authenticated users see a “Go to dashboard” CTA.

## Active organization and routing
Active org is scoped in the URL: `/org/[orgId]/...`, where `orgId` is the org UUID. New orgs are created at `/org/new`. Dashboards live at `/org/[orgId]/dashboard`. Old slug-based URLs redirect to the UUID route.

Post-auth routing:
- Users with at least one org are redirected to their first org dashboard.
- Users without an org are sent to `/org/new`.

Org creation uses the `create_org_with_owner` database function to atomically create the org and insert the creator as an `owner` (server-side enforced).

## Theme toggle
Theme preference (light/dark/system) is controlled by `next-themes`, persisted in localStorage. Default is `system`.

## Inbound endpoint
Inbound URL format:
```
/api/inbound/{orgId}/{inboundPath}
```

Example:
```
curl -X POST \
  https://your-domain/api/inbound/<org-id>/support-intake \
  -H "Content-Type: application/json" \
  -H "X-Apiconvert-Token: your-secret" \
  -d '{"customer":{"email":"a@example.com","name":"Alice"}}'
```

Responses include `x-apiconvert-request-id` for tracing.

### Inbound safety rails
- Payloads over 1 MB are rejected with `413`.
- Basic per-converter rate limiting defaults to 60 requests per minute.
- Forward URLs must use `http`/`https` and cannot target localhost or private IPs.
- Inbound response mode is configurable per converter (`passthrough` or `ack`).

## Logging detail
When request logging is enabled for a converter, apiconvert stores request/response
headers and bodies in `converter_logs`. Response metadata is captured alongside
forward status and latency. Controls include:
- Retention in days (defaults to 30).
- Header/body size caps (defaults to 8 KB headers, 32 KB bodies).
- Optional redaction of sensitive headers (`Authorization`, `Cookie`, `Set-Cookie`,
  `X-Apiconvert-Token`).

## Mapping rules
Mappings are stored as JSON:
```
{
  "rows": [
    {
      "outputPath": "ticket.requester.email",
      "sourceType": "path",
      "sourceValue": "customer.email"
    }
  ]
}
```

Supported `sourceType` values:
- `path`
- `constant`
- `transform`

Supported `transformType` values:
- `toLowerCase`
- `toUpperCase`
- `number`
- `boolean`
- `concat`

Concat uses comma-separated tokens and supports constants via `const:`:
```
sourceValue: "customer.firstName,const: ,customer.lastName"
```

## Seed data
`supabase/seed.sql` creates a local auth user (`dev@example.com` / `dev-password`) if missing,
then seeds an example org and converter owned by that user. Update the email/password in the seed
if you want different defaults.

## Commands
- `npm run dev`: start local dev server
- `npm run build`: production build
- `npm run start`: start production server
- `npm run lint`: lint
- `npm run test`: run tests

## Assumptions
- Converter inbound endpoints use org UUIDs + converter inbound paths for stable URLs.
- Secret API key is used only in the inbound route handler.
- Logging is minimal but extensible via `converter_logs`.
