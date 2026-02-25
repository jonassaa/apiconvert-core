import { defineConfig } from "vitepress";

const configuredBase = process.env.DOCS_BASE;
const base = configuredBase
  ? configuredBase.endsWith("/")
    ? configuredBase
    : `${configuredBase}/`
  : "/apiconvert-core/";

export default defineConfig({
  base,
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
      { text: "Overview", link: "/overview/what-is-apiconvert-core" },
      { text: "Get Started", link: "/getting-started/" },
      { text: "Guides", link: "/guides/runtime-api" },
      { text: "Reference", link: "/reference/rules-schema" },
      { text: "GitHub", link: "https://github.com/jonassaa/apiconvert-core" }
    ],
    sidebar: [
      {
        text: "Start",
        items: [
          { text: "Home", link: "/" },
          { text: "What Is Apiconvert.Core?", link: "/overview/what-is-apiconvert-core" },
          { text: "Getting Started", link: "/getting-started/" },
          { text: "First Conversion", link: "/getting-started/first-conversion" }
        ]
      },
      {
        text: "Core Concepts",
        items: [
          { text: "Rules Model", link: "/concepts/rules-model" },
          { text: "Conversion Lifecycle", link: "/concepts/conversion-lifecycle" },
          { text: "Determinism and Parity", link: "/concepts/determinism-and-parity" }
        ]
      },
      {
        text: "How-To Guides",
        items: [
          { text: "Runtime APIs", link: "/guides/runtime-api" },
          { text: "Streaming", link: "/guides/streaming" },
          { text: "Custom Transforms", link: "/guides/custom-transforms" },
          { text: "Performance and Caching", link: "/guides/performance-and-caching" }
        ]
      },
      {
        text: "Reference",
        items: [
          { text: "Rules Schema", link: "/reference/rules-schema" },
          { text: "Rule Nodes", link: "/reference/rule-nodes" },
          { text: "Sources and Transforms", link: "/reference/sources-and-transforms" },
          { text: "Condition Expressions", link: "/reference/conditions" },
          { text: "Merge and Collision", link: "/reference/merge-and-collision" },
          { text: "Fragments and Includes", link: "/reference/fragments-and-includes" },
          { text: "Validation and Diagnostics", link: "/reference/validation-and-diagnostics" },
          { text: "CLI", link: "/reference/cli" }
        ]
      },
      {
        text: "Recipes",
        items: [
          { text: "Hello World", link: "/recipes/hello-world" },
          { text: "JSON and XML", link: "/recipes/json-and-xml" },
          { text: "JSON and Query", link: "/recipes/json-and-query" },
          { text: "Arrays, Branches, Merge, Split", link: "/recipes/arrays-branches-merge-split" }
        ]
      },
      {
        text: "Troubleshooting",
        items: [
          { text: "Error Codes", link: "/troubleshooting/error-codes" },
          { text: "Troubleshooting Tree", link: "/troubleshooting/troubleshooting-tree" }
        ]
      }
    ],
    socialLinks: [{ icon: "github", link: "https://github.com/jonassaa/apiconvert-core" }]
  }
});
