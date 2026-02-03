# Converter Slug Evaluation

Date: 2026-01-24

## Current usage
- Stored on `converters.slug` with unique constraint per org. Used to build UI routes:
  - Converter detail routes: `/org/:orgId/converters/:slug`
  - Dashboard and list links reference `converter.slug`
- Used in search filtering for converters and logs (name/slug/URL).
- Collected on create form as a required field.

## Pros
- Human-readable URLs for sharing and navigation.
- Stable identifiers in UI that are shorter and friendlier than UUIDs.
- Helps users find converters via search and logs.

## Cons
- Extra required field during creation; adds friction.
- Potential collisions or renames require additional validation.
- Not used for inbound API routing (inbound path is separate), so it is UI-only.

## Recommendation
Keep the slug, but reduce creation friction by auto-generating it from the name and allowing edits (or making it optional with a default). This preserves friendly URLs and search behavior without forcing manual input.

## Follow-up tasks (if accepted)
- Auto-generate slug on create (from name), allow override.
- Add optional “edit slug” affordance with uniqueness validation.
- If removing slug entirely, migrate UI routes to use converter `id` and update all queries/search to use `id` instead of `slug`.
