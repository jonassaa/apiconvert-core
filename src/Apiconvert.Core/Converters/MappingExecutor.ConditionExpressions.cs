using System.Globalization;
using System.Text;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static partial class MappingExecutor
{
    private static bool TryEvaluateConditionExpression(
        object? root,
        object? item,
        string expression,
        out bool matched,
        out string error)
    {
        try
        {
            var parser = new ConditionExpressionParser(expression);
            var ast = parser.Parse();
            var result = EvaluateConditionExpressionNode(ast, root, item);
            matched = ToBoolean(result);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            matched = false;
            error = ex.Message;
            return false;
        }
    }

    private static object? EvaluateConditionExpressionNode(ConditionExpressionNode node, object? root, object? item)
    {
        return node switch
        {
            ConditionLiteralNode literal => literal.Value,
            ConditionPathNode path => ResolvePathValue(root, item, path.Path),
            ConditionArrayNode array => array.Items.Select(child => EvaluateConditionExpressionNode(child, root, item)).ToList(),
            ConditionExistsNode exists => EvaluateConditionExpressionNode(exists.Argument, root, item) != null,
            ConditionUnaryNode unary => unary.Operator switch
            {
                "!" => !ToBoolean(EvaluateConditionExpressionNode(unary.Operand, root, item)),
                _ => throw new InvalidOperationException($"Unsupported unary operator '{unary.Operator}'.")
            },
            ConditionBinaryNode binary => EvaluateConditionBinaryNode(binary, root, item),
            _ => throw new InvalidOperationException("Unsupported condition expression node.")
        };
    }

    private static object? EvaluateConditionBinaryNode(ConditionBinaryNode binary, object? root, object? item)
    {
        if (binary.Operator == "&&")
        {
            return ToBoolean(EvaluateConditionExpressionNode(binary.Left, root, item))
                && ToBoolean(EvaluateConditionExpressionNode(binary.Right, root, item));
        }

        if (binary.Operator == "||")
        {
            return ToBoolean(EvaluateConditionExpressionNode(binary.Left, root, item))
                || ToBoolean(EvaluateConditionExpressionNode(binary.Right, root, item));
        }

        var left = EvaluateConditionExpressionNode(binary.Left, root, item);
        var right = EvaluateConditionExpressionNode(binary.Right, root, item);

        return binary.Operator switch
        {
            "==" => ValuesEqual(left, right),
            "!=" => !ValuesEqual(left, right),
            ">" => ToNumber(left) > ToNumber(right),
            ">=" => ToNumber(left) >= ToNumber(right),
            "<" => ToNumber(left) < ToNumber(right),
            "<=" => ToNumber(left) <= ToNumber(right),
            "in" => right is List<object?> list && list.Any(entry => ValuesEqual(left, entry)),
            _ => throw new InvalidOperationException($"Unsupported binary operator '{binary.Operator}'.")
        };
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (TryGetNumericValue(left, out var leftNumber) && TryGetNumericValue(right, out var rightNumber))
        {
            return leftNumber == rightNumber;
        }

        return Equals(left, right);
    }

    private static bool TryGetNumericValue(object? value, out double number)
    {
        switch (value)
        {
            case byte b:
                number = b;
                return true;
            case sbyte sb:
                number = sb;
                return true;
            case short s:
                number = s;
                return true;
            case ushort us:
                number = us;
                return true;
            case int i:
                number = i;
                return true;
            case uint ui:
                number = ui;
                return true;
            case long l:
                number = l;
                return true;
            case ulong ul:
                number = ul;
                return true;
            case float f:
                number = f;
                return true;
            case double d:
                number = d;
                return true;
            case decimal dec:
                number = (double)dec;
                return true;
            case string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                number = parsed;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private static bool ToBoolean(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => s.Length > 0,
            _ => true
        };
    }


    private abstract record ConditionExpressionNode;

    private sealed record ConditionLiteralNode(object? Value) : ConditionExpressionNode;

    private sealed record ConditionPathNode(string Path) : ConditionExpressionNode;

    private sealed record ConditionExistsNode(ConditionExpressionNode Argument) : ConditionExpressionNode;

    private sealed record ConditionUnaryNode(string Operator, ConditionExpressionNode Operand) : ConditionExpressionNode;

    private sealed record ConditionBinaryNode(string Operator, ConditionExpressionNode Left, ConditionExpressionNode Right)
        : ConditionExpressionNode;

    private sealed record ConditionArrayNode(List<ConditionExpressionNode> Items) : ConditionExpressionNode;

    private enum ConditionTokenType
    {
        Identifier,
        Number,
        String,
        Operator,
        LeftParen,
        RightParen,
        LeftBracket,
        RightBracket,
        Comma,
        End
    }

    private sealed record ConditionToken(ConditionTokenType Type, string Text, int Position);

    private sealed class ConditionExpressionParser
    {
        private readonly string _expression;
        private readonly List<ConditionToken> _tokens;
        private int _index;

        internal ConditionExpressionParser(string expression)
        {
            _expression = expression;
            _tokens = Tokenize(expression);
        }

        internal ConditionExpressionNode Parse()
        {
            var node = ParseOr();
            if (Current.Type != ConditionTokenType.End)
            {
                throw CreateParseException(
                    $"Unexpected trailing token {FormatToken(Current)} after complete expression.");
            }
            return node;
        }

        private ConditionExpressionNode ParseOr()
        {
            var left = ParseAnd();
            while (MatchOperator("||") || MatchIdentifier("or"))
            {
                var right = ParseAnd();
                left = new ConditionBinaryNode("||", left, right);
            }
            return left;
        }

        private ConditionExpressionNode ParseAnd()
        {
            var left = ParseComparison();
            while (MatchOperator("&&") || MatchIdentifier("and"))
            {
                var right = ParseComparison();
                left = new ConditionBinaryNode("&&", left, right);
            }
            return left;
        }

        private ConditionExpressionNode ParseComparison()
        {
            var left = ParseUnary();

            while (TryParseComparisonOperator(out var op))
            {
                if (Current.Type is ConditionTokenType.End or ConditionTokenType.RightParen or ConditionTokenType.RightBracket or ConditionTokenType.Comma)
                {
                    throw CreateParseException(
                        $"Expected right-hand operand after operator '{op}', found {FormatToken(Current)}.");
                }

                var right = ParseUnary();
                if (op == "in" && right is not ConditionArrayNode)
                {
                    throw CreateParseException("Right-hand side of 'in' must be an array literal.");
                }
                left = new ConditionBinaryNode(op, left, right);
            }

            if (IsUnexpectedComparisonToken(Current))
            {
                var message =
                    $"Expected comparison operator (==, !=, eq, not eq, gt, gte, lt, lte, in) after {DescribeNode(left)}, found {FormatToken(Current)}.";
                var suggestion = GetOperatorSuggestion(Current.Text);
                if (suggestion != null)
                {
                    message = $"{message} {suggestion}";
                }

                throw CreateParseException(message);
            }

            return left;
        }

        private bool TryParseComparisonOperator(out string op)
        {
            if (MatchOperator("==")) { op = "=="; return true; }
            if (MatchOperator("!=")) { op = "!="; return true; }
            if (MatchOperator(">=")) { op = ">="; return true; }
            if (MatchOperator("<=")) { op = "<="; return true; }
            if (MatchOperator(">")) { op = ">"; return true; }
            if (MatchOperator("<")) { op = "<"; return true; }
            if (MatchIdentifier("eq")) { op = "=="; return true; }
            if (TryMatchNotEq()) { op = "!="; return true; }
            if (MatchIdentifier("gt")) { op = ">"; return true; }
            if (MatchIdentifier("gte")) { op = ">="; return true; }
            if (MatchIdentifier("lt")) { op = "<"; return true; }
            if (MatchIdentifier("lte")) { op = "<="; return true; }
            if (MatchIdentifier("in")) { op = "in"; return true; }
            op = string.Empty;
            return false;
        }

        private bool TryMatchNotEq()
        {
            if (!PeekIdentifier("not"))
            {
                return false;
            }

            if (!Peek(1).Type.Equals(ConditionTokenType.Identifier) ||
                !string.Equals(Peek(1).Text, "eq", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            _index += 2;
            return true;
        }

        private ConditionExpressionNode ParseUnary()
        {
            if (MatchOperator("!") || MatchIdentifier("not"))
            {
                return new ConditionUnaryNode("!", ParseUnary());
            }

            return ParsePrimary();
        }

        private ConditionExpressionNode ParsePrimary()
        {
            if (Match(ConditionTokenType.LeftParen))
            {
                var inner = ParseOr();
                Expect(ConditionTokenType.RightParen);
                return inner;
            }

            if (Match(ConditionTokenType.LeftBracket))
            {
                var items = new List<ConditionExpressionNode>();
                if (!Match(ConditionTokenType.RightBracket))
                {
                    do
                    {
                        items.Add(ParseOr());
                    }
                    while (Match(ConditionTokenType.Comma));
                    Expect(ConditionTokenType.RightBracket);
                }
                return new ConditionArrayNode(items);
            }

            if (Current.Type == ConditionTokenType.Number)
            {
                var raw = Current.Text;
                _index++;
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    throw CreateParseException($"Invalid number literal '{raw}'.");
                }
                return new ConditionLiteralNode(number);
            }

            if (Current.Type == ConditionTokenType.String)
            {
                var text = Current.Text;
                _index++;
                return new ConditionLiteralNode(text);
            }

            if (Current.Type == ConditionTokenType.Identifier)
            {
                var identifier = Current.Text;
                _index++;

                if (string.Equals(identifier, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return new ConditionLiteralNode(true);
                }

                if (string.Equals(identifier, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return new ConditionLiteralNode(false);
                }

                if (string.Equals(identifier, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return new ConditionLiteralNode(null);
                }

                if (string.Equals(identifier, "path", StringComparison.OrdinalIgnoreCase))
                {
                    return ParsePathCall();
                }

                if (string.Equals(identifier, "exists", StringComparison.OrdinalIgnoreCase))
                {
                    return ParseExistsCall();
                }

                throw CreateParseException(
                    $"Unexpected identifier '{identifier}'. Use path(...) for value references.");
            }

            throw CreateParseException($"Unexpected token {FormatToken(Current)}.");
        }

        private ConditionExpressionNode ParsePathCall()
        {
            Expect(ConditionTokenType.LeftParen);
            string path;
            if (Current.Type == ConditionTokenType.String || Current.Type == ConditionTokenType.Identifier)
            {
                path = Current.Text;
                _index++;
            }
            else
            {
                throw CreateParseException("path(...) requires a path reference.");
            }

            Expect(ConditionTokenType.RightParen);
            return new ConditionPathNode(path);
        }

        private ConditionExpressionNode ParseExistsCall()
        {
            Expect(ConditionTokenType.LeftParen);
            var argument = ParseOr();
            Expect(ConditionTokenType.RightParen);
            return new ConditionExistsNode(argument);
        }

        private ConditionToken Current => Peek(0);

        private ConditionToken Peek(int offset)
        {
            var cursor = _index + offset;
            if (cursor >= _tokens.Count)
            {
                return _tokens[^1];
            }
            return _tokens[cursor];
        }

        private bool Match(ConditionTokenType type)
        {
            if (Current.Type != type)
            {
                return false;
            }
            _index++;
            return true;
        }

        private void Expect(ConditionTokenType type)
        {
            if (!Match(type))
            {
                throw CreateParseException($"Expected {DescribeTokenType(type)} but found {FormatToken(Current)}.");
            }
        }

        private bool MatchOperator(string op)
        {
            if (Current.Type != ConditionTokenType.Operator ||
                !string.Equals(Current.Text, op, StringComparison.Ordinal))
            {
                return false;
            }
            _index++;
            return true;
        }

        private bool MatchIdentifier(string identifier)
        {
            if (!PeekIdentifier(identifier))
            {
                return false;
            }
            _index++;
            return true;
        }

        private bool PeekIdentifier(string identifier)
        {
            return Current.Type == ConditionTokenType.Identifier
                && string.Equals(Current.Text, identifier, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsUnexpectedComparisonToken(ConditionToken token)
        {
            if (token.Type is ConditionTokenType.End or ConditionTokenType.RightParen or ConditionTokenType.RightBracket or ConditionTokenType.Comma)
            {
                return false;
            }

            if (token.Type == ConditionTokenType.Operator &&
                (string.Equals(token.Text, "&&", StringComparison.Ordinal) ||
                string.Equals(token.Text, "||", StringComparison.Ordinal)))
            {
                return false;
            }

            if (token.Type == ConditionTokenType.Identifier &&
                (string.Equals(token.Text, "and", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token.Text, "or", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        private string? GetOperatorSuggestion(string tokenText)
        {
            var normalized = tokenText.Trim().ToLowerInvariant();
            if (normalized.Length == 0)
            {
                return null;
            }

            return normalized switch
            {
                "is" or "equals" => "Did you mean 'eq' or '=='?",
                "neq" => "Did you mean 'not eq' or '!='?",
                _ => GetClosestOperatorSuggestion(normalized)
            };
        }

        private string? GetClosestOperatorSuggestion(string tokenText)
        {
            var candidates = new[] { "eq", "gt", "gte", "lt", "lte", "in" };
            string? best = null;
            var bestDistance = int.MaxValue;

            foreach (var candidate in candidates)
            {
                var distance = ComputeDistance(tokenText, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            if (best == null || bestDistance > 2)
            {
                return null;
            }

            if (best == "eq")
            {
                return "Did you mean 'eq' or '=='?";
            }

            return $"Did you mean '{best}'?";
        }

        private static int ComputeDistance(string source, string target)
        {
            var costs = new int[target.Length + 1];
            for (var index = 0; index < costs.Length; index++)
            {
                costs[index] = index;
            }

            for (var sourceIndex = 1; sourceIndex <= source.Length; sourceIndex++)
            {
                costs[0] = sourceIndex;
                var previousDiagonal = sourceIndex - 1;
                for (var targetIndex = 1; targetIndex <= target.Length; targetIndex++)
                {
                    var previousTop = costs[targetIndex];
                    var substitutionCost = source[sourceIndex - 1] == target[targetIndex - 1] ? 0 : 1;
                    costs[targetIndex] = Math.Min(
                        Math.Min(costs[targetIndex] + 1, costs[targetIndex - 1] + 1),
                        previousDiagonal + substitutionCost);
                    previousDiagonal = previousTop;
                }
            }

            return costs[target.Length];
        }

        private string DescribeNode(ConditionExpressionNode node)
        {
            return node switch
            {
                ConditionPathNode path => $"path({path.Path})",
                ConditionLiteralNode literal => literal.Value switch
                {
                    null => "null",
                    string text => $"'{text}'",
                    bool b => b ? "true" : "false",
                    _ => Convert.ToString(literal.Value, CultureInfo.InvariantCulture) ?? "value"
                },
                _ => "expression"
            };
        }

        private static string DescribeTokenType(ConditionTokenType type)
        {
            return type switch
            {
                ConditionTokenType.LeftParen => "'('",
                ConditionTokenType.RightParen => "')'",
                ConditionTokenType.LeftBracket => "'['",
                ConditionTokenType.RightBracket => "']'",
                ConditionTokenType.Comma => "','",
                ConditionTokenType.End => "end of expression",
                _ => type.ToString()
            };
        }

        private static string FormatToken(ConditionToken token)
        {
            return token.Type == ConditionTokenType.End ? "end of expression" : $"'{token.Text}'";
        }

        private FormatException CreateParseException(string message)
        {
            return CreateParseException(message, Current.Position, Current.Text.Length);
        }

        private FormatException CreateParseException(string message, int position, int pointerWidth)
        {
            return new FormatException(FormatParseMessage(_expression, message, position, pointerWidth));
        }

        private static List<ConditionToken> Tokenize(string input)
        {
            var tokens = new List<ConditionToken>();
            var index = 0;

            while (index < input.Length)
            {
                var current = input[index];
                if (char.IsWhiteSpace(current))
                {
                    index++;
                    continue;
                }

                if (current == '\'')
                {
                    var (text, nextIndex) = ReadString(input, index);
                    tokens.Add(new ConditionToken(ConditionTokenType.String, text, index));
                    index = nextIndex;
                    continue;
                }

                if (char.IsDigit(current) || (current == '.' && index + 1 < input.Length && char.IsDigit(input[index + 1])))
                {
                    var start = index;
                    while (index < input.Length && (char.IsDigit(input[index]) || input[index] == '.'))
                    {
                        index++;
                    }
                    tokens.Add(new ConditionToken(ConditionTokenType.Number, input[start..index], start));
                    continue;
                }

                if (index + 1 < input.Length)
                {
                    var twoChar = input.Substring(index, 2);
                    if (twoChar is "&&" or "||" or "==" or "!=" or ">=" or "<=")
                    {
                        tokens.Add(new ConditionToken(ConditionTokenType.Operator, twoChar, index));
                        index += 2;
                        continue;
                    }
                }

                if (current is '!' or '>' or '<')
                {
                    tokens.Add(new ConditionToken(ConditionTokenType.Operator, current.ToString(), index));
                    index++;
                    continue;
                }

                if (current == '(')
                {
                    tokens.Add(new ConditionToken(ConditionTokenType.LeftParen, "(", index));
                    index++;
                    continue;
                }

                if (current == ')')
                {
                    tokens.Add(new ConditionToken(ConditionTokenType.RightParen, ")", index));
                    index++;
                    continue;
                }

                if (current == '[')
                {
                    tokens.Add(new ConditionToken(ConditionTokenType.LeftBracket, "[", index));
                    index++;
                    continue;
                }

                if (current == ']')
                {
                    tokens.Add(new ConditionToken(ConditionTokenType.RightBracket, "]", index));
                    index++;
                    continue;
                }

                if (current == ',')
                {
                    tokens.Add(new ConditionToken(ConditionTokenType.Comma, ",", index));
                    index++;
                    continue;
                }

                if (IsIdentifierStart(current))
                {
                    var start = index;
                    index++;
                    while (index < input.Length && IsIdentifierPart(input[index]))
                    {
                        index++;
                    }
                    tokens.Add(new ConditionToken(ConditionTokenType.Identifier, input[start..index], start));
                    continue;
                }

                throw CreateTokenizeException(input, $"Unexpected character '{current}'.", index, 1);
            }

            tokens.Add(new ConditionToken(ConditionTokenType.End, string.Empty, input.Length));
            return tokens;
        }

        private static bool IsIdentifierStart(char value)
        {
            return char.IsLetter(value) || value is '_' or '$';
        }

        private static bool IsIdentifierPart(char value)
        {
            return char.IsLetterOrDigit(value) || value is '_' or '$' or '.' or '[' or ']';
        }

        private static (string Value, int NextIndex) ReadString(string input, int startIndex)
        {
            var builder = new StringBuilder();
            var cursor = startIndex + 1;
            while (cursor < input.Length)
            {
                var current = input[cursor];
                if (current == '\'')
                {
                    return (builder.ToString(), cursor + 1);
                }

                if (current == '\\' && cursor + 1 < input.Length)
                {
                    cursor++;
                    builder.Append(input[cursor]);
                    cursor++;
                    continue;
                }

                builder.Append(current);
                cursor++;
            }

            throw CreateTokenizeException(input, "Unterminated string literal.", startIndex, 1);
        }

        private static FormatException CreateTokenizeException(string expression, string message, int position, int pointerWidth)
        {
            return new FormatException(FormatParseMessage(expression, message, position, pointerWidth));
        }

        private static string FormatParseMessage(string expression, string message, int position, int pointerWidth)
        {
            var safePosition = Math.Clamp(position, 0, expression.Length);
            var safeWidth = Math.Max(2, pointerWidth <= 0 ? 1 : pointerWidth);
            var pointer = new string(' ', safePosition) + new string('^', safeWidth);
            return $"Invalid expression at position {safePosition}: {message}\n{expression}\n{pointer}";
        }
    }
}
