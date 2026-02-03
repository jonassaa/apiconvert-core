do $$
declare
  v_user_id uuid;
  v_user_email text := 'asdf@asdf.com';
  v_user_password text := 'P@ssw0rd';
  v_instance_id uuid := '00000000-0000-0000-0000-000000000000';
  v_identity_id uuid := gen_random_uuid();
  v_identity_data jsonb;
  v_has_provider_id boolean;
  v_org_id uuid := gen_random_uuid();
  v_converter_id uuid := gen_random_uuid();
begin
  select id
    into v_user_id
    from auth.users
   where email = v_user_email
   limit 1;

  if v_user_id is null then
    v_user_id := gen_random_uuid();

    if not exists (select 1 from auth.instances where id = v_instance_id) then
      insert into auth.instances (id, uuid, created_at, updated_at)
      values (v_instance_id, v_instance_id, now(), now());
    end if;

    insert into auth.users (
      id,
      instance_id,
      aud,
      role,
      email,
      encrypted_password,
      email_confirmed_at,
      raw_app_meta_data,
      raw_user_meta_data,
      created_at,
      updated_at,
      last_sign_in_at,
      confirmation_token,
      recovery_token,
      email_change_token_new,
      email_change
    ) values (
      v_user_id,
      v_instance_id,
      'authenticated',
      'authenticated',
      v_user_email,
      crypt(v_user_password, gen_salt('bf', 10)),
      now(),
      jsonb_build_object('provider', 'email', 'providers', jsonb_build_array('email')),
      jsonb_build_object(
        'sub',
        v_user_id::text,
        'email',
        v_user_email,
        'email_verified',
        true,
        'phone_verified',
        false
      ),
      now(),
      now(),
      now(),
      '',
      '',
      '',
      ''
    );

    v_identity_data := jsonb_build_object(
      'sub', v_user_id::text,
      'email', v_user_email
    );

    v_has_provider_id := exists (
      select 1
        from information_schema.columns
       where table_schema = 'auth'
         and table_name = 'identities'
         and column_name = 'provider_id'
    );

    if v_has_provider_id then
      execute
        'insert into auth.identities (id, user_id, identity_data, provider, provider_id, created_at, updated_at)
         values ($1, $2, $3, $4, $5, $6, $7)
         on conflict do nothing'
      using v_identity_id, v_user_id, v_identity_data, 'email', v_user_id::text, now(), now();
    else
      execute
        'insert into auth.identities (id, user_id, identity_data, provider, created_at, updated_at)
         values ($1, $2, $3, $4, $5, $6)
         on conflict do nothing'
      using v_identity_id, v_user_id, v_identity_data, 'email', now(), now();
    end if;
  end if;

  insert into organizations (id, name, slug, created_by)
  values (v_org_id, 'Example Org', v_org_id::text, v_user_id);

  insert into organization_members (org_id, user_id, role)
  values (v_org_id, v_user_id, 'owner');

  insert into converters (
    id,
    org_id,
    name,
    inbound_path,
    forward_url,
    forward_method,
    forward_headers_json,
    log_requests_enabled,
    enabled,
    created_by
  ) values (
    v_converter_id,
    v_org_id,
    'Support ticket intake',
    'support/intake',
    'https://api.example.com/support',
    'POST',
    '{"Authorization":"Bearer example"}'::jsonb,
    true,
    true,
    v_user_id
  );

  insert into converter_mappings (converter_id, mapping_json, version)
  values (
    v_converter_id,
    '{"rows":[{"outputPath":"ticket.requester.email","sourceType":"path","sourceValue":"customer.email"},{"outputPath":"ticket.priority","sourceType":"constant","sourceValue":"normal"},{"outputPath":"ticket.requester.name","sourceType":"transform","sourceValue":"customer.name","transformType":"toLowerCase"}]}'::jsonb,
    1
  );
end $$;
