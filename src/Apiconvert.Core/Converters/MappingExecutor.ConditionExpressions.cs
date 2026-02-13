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
            "==" => Equals(left, right),
            "!=" => !Equals(left, right),
            ">" => ToNumber(left) > ToNumber(right),
            ">=" => ToNumber(left) >= ToNumber(right),
            "<" => ToNumber(left) < ToNumber(right),
            "<=" => ToNumber(left) <= ToNumber(right),
            "in" => right is List<object?> list && list.Any(entry => Equals(left, entry)),
            _ => throw new InvalidOperationException($"Unsupported binary operator '{binary.Operator}'.")
        };
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
            Expect(ConditionTokenType.End);
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
                var right = ParseUnary();
                if (op == "in" && right is not ConditionArrayNode)
                {
                    throw CreateParseException("Right-hand side of 'in' must be an array literal.");
                }
                left = new ConditionBinaryNode(op, left, right);
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

            throw CreateParseException($"Unexpected token '{Current.Text}'.");
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
                throw CreateParseException($"Expected {type} but found '{Current.Text}'.");
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

        private FormatException CreateParseException(string message)
        {
            return new FormatException($"{message} (position {Current.Position}).");
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

                throw new FormatException($"Unexpected character '{current}' at position {index}.");
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

            throw new FormatException($"Unterminated string literal at position {startIndex}.");
        }
    }
}
