alter table public.converters
  add column if not exists inbound_response_mode text default 'passthrough';

do $$
begin
  if not exists (
    select 1
    from pg_constraint
    where conname = 'inbound_response_mode_check'
      and conrelid = 'public.converters'::regclass
  ) then
    alter table public.converters
      add constraint inbound_response_mode_check
      check (inbound_response_mode in ('passthrough', 'ack'));
  end if;
end $$;

notify pgrst, 'reload schema';
