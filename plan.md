# Plan: improve logical-expression error clarity

## Goal
When a logical expression is invalid, return errors that clearly explain:
- where parsing failed,
- what token/operator was found,
- what the parser expected,
- and likely fixes.

## Current issue
Expression errors are technically correct but not always actionable. Users can see a generic parse failure without enough context to quickly fix syntax/operator mistakes.

## Scope
Apply improvements in both runtimes for parity:
- .NET: `src/Apiconvert.Core/Converters/MappingExecutor.ConditionExpressions.cs`
- TypeScript: `src/apiconvert-core/src/condition-expression-parser.ts`
- TypeScript tokenizer (if needed for richer context): `src/apiconvert-core/src/condition-expression-tokenizer.ts`

## Implementation steps
1. Standardize parse error shape and wording across runtimes.
2. Include expression snippet and caret pointer at failure position.
   - Example:
     - `Invalid expression at position 35: unexpected identifier 'is'.`
     - `path(name) not eq 'nora' and path(test) is 'philip'`
     - `                                   ^^`
3. Improve expected-token messages.
   - Replace broad messages like `Expected end` with specific alternatives:
   - `Expected comparison operator (==, !=, eq, not eq, gt, gte, lt, lte, in) after path(test), found 'is'.`
4. Add operator/keyword suggestion hints for near-miss inputs.
   - If token is unknown but close to a supported operator, append:
   - `Did you mean 'eq' or '=='?`
5. Detect trailing tokens explicitly.
   - If parser completes a valid sub-expression but tokens remain, report:
   - `Unexpected trailing token 'is' after complete expression.`
6. Keep evaluator errors distinct from parser errors.
   - Parse errors: syntax/operator guidance.
   - Runtime evaluation errors: type/operand guidance (e.g., non-numeric comparison).
7. Ensure all surfaced errors preserve original expression text in branch/source validation paths.

## Test updates
Add deterministic tests in both runtimes that assert message quality (not just fail/success):
- Unknown operator/alias (`is`)
- Missing right-hand operand (`path(name) ==`)
- Unclosed grouping (`not (path(x) == 1`)
- Invalid `in` RHS (`path(x) in path(y)`)
- Trailing token after valid expression

For each case, assert message includes:
- position index,
- offending token,
- expectation or hint.

## Acceptance criteria
- Invalid logical expressions produce actionable, specific errors in .NET and TS.
- Error messages are consistent in structure across runtimes.
- Existing valid expression behavior is unchanged.
- New diagnostics tests pass in both test suites.
