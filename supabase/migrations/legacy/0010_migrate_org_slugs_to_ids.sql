update organizations
set slug = id::text
where slug is distinct from id::text;
