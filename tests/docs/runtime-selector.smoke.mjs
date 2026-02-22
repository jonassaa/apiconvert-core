import { readFileSync } from "node:fs";
import { join } from "node:path";

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const root = process.cwd();
const indexHtml = readFileSync(join(root, "site", "index.html"), "utf8");
const selectorJs = readFileSync(join(root, "site", "assets", "javascripts", "runtime-selector.js"), "utf8");
const selectorCss = readFileSync(join(root, "site", "assets", "stylesheets", "runtime-selector.css"), "utf8");

assert(indexHtml.includes("id=\"runtime-selector\""), "Missing runtime selector element in built HTML.");
assert(indexHtml.includes("value=\"dotnet\""), "Missing .NET option in runtime selector.");
assert(indexHtml.includes("value=\"typescript\""), "Missing TypeScript option in runtime selector.");

assert(selectorJs.includes("localStorage"), "Selector JS must persist runtime state in localStorage.");
assert(selectorJs.includes("data-runtime"), "Selector JS must apply data-runtime body attribute.");

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
