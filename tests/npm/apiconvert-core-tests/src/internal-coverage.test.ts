import assert from "node:assert/strict";
import path from "node:path";
import test from "node:test";

const repoRoot = path.resolve(__dirname, "../../../..");

test("condition tokenizer and parser cover success and error branches", () => {
  const { tokenizeConditionExpression } = require(path.join(
    repoRoot,
    "src",
    "apiconvert-core",
    "dist",
    "condition-expression-tokenizer.js"
  )) as { tokenizeConditionExpression: (expression: string) => Array<{ type: string; text: string }> };
  const { ConditionExpressionParser } = require(path.join(
    repoRoot,
    "src",
    "apiconvert-core",
    "dist",
    "condition-expression-parser.js"
  )) as { ConditionExpressionParser: new (expression: string) => { parse: () => unknown } };

  const tokens = tokenizeConditionExpression("path(user.age) gte .5 and 'x\\'y' == 'x\\'y'");
  assert.ok(tokens.some((token) => token.type === "number" && token.text === ".5"));
  assert.ok(tokens.some((token) => token.type === "identifier" && token.text === "gte"));

  const parser = new ConditionExpressionParser("path(age) not eq 12 or exists(path(name))");
  assert.ok(parser.parse());

  assert.throws(() => tokenizeConditionExpression("1..2"), /Invalid number literal/);
  assert.throws(() => tokenizeConditionExpression("'unterminated"), /Unterminated string literal/);
  assert.throws(() => tokenizeConditionExpression("@"), /Unexpected character/);

  assert.throws(() => new ConditionExpressionParser("path(name) in 'x'").parse(), /must be an array literal/);
  assert.throws(() => new ConditionExpressionParser("path(name) =").parse(), /Unexpected character '='/);
  assert.throws(() => new ConditionExpressionParser("path(name) == ").parse(), /Expected right-hand operand/);
  assert.throws(() => new ConditionExpressionParser("path(name) is 'Ada'").parse(), /Did you mean 'eq' or '=='/);
  assert.throws(() => new ConditionExpressionParser("path()").parse(), /requires a path reference/);
});

test("query payload internals cover key parsing and formatting branches", () => {
  const { parseQueryString, formatQueryString } = require(path.join(
    repoRoot,
    "src",
    "apiconvert-core",
    "dist",
    "query-payload.js"
  )) as {
    parseQueryString: (text: string) => Record<string, unknown>;
    formatQueryString: (value: unknown) => string;
  };

  assert.deepEqual(parseQueryString(""), {});
  assert.deepEqual(parseQueryString("plain"), { plain: "" });
  assert.deepEqual(parseQueryString("list[2]=v&obj.child=1"), { list: [null, null, "v"], obj: { child: "1" } });

  const formatted = formatQueryString({
    "": "skip",
    blank: null,
    scalar: 1,
    flags: [true, null],
    obj: { child: { deep: "v" } },
    arr: [{ x: 1 }]
  });

  assert.ok(formatted.includes("blank="));
  assert.ok(formatted.includes("scalar=1"));
  assert.ok(formatted.includes("flags=true"));
  assert.ok(formatted.includes("flags="));
  assert.ok(formatted.includes("obj.child.deep=v"));
  assert.ok(formatted.includes("arr=%7B%22x%22%3A1%7D"));
  assert.throws(() => formatQueryString("nope"), /must be an object/);
});

test("source resolver internals cover recursion, unsupported and transform branches", () => {
  const { resolveSourceValue, resolvePathValue } = require(path.join(
    repoRoot,
    "src",
    "apiconvert-core",
    "dist",
    "source-resolver.js"
  )) as {
    resolveSourceValue: (
      root: unknown,
      item: unknown,
      source: Record<string, unknown>,
      errors: string[],
      diagnostics: Array<{ code: string; message: string }>,
      transforms: Record<string, (value: unknown) => unknown>,
      errorContext: string,
      depth?: number
    ) => unknown;
    resolvePathValue: (root: unknown, item: unknown, pathText: string) => unknown;
  };

  const diagnostics: Array<{ code: string; message: string }> = [];
  const errors: string[] = [];

  const recursion = resolveSourceValue(
    {},
    {},
    { type: "constant", value: "x" },
    errors,
    diagnostics,
    {},
    "ctx",
    33
  );
  assert.equal(recursion, null);
  assert.ok(diagnostics.some((entry) => entry.code === "ACV-RUN-800"));

  const unsupported = resolveSourceValue({}, {}, { type: "unknown" }, errors, diagnostics, {}, "ctx");
  assert.equal(unsupported, null);
  assert.ok(diagnostics.some((entry) => entry.code === "ACV-RUN-203"));

  const splitOutOfRange = resolveSourceValue(
    { name: "Ada Lovelace" },
    null,
    { type: "transform", transform: "split", path: "name", separator: " ", tokenIndex: 99 },
    errors,
    diagnostics,
    {},
    "ctx"
  );
  assert.equal(splitOutOfRange, null);

  const splitNoSeparator = resolveSourceValue(
    { name: "Ada Lovelace" },
    null,
    { type: "transform", transform: "split", path: "name", separator: "" },
    errors,
    diagnostics,
    {},
    "ctx"
  );
  assert.equal(splitNoSeparator, null);

  const customThrow = resolveSourceValue(
    { name: "Ada" },
    null,
    { type: "transform", path: "name", customTransform: "boom" },
    errors,
    diagnostics,
    { boom: () => { throw new Error("boom"); } },
    "ctx"
  );
  assert.equal(customThrow, null);
  assert.ok(diagnostics.some((entry) => entry.code === "ACV-RUN-202"));

  const rootObject = { root: true };
  assert.equal(resolvePathValue(rootObject, null, "$"), rootObject);
  assert.equal(resolvePathValue({ user: { id: 1 } }, null, "$.user.id"), 1);
  assert.equal(resolvePathValue({ users: [{ id: 2 }] }, null, "$.users[0].id"), 2);
  assert.equal(resolvePathValue({ fromRoot: 3 }, { local: 4 }, "local"), 4);
  assert.equal(resolvePathValue({ fromRoot: 3 }, null, "fromRoot"), 3);
});
