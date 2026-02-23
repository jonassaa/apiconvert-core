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
      text: Get Started
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
    details: Keep behavior aligned across NuGet and npm packages with shared conversion cases and CI parity gates.
  - icon: 🧩
    title: Rule-Driven Design
    details: Express conversion behavior declaratively through rule schema primitives instead of custom integration code.
  - icon: 🧪
    title: Deterministic Engine
    details: Build side-effect-free conversion flows that are predictable, testable, and safe to run in isolation.
---

<style scoped>
@import './home-layout-wrapper.css';
</style>

<div class="vp-doc home-wrapper">

### Quick example: field mapping

```json
{
  "inputFormat": "json",
  "outputFormat": "json",
  "rules": [
    {
      "source": "$.user.fullName",
      "target": "$.profile.name"
    }
  ]
}
```

### Quick example: .NET

<div class="runtime-dotnet">

```csharp
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var rules = RuleSet.Parse(jsonRules);
var result = ConversionEngine.Convert(inputPayload, rules);
```

</div>

### Quick example: TypeScript

<div class="runtime-typescript">

```ts
import { applyConversion, parseRuleSet } from "@apiconvert/core";

const rules = parseRuleSet(jsonRules);
const result = applyConversion(inputPayload, rules);
```

</div>

</div>
