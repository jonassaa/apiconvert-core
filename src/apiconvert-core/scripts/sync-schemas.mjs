import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const packageRoot = path.resolve(__dirname, "..");
const sourceRulesRoot = path.resolve(packageRoot, "../../schemas/rules");
const targetRulesRoot = path.resolve(packageRoot, "schemas/rules");

await fs.rm(targetRulesRoot, { recursive: true, force: true });
await fs.mkdir(path.dirname(targetRulesRoot), { recursive: true });
await fs.cp(sourceRulesRoot, targetRulesRoot, { recursive: true });
