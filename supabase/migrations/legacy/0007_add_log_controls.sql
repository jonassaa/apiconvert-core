alter table converters
  add column if not exists log_retention_days integer default 30,
  add column if not exists log_body_max_bytes integer default 32768,
  add column if not exists log_headers_max_bytes integer default 8192,
  add column if not exists log_redact_sensitive_headers boolean default true;
