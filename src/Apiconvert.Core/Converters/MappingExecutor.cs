using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static partial class MappingExecutor
{
    private const int MaxConditionBranchDepth = 64;
    private const int MaxRuleNestingDepth = 64;

    internal static ConversionResult ApplyConversion(object? input, object? rawRules, ConversionOptions? options = null)
    {
        var rules = RulesNormalizer.NormalizeConversionRules(rawRules);
        return ApplyConversion(input, rules, options);
    }

    internal static ConversionResult ApplyConversion(object? input, ConversionRules rules, ConversionOptions? options = null)
    {
        var errors = new List<string>();
        errors.AddRange(rules.ValidationErrors);
        var collisionPolicy = options?.CollisionPolicy ?? OutputCollisionPolicy.LastWriteWins;

        if (!rules.Rules.Any())
        {
            return new ConversionResult
            {
                Output = input ?? new Dictionary<string, object?>(),
                Errors = errors,
                Warnings = new List<string>()
            };
        }

        var output = new Dictionary<string, object?>();
        var warnings = new List<string>();
        ExecuteRules(
            input,
            null,
            rules.Rules,
            output,
            errors,
            warnings,
            new Dictionary<string, string>(StringComparer.Ordinal),
            collisionPolicy,
            "rules",
            0);

        return new ConversionResult { Output = output, Errors = errors, Warnings = warnings };
    }

    private static void ExecuteRules(
        object? root,
        object? item,
        IReadOnlyList<RuleNode> rules,
        Dictionary<string, object?> output,
        List<string> errors,
        List<string> warnings,
        Dictionary<string, string> writeOwners,
        OutputCollisionPolicy collisionPolicy,
        string path,
        int depth)
    {
        if (depth > MaxRuleNestingDepth)
        {
            errors.Add($"{path}: rule recursion limit exceeded.");
            return;
        }

        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];
            ExecuteRule(
                root,
                item,
                rule,
                output,
                errors,
                warnings,
                writeOwners,
                collisionPolicy,
                $"{path}[{index}]",
                depth);
        }
    }

    private static void ExecuteRule(
        object? root,
        object? item,
        RuleNode rule,
        Dictionary<string, object?> output,
        List<string> errors,
        List<string> warnings,
        Dictionary<string, string> writeOwners,
        OutputCollisionPolicy collisionPolicy,
        string path,
        int depth)
    {
        switch (rule.Kind)
        {
            case "field":
                ExecuteFieldRule(root, item, rule, output, errors, writeOwners, collisionPolicy, path);
                return;
            case "array":
                ExecuteArrayRule(root, item, rule, output, errors, warnings, writeOwners, collisionPolicy, path, depth);
                return;
            case "branch":
                ExecuteBranchRule(root, item, rule, output, errors, warnings, writeOwners, collisionPolicy, path, depth);
                return;
            default:
                errors.Add($"{path}: unsupported kind '{rule.Kind}'.");
                return;
        }
    }
}
