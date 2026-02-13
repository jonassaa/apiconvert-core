export type ConditionExpressionNode =
  | { kind: "literal"; value: unknown }
  | { kind: "path"; path: string }
  | { kind: "array"; items: ConditionExpressionNode[] }
  | { kind: "exists"; argument: ConditionExpressionNode }
  | { kind: "unary"; operand: ConditionExpressionNode }
  | {
      kind: "binary";
      operator: "==" | "!=" | ">" | ">=" | "<" | "<=" | "in" | "&&" | "||";
      left: ConditionExpressionNode;
      right: ConditionExpressionNode;
    };

export type ConditionTokenType =
  | "identifier"
  | "number"
  | "string"
  | "operator"
  | "leftParen"
  | "rightParen"
  | "leftBracket"
  | "rightBracket"
  | "comma"
  | "end";

export interface ConditionToken {
  type: ConditionTokenType;
  text: string;
  position: number;
}

export type ResolvePath = (path: string) => unknown;
