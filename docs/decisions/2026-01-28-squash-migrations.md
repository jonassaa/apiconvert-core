# Squashed Supabase migrations

We consolidated the Supabase migrations into a single schema migration to remove ordering conflicts and duplicate changes.

## Local verification
- `npm run supabase:reset`
- `npm run e2e`

## Cloud rollout plan
1) Back up the production database.
2) Apply the consolidated migration to the cloud database.
   - Preferred: `supabase db push` from this repo.
   - Alternative: run the SQL in the Supabase SQL editor.
3) Verify the schema matches expectations (tables, enums, functions, policies).
4) Refresh the PostgREST schema cache.
   - Supabase dashboard: API -> Reload schema.
   - Or SQL: `select pg_notify('pgrst', 'reload schema');`
5) Run smoke checks against core flows (create org, create converter, inbound request).
