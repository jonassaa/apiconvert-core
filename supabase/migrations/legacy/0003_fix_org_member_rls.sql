create or replace function public.is_org_member(check_org_id uuid)
returns boolean
language sql
security definer
set search_path = public
set row_security = off
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
as $$
  select exists (
    select 1 from organization_members
    where org_id = check_org_id
      and user_id = auth.uid()
      and role in ('owner', 'admin')
  );
$$;

create or replace function public.create_org_with_owner(
  org_name text,
  org_slug text
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
  values (org_name, org_slug, auth.uid())
  returning organizations.id into new_org_id;

  insert into organization_members (org_id, user_id, role)
  values (new_org_id, auth.uid(), 'owner');

  return query select new_org_id, org_slug;
end;
$$;
