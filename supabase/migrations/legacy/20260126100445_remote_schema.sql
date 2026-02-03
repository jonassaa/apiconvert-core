drop extension if exists "pg_net";

drop policy "Logs inserted by secret key" on "public"."converter_logs";

alter table "public"."converters" drop constraint "inbound_auth_mode_check";

alter table "public"."converters" alter column "forward_method" drop default;

alter type "public"."forward_method" rename to "forward_method__old_version_to_be_dropped";

create type "public"."forward_method" as enum ('POST', 'PUT', 'PATCH');

alter table "public"."converters" alter column forward_method type "public"."forward_method" using forward_method::text::"public"."forward_method";

alter table "public"."converters" alter column "forward_method" set default 'POST'::public.forward_method;

drop type "public"."forward_method__old_version_to_be_dropped";

alter table "public"."converter_mappings" drop column "input_sample";

alter table "public"."converter_mappings" drop column "output_sample";

alter table "public"."converters" drop column "inbound_auth_header_name";

alter table "public"."converters" drop column "inbound_auth_mode";

alter table "public"."converters" drop column "inbound_auth_username";

alter table "public"."converters" drop column "inbound_auth_value_hash";

alter table "public"."converters" drop column "inbound_auth_value_last4";


  create policy "Logs inserted by service role"
  on "public"."converter_logs"
  as permissive
  for insert
  to public
with check ((auth.role() = 'service_role'::text));



