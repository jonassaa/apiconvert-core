import { toNumber } from "./core-utils";
import { type ConditionExpressionNode, type ResolvePath } from "./condition-expression-types";

export function evaluateConditionExpression(node: ConditionExpressionNode, resolvePath: ResolvePath): unknown {
  switch (node.kind) {
    case "literal":
      return node.value;
    case "path":
      return resolvePath(node.path);
    case "array":
      return node.items.map((entry) => evaluateConditionExpression(entry, resolvePath));
    case "exists":
      return evaluateConditionExpression(node.argument, resolvePath) != null;
    case "unary":
      return !toBoolean(evaluateConditionExpression(node.operand, resolvePath));
    case "binary":
      return evaluateConditionBinary(node, resolvePath);
    default:
      throw new Error("Unsupported condition expression node.");
  }
}

export function toBoolean(value: unknown): boolean {
  if (value == null) return false;
  if (typeof value === "boolean") return value;
  if (typeof value === "string") return value.length > 0;
  return true;
}

function evaluateConditionBinary(
  node: Extract<ConditionExpressionNode, { kind: "binary" }>,
  resolvePath: ResolvePath
): unknown {
  if (node.operator === "&&") {
    return (
      toBoolean(evaluateConditionExpression(node.left, resolvePath)) &&
      toBoolean(evaluateConditionExpression(node.right, resolvePath))
    );
  }

  if (node.operator === "||") {
    return (
      toBoolean(evaluateConditionExpression(node.left, resolvePath)) ||
      toBoolean(evaluateConditionExpression(node.right, resolvePath))
    );
  }

  const left = evaluateConditionExpression(node.left, resolvePath);
  const right = evaluateConditionExpression(node.right, resolvePath);

  switch (node.operator) {
    case "==":
      return Object.is(left, right);
    case "!=":
      return !Object.is(left, right);
    case ">":
      return toNumber(left) > toNumber(right);
    case ">=":
      return toNumber(left) >= toNumber(right);
    case "<":
      return toNumber(left) < toNumber(right);
    case "<=":
      return toNumber(left) <= toNumber(right);
    case "in":
      return Array.isArray(right) ? right.some((entry) => Object.is(left, entry)) : false;
    default:
      throw new Error(`Unsupported condition operator '${node.operator}'.`);
  }
}
