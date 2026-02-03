# Real-time Logs Research (Supabase)

Date: 2026-01-24

## Goal
Provide near-real-time updates for converter logs in the UI with minimal extra infra.

## Options
1. Supabase Realtime (postgres_changes)
   - Subscribe to inserts on `converter_logs`.
   - Pros: built-in, low latency, no extra infra.
   - Cons: needs careful RLS filters; high-volume streams could be noisy.

2. Supabase Realtime Broadcast channel
   - Emit events from the inbound API route after insert.
   - Pros: more control over payload, can filter by org/converter.
   - Cons: app code must publish; events can be dropped if client disconnected.

3. Polling with short intervals
   - Query recent logs every N seconds.
   - Pros: simplest; no realtime setup.
   - Cons: higher DB load; updates feel delayed.

## Recommendation
Use Supabase Realtime `postgres_changes` on `converter_logs` with RLS rules that scope by `org_id` and optional `converter_id`, and let the UI subscribe to insert events for the current org/logs view. This provides low-latency updates without custom infrastructure, and keeps data access consistent with DB policies.

If payload size or volume becomes an issue, add a broadcast channel as a second phase to send a curated, smaller event payload.

## Proposed Follow-up Tasks
- Add a Realtime subscription in the logs page to insert new log rows into the table.
- Add RLS policy for `converter_logs` to allow realtime replication for org members.
- Add rate limiting/throttling in the UI to avoid rendering too frequently.
- Add a feature flag to disable realtime updates if needed.

## Risks
- High log volume could overwhelm client updates.
- RLS misconfiguration could leak logs across orgs.
- Realtime subscription limits need monitoring.
