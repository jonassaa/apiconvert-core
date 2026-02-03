create or replace function public.create_org_with_owner(
  org_name text,
  org_slug text
)
returns table (id uuid, slug text)
language plpgsql
security definer
set search_path = public
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

grant execute on function public.create_org_with_owner(text, text) to authenticated;
