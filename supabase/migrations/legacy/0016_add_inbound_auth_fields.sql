alter table converters
  add column if not exists inbound_auth_mode text,
  add column if not exists inbound_auth_header_name text,
  add column if not exists inbound_auth_username text,
  add column if not exists inbound_auth_value_hash text,
  add column if not exists inbound_auth_value_last4 text;

do $$
begin
  if not exists (
    select 1
    from pg_constraint
    where conname = 'inbound_auth_mode_check'
      and conrelid = 'public.converters'::regclass
  ) then
    alter table public.converters
      add constraint inbound_auth_mode_check
      check (
        inbound_auth_mode in ('none', 'bearer', 'basic', 'header')
        or inbound_auth_mode is null
      );
  end if;
end $$;

update converters
set inbound_auth_mode = 'bearer',
    inbound_auth_value_hash = inbound_secret_hash,
    inbound_auth_value_last4 = inbound_secret_last4
where inbound_auth_mode is null
  and inbound_secret_hash is not null;

notify pgrst, 'reload schema';
