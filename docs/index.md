---
layout: home

hero:
  name: "Apiconvert.Core"
  text: "Deterministic API conversion across .NET and TypeScript"
  tagline: Rule-driven payload transformations with shared parity cases and schema contracts.
  image:
    src: /logo.svg
    alt: Apiconvert.Core
  actions:
    - theme: brand
      text: Install
      link: /start-here/install
    - theme: alt
      text: First Conversion
      link: /start-here/first-conversion
    - theme: alt
      text: Rules Reference
      link: /rules-reference/node-types

features:
  - icon: ⚖️
    title: Runtime Parity
    details: Keep behavior aligned across NuGet and npm packages with shared conversion cases and parity CI gates.
  - icon: 🧩
    title: Rule-Driven Design
    details: Express conversion behavior declaratively through schema primitives, not integration-specific mapper code.
  - icon: 🧪
    title: Deterministic Engine
    details: Side-effect-free conversion flows with stable diagnostics and reproducible outputs.
---

<style scoped>
@import './home-layout-wrapper.css';
</style>

<div class="vp-doc home-wrapper">

## Start in 10 minutes

1. Install for your runtime: [Install Guide](/start-here/install)
2. Run one conversion end-to-end: [First Conversion Walkthrough](/start-here/first-conversion)
3. Understand execution flow: [Conversion Lifecycle](/start-here/conversion-lifecycle)
4. Learn rule authoring basics: [Rule Node Types](/rules-reference/node-types)

## Minimal rule example

```json
{
  "inputFormat": "json",
  "outputFormat": "json",
  "rules": [
    {
      "kind": "field",
      "outputPaths": ["profile.name"],
      "source": { "type": "path", "path": "user.fullName" }
    }
  ]
}
```

## Runtime quickstart

<div class="runtime-dotnet">

```csharp
using Apiconvert.Core.Converters;

var rules = ConversionEngine.NormalizeConversionRulesStrict(rulesJson);
var (input, parseError) = ConversionEngine.ParsePayload(inputText, rules.InputFormat);
if (parseError is not null) throw new Exception(parseError);

var result = ConversionEngine.ApplyConversion(input, rules);
var output = ConversionEngine.FormatPayload(result.Output, rules.OutputFormat, pretty: true);
```

</div>

<div class="runtime-typescript">

```ts
import {
  applyConversion,
  formatPayload,
  normalizeConversionRulesStrict,
  parsePayload
} from "@apiconvert/core";

const rules = normalizeConversionRulesStrict(rulesJson);
const parsed = parsePayload(inputText, rules.inputFormat!);
if (parsed.error) throw new Error(parsed.error);

const result = applyConversion(parsed.value, rules);
const output = formatPayload(result.output, rules.outputFormat!, true);
```

</div>

## Continue reading

- Runtime APIs: [API Usage by Runtime](/runtime-guides/api-usage)
- Rules reference: [Sources and Transforms](/rules-reference/sources-and-transforms)
- Recipes: [JSON and XML Recipe](/recipes/json-and-xml), [Arrays, Branches, Merge, Split Recipe](/recipes/arrays-branches-merge-split)

</div>
