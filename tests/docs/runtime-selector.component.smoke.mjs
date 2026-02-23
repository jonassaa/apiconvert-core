import { readFileSync } from "node:fs";
import { join } from "node:path";

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const root = process.cwd();
const component = readFileSync(
  join(root, "docs", ".vitepress", "theme", "runtime-selector.vue"),
  "utf8"
);

assert(component.includes('@change="updateRuntime('), "Selector component must react to runtime selection changes.");
assert(component.includes("localStorage"), "Selector component must persist runtime state in localStorage.");
assert(
  component.includes('document.body.setAttribute("data-runtime"') ||
    component.includes("document.body.setAttribute('data-runtime'"),
  "Selector component must apply the runtime on the body data-runtime attribute."
);

console.log("Runtime selector component smoke test passed.");
