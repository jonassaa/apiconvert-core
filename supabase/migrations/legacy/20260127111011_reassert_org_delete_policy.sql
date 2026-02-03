create or replace function public.is_org_owner(check_org_id uuid)
returns boolean
language sql
stable
security definer
set search_path = public
set row_security = off
as $$
  select exists (
    select 1 from organization_members
    where org_id = check_org_id
      and user_id = auth.uid()
      and role = 'owner'
  );
$$;

drop policy if exists "Organizations can be deleted by owners" on organizations;

create policy "Organizations can be deleted by owners"
  on organizations for delete
  using (is_org_owner(id));
