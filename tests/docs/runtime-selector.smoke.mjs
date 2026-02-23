import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const root = process.cwd();
const distRoot = join(root, "docs", ".vitepress", "dist");
const indexHtml = readFileSync(join(distRoot, "index.html"), "utf8");
const selectorCss = readFileSync(join(root, "docs", ".vitepress", "theme", "custom.css"), "utf8");
const configJs = readFileSync(join(root, "docs", ".vitepress", "config.js"), "utf8");

function getHtmlFiles(dir) {
  const entries = readdirSync(dir, { withFileTypes: true });
  return entries.flatMap((entry) => {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      return getHtmlFiles(fullPath);
    }

    return entry.isFile() && entry.name.endsWith(".html") ? [fullPath] : [];
  });
}

assert(indexHtml.includes("id=\"runtime-selector\""), "Missing runtime selector element in built HTML.");
assert(indexHtml.includes("value=\"dotnet\""), "Missing .NET option in runtime selector.");
assert(indexHtml.includes("value=\"typescript\""), "Missing TypeScript option in runtime selector.");

const htmlFiles = getHtmlFiles(distRoot);
for (const htmlFile of htmlFiles) {
  assert(!htmlFile.includes("/plans/"), `Internal plans page should not be published: ${htmlFile}`);
}

for (const htmlFile of htmlFiles) {
  if (htmlFile.endsWith("/404.html")) {
    continue;
  }

  const html = readFileSync(htmlFile, "utf8");
  assert(html.includes("id=\"runtime-selector\""), `Missing runtime selector on page: ${htmlFile}`);
}

assert(
  (selectorCss.includes('html[data-runtime="dotnet"] .runtime-typescript') ||
    selectorCss.includes('body[data-runtime="dotnet"] .runtime-typescript')) &&
    selectorCss.includes('display: none !important'),
  "Selector CSS must hide TypeScript blocks when .NET is selected."
);
assert(
  (selectorCss.includes('html[data-runtime="typescript"] .runtime-dotnet') ||
    selectorCss.includes('body[data-runtime="typescript"] .runtime-dotnet')) &&
    selectorCss.includes('display: none !important'),
  "Selector CSS must hide .NET blocks when TypeScript is selected."
);
assert(
  configJs.includes("apiconvert.docs.runtime"),
  "Docs config must reference the runtime selector storage key for persisted runtime."
);

console.log("Runtime selector smoke test passed.");
