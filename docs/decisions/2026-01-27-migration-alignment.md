# Migration alignment with production

## Context
Production migrations drifted from the local `supabase/migrations` directory after several timestamped migrations were applied directly in production. This caused local history to diverge from the production migration list.

## Decision
Keep `supabase/migrations` aligned with the production migration list by matching version filenames and contents, including timestamped migrations. When a production migration is applied outside the local repo, bring it back into the repo with the exact version prefix and SQL so future diffs are consistent.

## Notes
- If a migration name/version exists in production but not locally, add a matching SQL file with the exact version prefix.
- If a local migration is not in production, remove or rename it to align with the production list.
- Use `mcp__supabase__list_migrations` to validate the production list before aligning.
