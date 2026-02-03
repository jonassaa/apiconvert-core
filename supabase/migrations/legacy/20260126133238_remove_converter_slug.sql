alter table public.converters
  drop constraint if exists converters_org_id_slug_key;

alter table public.converters
  drop column if exists slug;

create unique index if not exists converters_org_id_name_lower_key
  on public.converters (org_id, lower(name));
