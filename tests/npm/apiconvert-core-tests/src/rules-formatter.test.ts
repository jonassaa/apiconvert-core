import assert from "node:assert/strict";
import test from "node:test";
import { formatConversionRules } from "@apiconvert/core";

test("formatConversionRules emits canonical deterministic JSON", () => {
  const formatted = formatConversionRules({
    rules: [
      { kind: "field", from: "name", to: ["user.name"] },
      { kind: "field", to: ["meta.source"], const: "crm" }
    ]
  });

  assert.equal(
    formatted,
    [
      "{",
      '  "rules": [',
      "    {",
      '      "kind": "field",',
      '      "outputPaths": [',
      '        "user.name"',
      "      ],",
      '      "source": {',
      '        "type": "path",',
      '        "path": "name"',
      "      }",
      "    },",
      "    {",
      '      "kind": "field",',
      '      "outputPaths": [',
      '        "meta.source"',
      "      ],",
      '      "source": {',
      '        "type": "constant",',
      '        "value": "crm"',
      "      }",
      "    }",
      "  ]",
      "}"
    ].join("\n")
  );
});
