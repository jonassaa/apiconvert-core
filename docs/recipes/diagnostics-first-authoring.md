# Diagnostics-first Authoring

Suggested loop:

1. write minimal rule set
2. run strict normalize
3. run lint and rule doctor
4. add shared case
5. promote to production rule bundle

<div class="runtime-dotnet">

<h2 id="diagnostics-first-authoring-dotnet-commands">.NET commands</h2>

```bash
dotnet test Apiconvert.Core.sln --filter RuleDoctorTests
```

</div>

<div class="runtime-typescript">

<h2 id="diagnostics-first-authoring-typescript-commands">TypeScript commands</h2>

```bash
npm --prefix tests/npm/apiconvert-core-tests test -- --grep "rule doctor"
```

</div>
