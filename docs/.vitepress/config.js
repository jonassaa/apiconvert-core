import { defineConfig } from "vitepress";

const base = "/apiconvert-core/";

export default defineConfig({
  base,
  title: "Apiconvert.Core",
  description: "Rule-driven API conversion docs for .NET and TypeScript",
  lang: "en-US",
  lastUpdated: true,
  head: [["link", { rel: "icon", href: `${base}favicon.ico` }]],
  themeConfig: {
    logo: "/logo.svg",
    nav: [
      { text: "Start Here", link: "/start-here/install" },
      { text: "Guide", link: "/core-concepts/intent-and-boundaries" },
      { text: "Rules", link: "/rules-reference/node-types" },
      { text: "GitHub", link: "https://github.com/jonassaa/apiconvert-core" }
    ],
    sidebar: [
      {
        text: "Start Here",
        items: [
          { text: "Overview", link: "/" },
          { text: "Install", link: "/start-here/install" },
          { text: "First Conversion", link: "/start-here/first-conversion" },
          { text: "Migration", link: "/start-here/migration" }
        ]
      },
      {
        text: "Core Concepts",
        items: [
          { text: "Intent and Boundaries", link: "/core-concepts/intent-and-boundaries" },
          { text: "Determinism", link: "/core-concepts/determinism" },
          { text: "Rules Model", link: "/core-concepts/rules-model" },
          { text: "Conversion Lifecycle", link: "/core-concepts/conversion-lifecycle" }
        ]
      },
      {
        text: "Runtime Guides",
        items: [
          { text: "Runtime Selector", link: "/runtime-guides/runtime-selector" },
          { text: ".NET API Usage", link: "/runtime-guides/dotnet-api-usage" },
          { text: "TypeScript API Usage", link: "/runtime-guides/typescript-api-usage" },
          { text: "Streaming", link: "/runtime-guides/streaming" },
          { text: "Custom Transforms", link: "/runtime-guides/custom-transforms" },
          { text: "Performance and Caching", link: "/runtime-guides/performance-and-caching" }
        ]
      },
      {
        text: "Rules Reference",
        items: [
          { text: "Node Types", link: "/rules-reference/node-types" },
          { text: "Sources and Transforms", link: "/rules-reference/sources-and-transforms" },
          { text: "Condition Expressions", link: "/rules-reference/condition-expressions" },
          { text: "Merge and Collision", link: "/rules-reference/merge-and-collision" },
          { text: "Fragments and Includes", link: "/rules-reference/fragments-and-includes" },
          { text: "Validation Modes", link: "/rules-reference/validation-modes" }
        ]
      },
      {
        text: "Recipes",
        items: [
          { text: "JSON and XML", link: "/recipes/json-and-xml" },
          { text: "JSON and Query", link: "/recipes/json-and-query" },
          { text: "Arrays Branches Merge Split", link: "/recipes/arrays-branches-merge-split" },
          { text: "Diagnostics-first Authoring", link: "/recipes/diagnostics-first-authoring" }
        ]
      },
      {
        text: "Diagnostics and Troubleshooting",
        items: [
          { text: "Error Codes", link: "/diagnostics/error-codes" },
          { text: "Lint Diagnostics", link: "/diagnostics/lint-diagnostics" },
          { text: "Rule Doctor Workflow", link: "/diagnostics/rule-doctor-workflow" },
          { text: "Troubleshooting Tree", link: "/diagnostics/troubleshooting-tree" }
        ]
      },
      {
        text: "Schema and Versioning",
        items: [
          { text: "Contract", link: "/schema-versioning/contract" },
          { text: "SemVer Lockstep", link: "/schema-versioning/semver-lockstep" },
          { text: "Version Pinning", link: "/schema-versioning/version-pinning" },
          { text: "Breaking Changes", link: "/schema-versioning/breaking-changes" }
        ]
      },
      {
        text: "Parity and Testing",
        items: [
          { text: "Shared Cases", link: "/parity-testing/shared-cases" },
          { text: "Runtime Parity Workflow", link: "/parity-testing/runtime-parity-workflow" },
          { text: "Parity Gate CI", link: "/parity-testing/parity-gate-ci" },
          { text: "Add New Case", link: "/parity-testing/add-new-case" }
        ]
      },
      {
        text: "CLI and Tooling",
        items: [
          { text: "CLI Reference", link: "/cli-tooling/cli-reference" },
          { text: "Rules Bundling", link: "/cli-tooling/rules-bundling" },
          { text: "Plan Profiling", link: "/cli-tooling/plan-profiling" },
          { text: "Compatibility Checks", link: "/cli-tooling/compatibility-checks" }
        ]
      },
      {
        text: "Contributing",
        items: [
          { text: "Overview", link: "/contributing/index" },
          { text: "Local Setup", link: "/contributing/local-setup" },
          { text: "Docs Authoring Guide", link: "/contributing/docs-authoring-guide" },
          { text: "Release Flow", link: "/contributing/release-flow" },
          { text: "Governance", link: "/contributing/governance" }
        ]
      }
    ],
    socialLinks: [{ icon: "github", link: "https://github.com/jonassaa/apiconvert-core" }]
  }
});
