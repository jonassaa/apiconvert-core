# Install

Use this page to get a working local setup quickly for both runtimes.

## Prerequisites

- .NET SDK 8+ for the NuGet package
- Node.js 18+ for the npm package
- npm (bundled with Node.js)

## Install package

<div class="runtime-dotnet">

<h3 id="install-dotnet">.NET</h3>

```bash
dotnet add package Apiconvert.Core
```

</div>

<div class="runtime-typescript">

<h3 id="install-typescript">TypeScript</h3>

```bash
npm install @apiconvert/core
```

</div>

## Sanity checks

Run these once after setup:

```bash
dotnet --version
node --version
npm --version
```

## Verify package integration

<div class="runtime-dotnet">

Create a minimal compile check:

```csharp
using Apiconvert.Core.Converters;

Console.WriteLine(typeof(ConversionEngine).FullName);
```

</div>

<div class="runtime-typescript">

Create a minimal import check:

```ts
import { normalizeConversionRulesStrict } from "@apiconvert/core";

console.log(typeof normalizeConversionRulesStrict);
```

</div>

## Next steps

1. Run the first end-to-end conversion: [/start-here/first-conversion](/start-here/first-conversion)
2. Understand conversion lifecycle: [/start-here/conversion-lifecycle](/start-here/conversion-lifecycle)
3. Jump to runtime API reference: [/runtime-guides/api-usage](/runtime-guides/api-usage)
