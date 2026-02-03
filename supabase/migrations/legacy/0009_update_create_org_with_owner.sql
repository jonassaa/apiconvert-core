drop function if exists public.create_org_with_owner(text, text);

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
  where id = new_org_id;

  insert into organization_members (org_id, user_id, role)
  values (new_org_id, auth.uid(), 'owner');

  return query select new_org_id, new_org_id::text;
end;
$$;

grant execute on function public.create_org_with_owner(text) to authenticated;
