-- Consolidated schema migration (squashed from prior migrations)

create extension if not exists "pgcrypto";
drop extension if exists "pg_net";

create type org_role as enum ('owner', 'admin', 'member');
create type forward_method as enum (
  'POST',
  'PUT',
  'PATCH'
);

create table organizations (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  slug text not null unique,
  created_by uuid references auth.users(id),
  created_at timestamptz not null default now()
);

create table organization_members (
  id uuid primary key default gen_random_uuid(),
  org_id uuid not null references organizations(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  role org_role not null default 'member',
  created_at timestamptz not null default now(),
  unique (org_id, user_id)
);

create table invites (
  id uuid primary key default gen_random_uuid(),
  org_id uuid not null references organizations(id) on delete cascade,
  email text not null,
  role org_role not null default 'member',
  token text not null unique,
  expires_at timestamptz not null,
  accepted_at timestamptz,
  created_by uuid references auth.users(id),
  created_at timestamptz not null default now()
);

create table converters (
  id uuid primary key default gen_random_uuid(),
  org_id uuid not null references organizations(id) on delete cascade,
  name text not null,
  inbound_path text not null,
  enabled boolean not null default true,
  forward_url text not null,
  forward_method forward_method not null default 'POST',
  forward_headers_json jsonb not null default '{}'::jsonb,
  log_requests_enabled boolean not null default true,
  inbound_secret_hash text,
  inbound_secret_last4 text,
  created_by uuid references auth.users(id) default auth.uid(),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  log_retention_days integer default 30,
  log_body_max_bytes integer default 32768,
  log_headers_max_bytes integer default 8192,
  log_redact_sensitive_headers boolean default true,
  inbound_response_mode text default 'passthrough',
  inbound_auth_mode text,
  inbound_auth_header_name text,
  inbound_auth_username text,
  inbound_auth_value_hash text,
  inbound_auth_value_last4 text,
  constraint inbound_response_mode_check
    check (inbound_response_mode in ('passthrough', 'ack')),
  constraint inbound_auth_mode_check
    check (
      inbound_auth_mode in ('none', 'bearer', 'basic', 'header')
      or inbound_auth_mode is null
    )
);

create table converter_mappings (
  id uuid primary key default gen_random_uuid(),
  converter_id uuid not null references converters(id) on delete cascade,
  mapping_json jsonb not null default '{"rows":[]}'::jsonb,
  version integer not null default 1,
  updated_at timestamptz not null default now(),
  input_sample text,
  output_sample text,
  unique (converter_id, version)
);

create table converter_logs (
  id uuid primary key default gen_random_uuid(),
  converter_id uuid not null references converters(id) on delete cascade,
  org_id uuid not null references organizations(id) on delete cascade,
  received_at timestamptz not null default now(),
  request_id uuid not null,
  source_ip text,
  method text,
  path text,
  headers_json jsonb,
  query_json jsonb,
  body_json jsonb,
  transformed_body_json jsonb,
  forward_url text,
  forward_status integer,
  forward_response_ms integer,
  error_text text,
  forward_response_headers_json jsonb,
  forward_response_body_json jsonb,
  forward_response_body_text text
);

create table organization_favorites (
  org_id uuid not null references organizations(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (org_id, user_id)
);

create index converter_logs_org_time_idx on converter_logs (org_id, received_at desc);
create index converter_logs_converter_time_idx on converter_logs (converter_id, received_at desc);
create index converter_logs_request_id_idx on converter_logs (request_id);
create unique index converters_org_inbound_path_unique
  on converters (org_id, inbound_path);
create unique index converters_org_id_name_lower_key
  on converters (org_id, lower(name));
create index organization_favorites_user_id_idx on organization_favorites (user_id);

create or replace function public.set_updated_at()
returns trigger
language plpgsql
set search_path to 'public', 'extensions'
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

create trigger set_converters_updated_at
before update on converters
for each row execute function set_updated_at();

create trigger set_converter_mappings_updated_at
before update on converter_mappings
for each row execute function set_updated_at();

create or replace function public.is_org_member(check_org_id uuid)
returns boolean
language sql
security definer
set search_path = public
set row_security = off
stable
as $$
  select exists (
    select 1 from organization_members
    where org_id = check_org_id
      and user_id = auth.uid()
  );
$$;

create or replace function public.is_org_admin(check_org_id uuid)
returns boolean
language sql
security definer
set search_path = public
set row_security = off
stable
as $$
  select exists (
    select 1 from organization_members
    where org_id = check_org_id
      and user_id = auth.uid()
      and role in ('owner', 'admin')
  );
$$;

create or replace function public.is_org_owner(check_org_id uuid)
returns boolean
language sql
security definer
set search_path = public
set row_security = off
stable
as $$
  select exists (
    select 1 from organization_members
    where org_id = check_org_id
      and user_id = auth.uid()
      and role = 'owner'
  );
$$;

create or replace function public.create_org_with_owner(
  org_name text
)
returns table (id uuid, slug text)
language plpgsql
security definer
set search_path = public
set row_security = off
as $$
declare
  new_org_id uuid;
begin
  if auth.uid() is null then
    raise exception 'Not authenticated';
  end if;

  insert into organizations (name, slug, created_by)
  values (org_name, gen_random_uuid()::text, auth.uid())
  returning organizations.id into new_org_id;

  update organizations
  set slug = new_org_id::text
  where organizations.id = new_org_id;

  insert into organization_members (org_id, user_id, role)
  values (new_org_id, auth.uid(), 'owner');

  return query select new_org_id, new_org_id::text;
end;
$$;

grant execute on function public.create_org_with_owner(text) to authenticated;

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

alter table organizations enable row level security;
alter table organization_members enable row level security;
alter table invites enable row level security;
alter table converters enable row level security;
alter table converter_mappings enable row level security;
alter table converter_logs enable row level security;
alter table organization_favorites enable row level security;

create policy "Organizations are visible to members"
  on organizations for select
  using (is_org_member(id));

create policy "Organizations can be created by authenticated users"
  on organizations for insert
  with check (auth.uid() is not null and created_by = auth.uid());

create policy "Organizations can be updated by admins"
  on organizations for update
  using (is_org_admin(id));

create policy "Organizations can be deleted by owners"
  on organizations for delete
  using (is_org_owner(id));

create policy "Members can view org members"
  on organization_members for select
  using (is_org_member(org_id));

create policy "Admins can manage members"
  on organization_members for insert
  with check (
    is_org_admin(org_id)
    or exists (
      select 1 from organizations
      where id = org_id and created_by = auth.uid()
    )
  );

create policy "Admins can update members"
  on organization_members for update
  using (is_org_admin(org_id));

create policy "Admins can remove members"
  on organization_members for delete
  using (is_org_admin(org_id));

create policy "Invites visible to admins and invitees"
  on invites for select
  using (
    is_org_admin(org_id)
    or lower(email) = lower(coalesce(auth.jwt() ->> 'email', ''))
  );

create policy "Invites created by admins"
  on invites for insert
  with check (is_org_admin(org_id));

create policy "Invites can be updated by admins or invitees"
  on invites for update
  using (
    is_org_admin(org_id)
    or lower(email) = lower(coalesce(auth.jwt() ->> 'email', ''))
  );

create policy "Invites deleted by admins"
  on invites for delete
  using (is_org_admin(org_id));

create policy "Converters visible to members"
  on converters for select
  using (is_org_member(org_id));

create policy "Converters managed by admins"
  on converters for insert
  with check (is_org_admin(org_id));

create policy "Converters updated by admins"
  on converters for update
  using (is_org_admin(org_id));

create policy "Converters deleted by admins"
  on converters for delete
  using (is_org_admin(org_id));

create policy "Mappings visible to members"
  on converter_mappings for select
  using (
    exists (
      select 1 from converters
      where id = converter_id and is_org_member(org_id)
    )
  );

create policy "Mappings managed by admins"
  on converter_mappings for insert
  with check (
    exists (
      select 1 from converters
      where id = converter_id and is_org_admin(org_id)
    )
  );

create policy "Mappings updated by admins"
  on converter_mappings for update
  using (
    exists (
      select 1 from converters
      where id = converter_id and is_org_admin(org_id)
    )
  );

create policy "Mappings deleted by admins"
  on converter_mappings for delete
  using (
    exists (
      select 1 from converters
      where id = converter_id and is_org_admin(org_id)
    )
  );

create policy "Logs visible to members"
  on converter_logs for select
  using (is_org_member(org_id));

create policy "Logs inserted by service role"
  on converter_logs for insert
  to public
  with check (auth.role() = 'service_role');

create policy "Users can view their org favorites"
  on organization_favorites for select
  using (user_id = auth.uid() and is_org_member(org_id));

create policy "Users can add org favorites"
  on organization_favorites for insert
  with check (user_id = auth.uid() and is_org_member(org_id));

create policy "Users can delete org favorites"
  on organization_favorites for delete
  using (user_id = auth.uid() and is_org_member(org_id));
