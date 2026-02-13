import { evaluateConditionExpression, toBoolean } from "./condition-expression-evaluator";
import { ConditionExpressionParser } from "./condition-expression-parser";
import { type ResolvePath } from "./condition-expression-types";

export function tryEvaluateConditionExpression(
  expression: string,
  resolvePath: ResolvePath
): { ok: true; value: boolean } | { ok: false; error: string } {
  try {
    const parser = new ConditionExpressionParser(expression);
    const ast = parser.parse();
    return { ok: true, value: toBoolean(evaluateConditionExpression(ast, resolvePath)) };
  } catch (error) {
    return { ok: false, error: error instanceof Error ? error.message : String(error) };
  }
}
