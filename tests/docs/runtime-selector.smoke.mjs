import { readFileSync } from "node:fs";
import { join } from "node:path";

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const root = process.cwd();
const indexHtml = readFileSync(join(root, "docs", ".vitepress", "dist", "index.html"), "utf8");
const selectorCss = readFileSync(join(root, "docs", ".vitepress", "theme", "custom.css"), "utf8");

assert(indexHtml.includes("id=\"runtime-selector\""), "Missing runtime selector element in built HTML.");
assert(indexHtml.includes("value=\"dotnet\""), "Missing .NET option in runtime selector.");
assert(indexHtml.includes("value=\"typescript\""), "Missing TypeScript option in runtime selector.");

assert(
  selectorCss.includes('body[data-runtime="dotnet"] .runtime-typescript') &&
    selectorCss.includes('display: none !important'),
  "Selector CSS must hide TypeScript blocks when .NET is selected."
);
assert(
  selectorCss.includes('body[data-runtime="typescript"] .runtime-dotnet') &&
    selectorCss.includes('display: none !important'),
  "Selector CSS must hide .NET blocks when TypeScript is selected."
);

console.log("Runtime selector smoke test passed.");
