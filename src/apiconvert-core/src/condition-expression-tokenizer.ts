import { type ConditionToken } from "./condition-expression-types";

export function tokenizeConditionExpression(expression: string): ConditionToken[] {
  const tokens: ConditionToken[] = [];
  let index = 0;

  while (index < expression.length) {
    const current = expression[index];
    if (/\s/.test(current)) {
      index += 1;
      continue;
    }

    if (current === "'") {
      const parsed = readQuotedConditionString(expression, index);
      tokens.push({ type: "string", text: parsed.value, position: index });
      index = parsed.nextIndex;
      continue;
    }

    if (/\d/.test(current) || (current === "." && index + 1 < expression.length && /\d/.test(expression[index + 1]))) {
      const start = index;
      index += 1;
      while (index < expression.length && /[\d.]/.test(expression[index])) {
        index += 1;
      }
      tokens.push({ type: "number", text: expression.slice(start, index), position: start });
      continue;
    }

    const twoChar = expression.slice(index, index + 2);
    if (["&&", "||", "==", "!=", ">=", "<="].includes(twoChar)) {
      tokens.push({ type: "operator", text: twoChar, position: index });
      index += 2;
      continue;
    }

    if (["!", ">", "<"].includes(current)) {
      tokens.push({ type: "operator", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === "(") {
      tokens.push({ type: "leftParen", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === ")") {
      tokens.push({ type: "rightParen", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === "[") {
      tokens.push({ type: "leftBracket", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === "]") {
      tokens.push({ type: "rightBracket", text: current, position: index });
      index += 1;
      continue;
    }

    if (current === ",") {
      tokens.push({ type: "comma", text: current, position: index });
      index += 1;
      continue;
    }

    if (isConditionIdentifierStart(current)) {
      const start = index;
      index += 1;
      while (index < expression.length && isConditionIdentifierPart(expression[index])) {
        index += 1;
      }
      tokens.push({ type: "identifier", text: expression.slice(start, index), position: start });
      continue;
    }

    throw createTokenizerError(expression, `Unexpected character '${current}'.`, index, 1);
  }

  tokens.push({ type: "end", text: "", position: expression.length });
  return tokens;
}

function isConditionIdentifierStart(value: string): boolean {
  return /[A-Za-z_$]/.test(value);
}

function isConditionIdentifierPart(value: string): boolean {
  return /[A-Za-z0-9_.$[\]]/.test(value);
}

function readQuotedConditionString(
  expression: string,
  startIndex: number
): { value: string; nextIndex: number } {
  let output = "";
  let cursor = startIndex + 1;

  while (cursor < expression.length) {
    const current = expression[cursor];
    if (current === "'") {
      return { value: output, nextIndex: cursor + 1 };
    }

    if (current === "\\" && cursor + 1 < expression.length) {
      cursor += 1;
      output += expression[cursor];
      cursor += 1;
      continue;
    }

    output += current;
    cursor += 1;
  }

  throw createTokenizerError(expression, "Unterminated string literal.", startIndex, 1);
}

function createTokenizerError(
  expression: string,
  message: string,
  position: number,
  pointerWidth: number
): Error {
  const safePosition = Math.max(0, Math.min(position, expression.length));
  const safeWidth = Math.max(2, pointerWidth > 0 ? pointerWidth : 1);
  const pointer = `${" ".repeat(safePosition)}${"^".repeat(safeWidth)}`;
  return new Error(`Invalid expression at position ${safePosition}: ${message}\n${expression}\n${pointer}`);
}
