# Azure App Service deployment

This document lists the minimal configuration required to run the .NET API on
Azure App Service with Supabase.

## Required App Service settings

Set these Application Settings (or Connection Strings) in the App Service:

- `ConnectionStrings:ApiconvertDb` or `APICONVERT_DB_CONNECTION`
- `SUPABASE_URL`
- `SUPABASE_JWT_AUDIENCE` (optional, defaults to `authenticated`)
- `SUPABASE_SECRET_KEY` (service role key for admin API usage)
- `CLIENT_ORIGIN` or `NEXT_PUBLIC_SITE_URL`
- `OPENROUTER_SITE_URL`
- `OPENROUTER_APP_NAME`
- `OPENROUTER_API_KEY` (if using AI mapping generation)
- `OPENROUTER_BASE_URL` (optional)
- `OPENROUTER_MODEL` (optional)

Notes:
- Prefer the Supabase Postgres pooler connection string when scaling out.
- Ensure the connection string uses TLS (`sslmode=Require`).

## App Service configuration

- Enable HTTPS only.
- Configure Health check path to `/health`.
- Enable Application Insights or OpenTelemetry for logs/metrics/traces.

## Why App Service over Container Apps

We chose App Service because it is the simplest operational model for a steady,
always-on API. It reduces container build/deploy overhead and avoids cold-start
latency, which matters for consistent response times. Container Apps can be a
better value for highly bursty or idle workloads that benefit from scale-to-zero,
but App Service offers the most predictable UX and the least moving parts for
this project right now.

## Supabase access

App Service connects to Supabase over the public internet. Use strong credentials
and rotate the service role key if needed. If you restrict IPs, ensure App
Service outbound IPs are allowed.
