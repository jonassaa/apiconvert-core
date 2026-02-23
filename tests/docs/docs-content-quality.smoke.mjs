import { readFileSync, readdirSync } from "node:fs";
import { join, relative } from "node:path";

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const root = process.cwd();
const docsRoot = join(root, "docs");

function getMarkdownFiles(dir) {
  const entries = readdirSync(dir, { withFileTypes: true });
  return entries.flatMap((entry) => {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === "node_modules" || entry.name === ".vitepress") {
        return [];
      }

      return getMarkdownFiles(fullPath);
    }

    return entry.isFile() && entry.name.endsWith(".md") ? [fullPath] : [];
  });
}

const markdownFiles = getMarkdownFiles(docsRoot).filter((file) => !file.includes(`${join("docs", "plans")}`));

for (const file of markdownFiles) {
  const rel = relative(root, file);
  const text = readFileSync(file, "utf8");

  const dotnetCount = (text.match(/runtime-dotnet/g) ?? []).length;
  const typescriptCount = (text.match(/runtime-typescript/g) ?? []).length;

  if (dotnetCount > 0 || typescriptCount > 0) {
    assert(
      dotnetCount > 0 && typescriptCount > 0,
      `Runtime-tag imbalance in ${rel}: dotnet=${dotnetCount}, typescript=${typescriptCount}`
    );
  }
}

console.log("Docs content quality smoke test passed.");
