alter table converter_logs
  add column if not exists forward_response_headers_json jsonb,
  add column if not exists forward_response_body_json jsonb,
  add column if not exists forward_response_body_text text;
