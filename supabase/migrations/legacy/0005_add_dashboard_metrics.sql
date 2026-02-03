create or replace function public.get_org_dashboard_metrics(org_id uuid)
returns table (
  requests_24h bigint,
  requests_7d bigint,
  success_7d bigint,
  avg_response_ms numeric
)
language sql
security definer
set search_path = public
set row_security = off
as $$
  select
    (
      select count(*)
      from converter_logs
      where converter_logs.org_id = get_org_dashboard_metrics.org_id
        and received_at >= now() - interval '24 hours'
    ) as requests_24h,
    (
      select count(*)
      from converter_logs
      where converter_logs.org_id = get_org_dashboard_metrics.org_id
        and received_at >= now() - interval '7 days'
    ) as requests_7d,
    (
      select count(*)
      from converter_logs
      where converter_logs.org_id = get_org_dashboard_metrics.org_id
        and received_at >= now() - interval '7 days'
        and forward_status between 200 and 299
    ) as success_7d,
    (
      select coalesce(avg(forward_response_ms), 0)
      from converter_logs
      where converter_logs.org_id = get_org_dashboard_metrics.org_id
        and received_at >= now() - interval '7 days'
    ) as avg_response_ms;
$$;

grant execute on function public.get_org_dashboard_metrics(uuid) to authenticated;
