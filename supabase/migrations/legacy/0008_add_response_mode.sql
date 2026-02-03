alter table converters
  add column if not exists inbound_response_mode text default 'passthrough';

alter table converters
  add constraint inbound_response_mode_check
  check (inbound_response_mode in ('passthrough', 'ack'));
