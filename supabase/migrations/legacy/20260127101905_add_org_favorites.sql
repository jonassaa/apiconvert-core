create table organization_favorites (
  org_id uuid not null references organizations(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (org_id, user_id)
);

create index organization_favorites_user_id_idx on organization_favorites (user_id);

alter table organization_favorites enable row level security;

create policy "Users can view their org favorites"
  on organization_favorites for select
  using (user_id = auth.uid() and is_org_member(org_id));

create policy "Users can add org favorites"
  on organization_favorites for insert
  with check (user_id = auth.uid() and is_org_member(org_id));

create policy "Users can delete org favorites"
  on organization_favorites for delete
  using (user_id = auth.uid() and is_org_member(org_id));
