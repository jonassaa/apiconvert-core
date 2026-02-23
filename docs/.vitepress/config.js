import { defineConfig } from "vitepress";

const base = "/apiconvert-core/";

export default defineConfig({
  base,
  srcExclude: ["plans/**"],
  title: "Apiconvert.Core",
  description: "Rule-driven API conversion docs for .NET and TypeScript",
  lang: "en-US",
  lastUpdated: true,
  head: [
    ["link", { rel: "icon", href: `${base}favicon.ico` }],
    [
      "script",
      {},
      "(function(){try{var key='apiconvert.docs.runtime';var saved=localStorage.getItem(key);var runtime=saved==='typescript'?'typescript':'dotnet';document.documentElement.setAttribute('data-runtime',runtime);}catch(_){}})();"
    ]
  ],
  themeConfig: {
    logo: "/logo.svg",
    search: {
      provider: "local"
    },
    nav: [
      { text: "Start Here", link: "/start-here/install" },
      { text: "Guide", link: "/start-here/conversion-lifecycle" },
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
          { text: "Conversion Lifecycle", link: "/start-here/conversion-lifecycle" }
        ]
      },
      {
        text: "Runtime Guides",
        items: [
          { text: "API Usage", link: "/runtime-guides/api-usage" },
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
        text: "CLI and Tooling",
        items: [
          { text: "CLI Reference", link: "/cli-tooling/cli-reference" },
          { text: "Rules Bundling", link: "/cli-tooling/rules-bundling" },
          { text: "Plan Profiling", link: "/cli-tooling/plan-profiling" },
          { text: "Compatibility Checks", link: "/cli-tooling/compatibility-checks" }
        ]
      },
    ],
    socialLinks: [{ icon: "github", link: "https://github.com/jonassaa/apiconvert-core" }]
  }
});
