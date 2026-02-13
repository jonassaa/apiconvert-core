import {
  type ConditionExpressionNode,
  type ConditionToken,
  type ConditionTokenType
} from "./condition-expression-types";
import { tokenizeConditionExpression } from "./condition-expression-tokenizer";

export class ConditionExpressionParser {
  private readonly tokens: ConditionToken[];
  private index = 0;

  constructor(private readonly expression: string) {
    this.tokens = tokenizeConditionExpression(expression);
  }

  parse(): ConditionExpressionNode {
    const node = this.parseOr();
    if (this.current.type !== "end") {
      throw this.error(`Unexpected trailing token ${this.formatToken(this.current)} after complete expression.`);
    }
    return node;
  }

  private parseOr(): ConditionExpressionNode {
    let left = this.parseAnd();
    while (this.matchOperator("||") || this.matchIdentifier("or")) {
      left = { kind: "binary", operator: "||", left, right: this.parseAnd() };
    }
    return left;
  }

  private parseAnd(): ConditionExpressionNode {
    let left = this.parseComparison();
    while (this.matchOperator("&&") || this.matchIdentifier("and")) {
      left = { kind: "binary", operator: "&&", left, right: this.parseComparison() };
    }
    return left;
  }

  private parseComparison(): ConditionExpressionNode {
    let left = this.parseUnary();

    while (true) {
      const operator = this.parseComparisonOperator();
      if (!operator) {
        if (this.isUnexpectedComparisonToken(this.current)) {
          let message = `Expected comparison operator (==, !=, eq, not eq, gt, gte, lt, lte, in) after ${this.describeNode(left)}, found ${this.formatToken(this.current)}.`;
          const suggestion = this.getOperatorSuggestion(this.current.text);
          if (suggestion) {
            message += ` ${suggestion}`;
          }
          throw this.error(message);
        }
        return left;
      }

      if (
        this.current.type === "end" ||
        this.current.type === "rightParen" ||
        this.current.type === "rightBracket" ||
        this.current.type === "comma"
      ) {
        throw this.error(
          `Expected right-hand operand after operator '${operator}', found ${this.formatToken(this.current)}.`
        );
      }

      const right = this.parseUnary();
      if (operator === "in" && right.kind !== "array") {
        throw this.error("Right-hand side of 'in' must be an array literal.");
      }
      left = { kind: "binary", operator, left, right };
    }
  }

  private parseComparisonOperator():
    | "=="
    | "!="
    | ">"
    | ">="
    | "<"
    | "<="
    | "in"
    | null {
    if (this.matchOperator("==")) return "==";
    if (this.matchOperator("!=")) return "!=";
    if (this.matchOperator(">=")) return ">=";
    if (this.matchOperator("<=")) return "<=";
    if (this.matchOperator(">")) return ">";
    if (this.matchOperator("<")) return "<";
    if (this.matchIdentifier("eq")) return "==";
    if (this.matchNotEqAlias()) return "!=";
    if (this.matchIdentifier("gt")) return ">";
    if (this.matchIdentifier("gte")) return ">=";
    if (this.matchIdentifier("lt")) return "<";
    if (this.matchIdentifier("lte")) return "<=";
    if (this.matchIdentifier("in")) return "in";
    return null;
  }

  private matchNotEqAlias(): boolean {
    if (!this.peekIdentifier("not")) return false;
    const next = this.peek(1);
    if (next.type !== "identifier" || next.text.toLowerCase() !== "eq") {
      return false;
    }
    this.index += 2;
    return true;
  }

  private parseUnary(): ConditionExpressionNode {
    if (this.matchOperator("!") || this.matchIdentifier("not")) {
      return { kind: "unary", operand: this.parseUnary() };
    }
    return this.parsePrimary();
  }

  private parsePrimary(): ConditionExpressionNode {
    if (this.match("leftParen")) {
      const node = this.parseOr();
      this.expect("rightParen");
      return node;
    }

    if (this.match("leftBracket")) {
      const items: ConditionExpressionNode[] = [];
      if (!this.match("rightBracket")) {
        do {
          items.push(this.parseOr());
        } while (this.match("comma"));
        this.expect("rightBracket");
      }
      return { kind: "array", items };
    }

    if (this.current.type === "number") {
      const text = this.current.text;
      this.index += 1;
      const number = Number.parseFloat(text);
      if (Number.isNaN(number) || !isFinite(number)) {
        throw this.error(`Invalid number literal '${text}'.`);
      }
      return { kind: "literal", value: number };
    }

    if (this.current.type === "string") {
      const text = this.current.text;
      this.index += 1;
      return { kind: "literal", value: text };
    }

    if (this.current.type === "identifier") {
      const identifier = this.current.text;
      this.index += 1;
      const lowered = identifier.toLowerCase();

      if (lowered === "true") return { kind: "literal", value: true };
      if (lowered === "false") return { kind: "literal", value: false };
      if (lowered === "null") return { kind: "literal", value: null };
      if (lowered === "path") return this.parsePathCall();
      if (lowered === "exists") return this.parseExistsCall();

      throw this.error(`Unexpected identifier '${identifier}'. Use path(...) for value references.`);
    }

    throw this.error(`Unexpected token '${this.current.text}'.`);
  }

  private parsePathCall(): ConditionExpressionNode {
    this.expect("leftParen");
    if (this.current.type !== "identifier" && this.current.type !== "string") {
      throw this.error("path(...) requires a path reference.");
    }
    const path = this.current.text;
    this.index += 1;
    this.expect("rightParen");
    return { kind: "path", path };
  }

  private parseExistsCall(): ConditionExpressionNode {
    this.expect("leftParen");
    const argument = this.parseOr();
    this.expect("rightParen");
    return { kind: "exists", argument };
  }

  private get current(): ConditionToken {
    return this.peek(0);
  }

  private peek(offset: number): ConditionToken {
    const cursor = this.index + offset;
    return cursor >= this.tokens.length ? this.tokens[this.tokens.length - 1] : this.tokens[cursor];
  }

  private match(type: ConditionTokenType): boolean {
    if (this.current.type !== type) return false;
    this.index += 1;
    return true;
  }

  private expect(type: ConditionTokenType): void {
    if (!this.match(type)) {
      throw this.error(`Expected ${this.describeTokenType(type)} but found ${this.formatToken(this.current)}.`);
    }
  }

  private matchOperator(operator: string): boolean {
    if (this.current.type !== "operator" || this.current.text !== operator) return false;
    this.index += 1;
    return true;
  }

  private matchIdentifier(identifier: string): boolean {
    if (!this.peekIdentifier(identifier)) return false;
    this.index += 1;
    return true;
  }

  private peekIdentifier(identifier: string): boolean {
    return (
      this.current.type === "identifier" && this.current.text.toLowerCase() === identifier.toLowerCase()
    );
  }

  private isUnexpectedComparisonToken(token: ConditionToken): boolean {
    if (
      token.type === "end" ||
      token.type === "rightParen" ||
      token.type === "rightBracket" ||
      token.type === "comma"
    ) {
      return false;
    }

    if (token.type === "operator" && (token.text === "&&" || token.text === "||")) {
      return false;
    }

    if (
      token.type === "identifier" &&
      (token.text.toLowerCase() === "and" || token.text.toLowerCase() === "or")
    ) {
      return false;
    }

    return true;
  }

  private getOperatorSuggestion(tokenText: string): string | null {
    const normalized = tokenText.trim().toLowerCase();
    if (!normalized) {
      return null;
    }

    if (normalized === "is" || normalized === "equals") {
      return "Did you mean 'eq' or '=='?";
    }

    if (normalized === "neq") {
      return "Did you mean 'not eq' or '!='?";
    }

    return this.getClosestOperatorSuggestion(normalized);
  }

  private getClosestOperatorSuggestion(tokenText: string): string | null {
    const candidates = ["eq", "gt", "gte", "lt", "lte", "in"] as const;
    let best: string | null = null;
    let bestDistance = Number.POSITIVE_INFINITY;

    for (const candidate of candidates) {
      const distance = this.computeDistance(tokenText, candidate);
      if (distance < bestDistance) {
        bestDistance = distance;
        best = candidate;
      }
    }

    if (!best || bestDistance > 2) {
      return null;
    }

    if (best === "eq") {
      return "Did you mean 'eq' or '=='?";
    }

    return `Did you mean '${best}'?`;
  }

  private computeDistance(source: string, target: string): number {
    const costs: number[] = [];
    for (let index = 0; index <= target.length; index += 1) {
      costs[index] = index;
    }

    for (let sourceIndex = 1; sourceIndex <= source.length; sourceIndex += 1) {
      costs[0] = sourceIndex;
      let previousDiagonal = sourceIndex - 1;
      for (let targetIndex = 1; targetIndex <= target.length; targetIndex += 1) {
        const previousTop = costs[targetIndex];
        const substitutionCost = source[sourceIndex - 1] === target[targetIndex - 1] ? 0 : 1;
        costs[targetIndex] = Math.min(
          Math.min(costs[targetIndex] + 1, costs[targetIndex - 1] + 1),
          previousDiagonal + substitutionCost
        );
        previousDiagonal = previousTop;
      }
    }

    return costs[target.length];
  }

  private describeNode(node: ConditionExpressionNode): string {
    if (node.kind === "path") {
      return `path(${node.path})`;
    }

    if (node.kind === "literal") {
      if (node.value == null) return "null";
      if (typeof node.value === "string") return `'${node.value}'`;
      if (typeof node.value === "boolean") return node.value ? "true" : "false";
      return String(node.value);
    }

    return "expression";
  }

  private describeTokenType(type: ConditionTokenType): string {
    switch (type) {
      case "leftParen":
        return "'('";
      case "rightParen":
        return "')'";
      case "leftBracket":
        return "'['";
      case "rightBracket":
        return "']'";
      case "comma":
        return "','";
      case "end":
        return "end of expression";
      default:
        return type;
    }
  }

  private formatToken(token: ConditionToken): string {
    return token.type === "end" ? "end of expression" : `'${token.text}'`;
  }

  private error(message: string): Error {
    return this.errorAt(message, this.current.position, this.current.text.length);
  }

  private errorAt(message: string, position: number, pointerWidth: number): Error {
    const safePosition = Math.max(0, Math.min(position, this.expression.length));
    const safeWidth = Math.max(2, pointerWidth > 0 ? pointerWidth : 1);
    const pointer = `${" ".repeat(safePosition)}${"^".repeat(safeWidth)}`;
    return new Error(
      `Invalid expression at position ${safePosition}: ${message}\n${this.expression}\n${pointer}`
    );
  }
}
