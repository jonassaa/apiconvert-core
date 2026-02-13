using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class MappingExecutor
{
    internal static ConversionResult ApplyConversion(object? input, object? rawRules)
    {
        var rules = RulesNormalizer.NormalizeConversionRules(rawRules);
        if (!rules.FieldMappings.Any() && !rules.ArrayMappings.Any())
        {
            return new ConversionResult
            {
                Output = input ?? new Dictionary<string, object?>(),
                Errors = new List<string>(),
                Warnings = new List<string>()
            };
        }

        var output = new Dictionary<string, object?>();
        var errors = new List<string>();
        var warnings = new List<string>();

        ApplyFieldMappings(input, null, rules.FieldMappings, output, errors, "Field");

        for (var index = 0; index < rules.ArrayMappings.Count; index++)
        {
            var arrayRule = rules.ArrayMappings[index];
            var value = ResolvePathValue(input, null, arrayRule.InputPath);
            var items = value as List<object?>;
            if (items == null && arrayRule.CoerceSingle && value != null)
            {
                items = new List<object?> { value };
            }

            if (items == null)
            {
                if (value == null)
                {
                    warnings.Add(
                        $"Array mapping skipped: inputPath \"{arrayRule.InputPath}\" not found (arrayMappings[{index}]).");
                }
                else
                {
                    errors.Add($"Array {index + 1}: input path did not resolve to an array ({arrayRule.InputPath}).");
                }
                continue;
            }

            var mappedItems = new List<object?>();
            foreach (var item in items)
            {
                var itemOutput = new Dictionary<string, object?>();
                ApplyFieldMappings(input, item, arrayRule.ItemMappings, itemOutput, errors, $"Array {index + 1} item");
                mappedItems.Add(itemOutput);
            }

            var arrayWritePaths = GetArrayWritePaths(arrayRule);
            if (arrayWritePaths.Count == 0)
            {
                errors.Add($"Array {index + 1}: output path is required.");
                continue;
            }

            foreach (var outputPath in arrayWritePaths)
            {
                SetValueByPath(output, outputPath, mappedItems);
            }
        }

        return new ConversionResult { Output = output, Errors = errors, Warnings = warnings };
    }

    private static void ApplyFieldMappings(
        object? root,
        object? item,
        IEnumerable<FieldRule> mappings,
        Dictionary<string, object?> output,
        List<string> errors,
        string label)
    {
        var index = 0;
        foreach (var rule in mappings)
        {
            index++;
            var writePaths = GetWritePaths(rule);
            if (writePaths.Count == 0)
            {
                errors.Add($"{label} {index}: output path is required.");
                continue;
            }

            var value = ResolveSourceValue(root, item, rule.Source, errors, $"{label} {index}");
            if ((value == null || (value is string str && string.IsNullOrEmpty(str))) && !string.IsNullOrEmpty(rule.DefaultValue))
            {
                value = ParsePrimitive(rule.DefaultValue);
            }

            foreach (var writePath in writePaths)
            {
                SetValueByPath(output, writePath, value);
            }
        }
    }

    private static object? ResolveSourceValue(
        object? root,
        object? item,
        ValueSource source,
        List<string> errors,
        string errorContext)
    {
        switch (source.Type)
        {
            case "constant":
                return ParsePrimitive(source.Value ?? string.Empty);
            case "path":
                return ResolvePathValue(root, item, source.Path ?? string.Empty);
            case "merge":
                return ResolveMergeSourceValue(root, item, source);
            case "transform":
            {
                if (source.Transform == TransformType.Concat)
                {
                    var tokens = (source.Path ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(token => token.Trim())
                        .Where(token => token.Length > 0);
                    var sb = new StringBuilder();
                    foreach (var token in tokens)
                    {
                        if (token.StartsWith("const:", StringComparison.OrdinalIgnoreCase))
                        {
                            sb.Append(token.Replace("const:", string.Empty, StringComparison.OrdinalIgnoreCase));
                            continue;
                        }
                        sb.Append(ResolvePathValue(root, item, token) ?? string.Empty);
                    }
                    return sb.ToString();
                }

                if (source.Transform == TransformType.Split)
                {
                    var splitBaseValue = ResolvePathValue(root, item, source.Path ?? string.Empty);
                    if (splitBaseValue == null)
                    {
                        return null;
                    }

                    var separator = source.Separator ?? " ";
                    if (separator.Length == 0)
                    {
                        return null;
                    }
                    var tokenIndex = source.TokenIndex ?? 0;
                    var trimAfterSplit = source.TrimAfterSplit ?? true;
                    var rawTokens = splitBaseValue
                        .ToString()
                        ?.Split(separator, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    var tokens = trimAfterSplit
                        ? rawTokens.Select(token => token.Trim()).Where(token => token.Length > 0).ToArray()
                        : rawTokens;

                    if (tokenIndex < 0)
                    {
                        tokenIndex = tokens.Length + tokenIndex;
                    }

                    if (tokenIndex < 0 || tokenIndex >= tokens.Length)
                    {
                        return null;
                    }

                    return tokens[tokenIndex];
                }

                var baseValue = ResolvePathValue(root, item, source.Path ?? string.Empty);
                return ResolveTransform(baseValue, source.Transform ?? TransformType.ToLowerCase);
            }
            case "condition":
            {
                if (string.IsNullOrWhiteSpace(source.Expression))
                {
                    errors.Add($"{errorContext}: condition expression is required.");
                    var missingResolved = source.FalseValue;
                    return ParsePrimitive(missingResolved ?? string.Empty);
                }

                var evaluation = TryEvaluateConditionExpression(root, item, source.Expression!, out var matched, out var error);
                if (!evaluation)
                {
                    errors.Add($"{errorContext}: invalid condition expression \"{source.Expression}\": {error}");
                    matched = false;
                }

                var resolved = matched ? source.TrueValue : source.FalseValue;
                return ParsePrimitive(resolved ?? string.Empty);
            }
            default:
                return null;
        }
    }

    private static object? ResolveMergeSourceValue(object? root, object? item, ValueSource source)
    {
        var values = source.Paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => ResolvePathValue(root, item, path))
            .ToList();

        var mode = source.MergeMode ?? MergeMode.Concat;

        return mode switch
        {
            MergeMode.FirstNonEmpty => values.FirstOrDefault(v => v != null && (v is not string s || s.Length > 0)),
            MergeMode.Array => values,
            _ => string.Join(source.Separator ?? string.Empty, values.Select(v => v?.ToString() ?? string.Empty))
        };
    }

    private static object? ResolvePathValue(object? root, object? item, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path == "$") return root;
        if (path.StartsWith("$.", StringComparison.Ordinal))
        {
            return GetValueByPath(root, path[2..]);
        }
        if (path.StartsWith("$[", StringComparison.Ordinal))
        {
            return GetValueByPath(root, path[1..]);
        }
        if (item != null)
        {
            return GetValueByPath(item, path);
        }
        return GetValueByPath(root, path);
    }

    private static object? ResolveTransform(object? value, TransformType transform)
    {
        return transform switch
        {
            TransformType.ToLowerCase => value is string lower ? lower.ToLowerInvariant() : value,
            TransformType.ToUpperCase => value is string upper ? upper.ToUpperInvariant() : value,
            TransformType.Number => value == null || (value is string s && s == string.Empty) ? value : ToNumber(value),
            TransformType.Boolean => value switch
            {
                bool b => b,
                string s => new[] { "true", "1", "yes", "y" }.Contains(s.ToLowerInvariant()),
                _ => value != null
            },
            _ => value
        };
    }

    private static object? GetValueByPath(object? input, string path)
    {
        if (input == null || string.IsNullOrWhiteSpace(path)) return null;
        var parts = path.Split('.').Select(part => part.Trim()).ToArray();
        object? current = input;
        foreach (var part in parts)
        {
            if (current == null) return null;
            var arrayMatch = Regex.Match(part, "^(\\w+)\\[(\\d+)\\]$");
            if (arrayMatch.Success)
            {
                var key = arrayMatch.Groups[1].Value;
                var index = int.Parse(arrayMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                if (current is not Dictionary<string, object?> dict || !dict.TryGetValue(key, out var next)) return null;
                if (next is not List<object?> list || index >= list.Count) return null;
                current = list[index];
                continue;
            }

            if (Regex.IsMatch(part, "^\\d+$"))
            {
                if (current is not List<object?> list || !int.TryParse(part, out var index) || index >= list.Count) return null;
                current = list[index];
                continue;
            }

            if (current is not Dictionary<string, object?> obj || !obj.TryGetValue(part, out var value)) return null;
            current = value;
        }
        return current;
    }

    private static void SetValueByPath(Dictionary<string, object?> target, string path, object? value)
    {
        var parts = path.Split('.').Select(part => part.Trim()).ToArray();
        var current = target;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            var isLast = index == parts.Length - 1;
            if (isLast)
            {
                current[part] = value;
                return;
            }

            if (!current.TryGetValue(part, out var next) || next is not Dictionary<string, object?> nested)
            {
                nested = new Dictionary<string, object?>();
                current[part] = nested;
            }
            current = nested;
        }
    }

    private static string NormalizeWritePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (path.StartsWith("$.", StringComparison.Ordinal))
        {
            return path[2..];
        }

        if (path == "$")
        {
            return string.Empty;
        }

        return path;
    }

    private static List<string> GetWritePaths(FieldRule rule)
    {
        var paths = new List<string>();

        if (!string.IsNullOrWhiteSpace(rule.OutputPath))
        {
            paths.Add(rule.OutputPath);
        }

        if (rule.OutputPaths.Count > 0)
        {
            paths.AddRange(rule.OutputPaths.Where(path => !string.IsNullOrWhiteSpace(path)));
        }

        return paths
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> GetArrayWritePaths(ArrayRule rule)
    {
        var paths = new List<string>();

        if (!string.IsNullOrWhiteSpace(rule.OutputPath))
        {
            var normalized = NormalizeWritePath(rule.OutputPath);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                paths.Add(normalized);
            }
        }

        if (rule.OutputPaths.Count > 0)
        {
            paths.AddRange(
                rule.OutputPaths
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(NormalizeWritePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path)));
        }

        return paths
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

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

    private static object? ParsePrimitive(string value)
    {
        return PrimitiveParser.ParsePrimitive(value);
    }

    private static double ToNumber(object? value)
    {
        if (value == null) return double.NaN;
        if (value is double d) return d;
        if (value is float f) return f;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is decimal m) return (double)m;
        if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return double.NaN;
        }
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
