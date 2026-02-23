import { execSync } from "node:child_process";

const commands = [
  "node tests/docs/runtime-selector.component.smoke.mjs",
  "node tests/docs/runtime-selector.smoke.mjs",
  "node tests/docs/docs-content-quality.smoke.mjs"
];

for (const command of commands) {
  execSync(command, { stdio: "inherit" });
}

console.log("All docs smoke tests passed.");
