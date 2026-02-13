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
    this.expect("end");
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
        return left;
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
      throw this.error(`Expected ${type} but found '${this.current.text}'.`);
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

  private error(message: string): Error {
    return new Error(`${message} (position ${this.current.position}).`);
  }
}
