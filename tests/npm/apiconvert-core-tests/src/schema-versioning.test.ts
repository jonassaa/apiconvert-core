import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";

test("current schema matches latest versioned schema", () => {
  const repoRoot = locateRepoRoot();
  const schemasRoot = path.join(repoRoot, "schemas", "rules");
  const versions = fs
    .readdirSync(schemasRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory() && /^v\d+\.\d+\.\d+$/.test(entry.name))
    .map((entry) => entry.name)
    .sort(semverCompare);

  assert.ok(versions.length > 0, "No versioned schemas found under schemas/rules.");

  const latestVersion = versions[versions.length - 1];
  const latestSchemaPath = path.join(schemasRoot, latestVersion, "schema.json");
  const currentSchemaPath = path.join(schemasRoot, "current", "schema.json");

  assert.ok(fs.existsSync(latestSchemaPath), `Missing latest versioned schema: ${latestSchemaPath}`);
  assert.ok(fs.existsSync(currentSchemaPath), `Missing current schema alias: ${currentSchemaPath}`);

  const latestText = fs.readFileSync(latestSchemaPath, "utf8");
  const currentText = fs.readFileSync(currentSchemaPath, "utf8");
  assert.equal(currentText, latestText);
});

test("legacy schema alias remains available and parseable", () => {
  const repoRoot = locateRepoRoot();
  const legacySchemaPath = path.join(repoRoot, "schemas", "rules", "rules.schema.json");
  assert.ok(fs.existsSync(legacySchemaPath), `Missing legacy schema alias: ${legacySchemaPath}`);

  const parsed = JSON.parse(fs.readFileSync(legacySchemaPath, "utf8")) as Record<string, unknown>;
  assert.equal(parsed.type, "object");
  assert.ok(parsed.$defs && typeof parsed.$defs === "object");
  assert.ok(parsed.properties && typeof parsed.properties === "object");
});

function locateRepoRoot(): string {
  let current = __dirname;

  while (true) {
    const gitDirectory = path.join(current, ".git");
    if (fs.existsSync(gitDirectory)) {
      return current;
    }

    const next = path.dirname(current);
    if (next === current) {
      break;
    }

    current = next;
  }

  throw new Error("Could not locate repository root.");
}

function semverCompare(a: string, b: string): number {
  const left = parseVersion(a);
  const right = parseVersion(b);

  if (left.major !== right.major) return left.major - right.major;
  if (left.minor !== right.minor) return left.minor - right.minor;
  return left.patch - right.patch;
}

function parseVersion(tag: string): { major: number; minor: number; patch: number } {
  const match = /^v(\d+)\.(\d+)\.(\d+)$/.exec(tag);
  if (!match) {
    throw new Error(`Invalid schema version directory: ${tag}`);
  }

  return {
    major: Number(match[1]),
    minor: Number(match[2]),
    patch: Number(match[3])
  };
}
