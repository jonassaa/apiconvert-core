# Local development

## Environment variables

- Copy `.env.example` to `.env` at the repo root for Docker Compose.
- If running the Next.js client directly (without Docker), copy `.env.example` to
  `src/Apiconvert.Client/.env` so Next.js can load the variables.

## Install dependencies

```bash
npm --prefix src/Apiconvert.Client install
```

## Docker Compose (full stack)

```bash
docker compose up --build
```

Services:
- Client: http://localhost:3123
- API: http://localhost:8080/health

## Optional .NET conversion preview

To use the .NET API for conversion previews in the UI, set:

- `NEXT_PUBLIC_CONVERSION_ENGINE=dotnet`
- `DOTNET_API_BASE_URL=http://localhost:8080`
 - `NEXT_PUBLIC_API_BASE_URL=http://localhost:8080`

## Optional .NET admin API (converter CRUD + mappings + logs)

To route converter admin actions through the .NET API, set:

- `NEXT_PUBLIC_ADMIN_API=dotnet`
- `NEXT_PUBLIC_DOTNET_API_BASE_URL=http://localhost:8080`
 - `NEXT_PUBLIC_API_BASE_URL=http://localhost:8080`

## OpenRouter (AI mapping generation)

Configure OpenRouter in `.env` for the .NET API:

- `OPENROUTER_API_KEY=...`
- `OPENROUTER_MODEL=openai/gpt-4o-mini`
- `OPENROUTER_BASE_URL=https://openrouter.ai/api/v1`
- `OPENROUTER_SITE_URL=http://localhost:3123`
- `OPENROUTER_APP_NAME=apiconvert`

## Supabase (auth/data)

The client relies on Supabase for auth and data access. Provide working
`NEXT_PUBLIC_SUPABASE_URL` and `NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY` values in
`.env`. For local Supabase, run `supabase start` in another terminal and copy
values from `supabase status`.

If you run the .NET API against local Supabase Postgres, set
`APICONVERT_DB_CONNECTION` to the connection string (from `supabase status`).
