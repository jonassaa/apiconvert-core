alter table public.converter_mappings
  add column if not exists input_sample text,
  add column if not exists output_sample text;

notify pgrst, 'reload schema';
