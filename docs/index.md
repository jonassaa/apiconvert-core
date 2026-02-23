---
layout: home

hero:
  name: "Apiconvert.Core"
  text: "Deterministic API conversion for .NET and TypeScript"
  tagline: "Rule-driven payload transformation with shared parity cases and schema contracts."
  image:
    src: /logo.svg
    alt: Apiconvert.Core
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started/
    - theme: alt
      text: Rules Schema Reference
      link: /reference/rules-schema

features:
  - icon: 🧭
    title: Rules As The Contract
    details: Keep API transformation behavior in versionable, reviewable rule files instead of embedding mapping logic across services.
  - icon: 🧠
    title: Replace Custom Mapper Code
    details: Define transformations declaratively with rule nodes like field, array, and branch instead of writing one-off mapping logic per integration.
  - icon: 🛡️
    title: Deterministic Conversion Pipeline
    details: Convert payloads between JSON, XML, and query formats using deterministic, side-effect-free execution with validation and diagnostics.
---

<style scoped>
@import './home-layout-wrapper.css';
</style>

<div class="vp-doc home-wrapper">

## Quick path

1. Understand the package scope: [What is Apiconvert.Core?](./overview/what-is-apiconvert-core.md)
2. Run your first conversion: [Getting started](./getting-started/index.md)
3. Learn the shared model: [Rules model](./concepts/rules-model.md)
4. Use the source-of-truth schema: [Rules schema reference](./reference/rules-schema.md)

## Minimal rule

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

## Next sections

- Runtime APIs: [Runtime API guide](./guides/runtime-api.md)
- Practical scenarios: [Recipes](./recipes/hello-world.md)
- Troubleshooting: [Troubleshooting tree](./troubleshooting/troubleshooting-tree.md)

</div>
