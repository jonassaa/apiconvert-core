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
        var diagnostics = new List<ConversionDiagnostic>();
        foreach (var validationError in rules.ValidationErrors)
        {
            AddError(diagnostics, "ACV-RUN-000", "rules", validationError);
        }

        var errors = new List<string>();
        var collisionPolicy = options?.CollisionPolicy ?? OutputCollisionPolicy.LastWriteWins;
        var transformRegistry = options?.TransformRegistry ?? new Dictionary<string, Func<object?, object?>>(StringComparer.Ordinal);
        var trace = options?.Explain == true ? new List<ConversionTraceEntry>() : null;

        if (!rules.Rules.Any())
        {
            return new ConversionResult
            {
                Output = input ?? new Dictionary<string, object?>(),
                Errors = diagnostics
                    .Where(diagnostic => diagnostic.Severity == ConversionDiagnosticSeverity.Error)
                    .Select(diagnostic => diagnostic.Message)
                    .ToList(),
                Warnings = diagnostics
                    .Where(diagnostic => diagnostic.Severity == ConversionDiagnosticSeverity.Warning)
                    .Select(diagnostic => diagnostic.Message)
                    .ToList(),
                Trace = trace ?? new List<ConversionTraceEntry>(),
                Diagnostics = diagnostics
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
            diagnostics,
            new Dictionary<string, string>(StringComparer.Ordinal),
            collisionPolicy,
            transformRegistry,
            trace,
            "rules",
            0);

        return new ConversionResult
        {
            Output = output,
            Errors = diagnostics
                .Where(diagnostic => diagnostic.Severity == ConversionDiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.Message)
                .ToList(),
            Warnings = diagnostics
                .Where(diagnostic => diagnostic.Severity == ConversionDiagnosticSeverity.Warning)
                .Select(diagnostic => diagnostic.Message)
                .ToList(),
            Trace = trace ?? new List<ConversionTraceEntry>(),
            Diagnostics = diagnostics
        };
    }

    private static void ExecuteRules(
        object? root,
        object? item,
        IReadOnlyList<RuleNode> rules,
        Dictionary<string, object?> output,
        List<string> errors,
        List<string> warnings,
        List<ConversionDiagnostic> diagnostics,
        Dictionary<string, string> writeOwners,
        OutputCollisionPolicy collisionPolicy,
        IReadOnlyDictionary<string, Func<object?, object?>> transformRegistry,
        List<ConversionTraceEntry>? trace,
        string path,
        int depth)
    {
        if (depth > MaxRuleNestingDepth)
        {
            AddError(diagnostics, "ACV-RUN-900", path, $"{path}: rule recursion limit exceeded.");
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
                diagnostics,
                writeOwners,
                collisionPolicy,
                transformRegistry,
                trace,
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
        List<ConversionDiagnostic> diagnostics,
        Dictionary<string, string> writeOwners,
        OutputCollisionPolicy collisionPolicy,
        IReadOnlyDictionary<string, Func<object?, object?>> transformRegistry,
        List<ConversionTraceEntry>? trace,
        string path,
        int depth)
    {
        switch (rule.Kind)
        {
            case "field":
                ExecuteFieldRule(root, item, rule, output, errors, diagnostics, writeOwners, collisionPolicy, transformRegistry, trace, path);
                return;
            case "array":
                ExecuteArrayRule(root, item, rule, output, errors, warnings, diagnostics, writeOwners, collisionPolicy, transformRegistry, trace, path, depth);
                return;
            case "branch":
                ExecuteBranchRule(root, item, rule, output, errors, warnings, diagnostics, writeOwners, collisionPolicy, transformRegistry, trace, path, depth);
                return;
            default:
                var error = $"{path}: unsupported kind '{rule.Kind}'.";
                AddError(diagnostics, "ACV-RUN-901", path, error);
                AddTrace(trace, path, rule.Kind, "unsupported", error: error);
                return;
        }
    }

    private static void AddError(List<ConversionDiagnostic> diagnostics, string code, string rulePath, string message)
    {
        diagnostics.Add(new ConversionDiagnostic
        {
            Code = code,
            RulePath = rulePath,
            Message = message,
            Severity = ConversionDiagnosticSeverity.Error
        });
    }

    private static void AddWarning(List<ConversionDiagnostic> diagnostics, string code, string rulePath, string message)
    {
        diagnostics.Add(new ConversionDiagnostic
        {
            Code = code,
            RulePath = rulePath,
            Message = message,
            Severity = ConversionDiagnosticSeverity.Warning
        });
    }
}
