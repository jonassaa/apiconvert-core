using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class RulesLinter
{
    internal static List<RuleLintDiagnostic> LintRules(object? rawRules)
    {
        var rules = RulesNormalizer.NormalizeConversionRules(rawRules);
        var diagnostics = new List<RuleLintDiagnostic>();

        foreach (var validationError in rules.ValidationErrors)
        {
            diagnostics.Add(new RuleLintDiagnostic
            {
                Code = "ACV-LINT-001",
                Severity = RuleLintSeverity.Error,
                RulePath = "rules",
                Message = validationError,
                Suggestion = "Fix schema/normalization errors before running conversion."
            });
        }

        var outputWriters = new Dictionary<string, string>(StringComparer.Ordinal);
        AnalyzeRuleNodes(rules.Rules, "rules", diagnostics, outputWriters);
        return diagnostics;
    }

    private static void AnalyzeRuleNodes(
        IReadOnlyList<RuleNode>? nodes,
        string path,
        List<RuleLintDiagnostic> diagnostics,
        Dictionary<string, string> outputWriters)
    {
        var ruleNodes = nodes ?? Array.Empty<RuleNode>();
        for (var index = 0; index < ruleNodes.Count; index++)
        {
            var node = ruleNodes[index];
            var nodePath = $"{path}[{index}]";
            switch (node.Kind)
            {
                case "field":
                    AnalyzeFieldRule(node, nodePath, diagnostics, outputWriters);
                    break;
                case "array":
                    AnalyzeArrayRule(node, nodePath, diagnostics, outputWriters);
                    break;
                case "branch":
                    AnalyzeBranchRule(node, nodePath, diagnostics, outputWriters);
                    break;
            }
        }
    }

    private static void AnalyzeFieldRule(
        RuleNode node,
        string nodePath,
        List<RuleLintDiagnostic> diagnostics,
        Dictionary<string, string> outputWriters)
    {
        AnalyzeOutputPaths(node.OutputPaths, nodePath, diagnostics, outputWriters);

        if (node.Source?.Type == "path" && string.IsNullOrWhiteSpace(node.DefaultValue))
        {
            diagnostics.Add(new RuleLintDiagnostic
            {
                Code = "ACV-LINT-002",
                Severity = RuleLintSeverity.Warning,
                RulePath = nodePath,
                Message = $"{nodePath}: source.type=path without defaultValue can produce null/empty writes when input is missing.",
                Suggestion = "Set defaultValue when missing input is expected, or keep as-is if null propagation is intentional."
            });
        }
    }

    private static void AnalyzeArrayRule(
        RuleNode node,
        string nodePath,
        List<RuleLintDiagnostic> diagnostics,
        Dictionary<string, string> outputWriters)
    {
        AnalyzeOutputPaths(node.OutputPaths, nodePath, diagnostics, outputWriters);
        AnalyzeRuleNodes(node.ItemRules, $"{nodePath}.itemRules", diagnostics, outputWriters);
    }

    private static void AnalyzeBranchRule(
        RuleNode node,
        string nodePath,
        List<RuleLintDiagnostic> diagnostics,
        Dictionary<string, string> outputWriters)
    {
        var literal = ParseBooleanLiteral(node.Expression);
        if (literal == true && (node.ElseIf.Count > 0 || node.Else.Count > 0))
        {
            diagnostics.Add(new RuleLintDiagnostic
            {
                Code = "ACV-LINT-003",
                Severity = RuleLintSeverity.Warning,
                RulePath = nodePath,
                Message = $"{nodePath}: expression is always true; elseIf/else branches are unreachable.",
                Suggestion = "Remove unreachable branches or replace expression with a non-literal condition."
            });
        }
        else if (literal == false && node.ElseIf.Count == 0 && node.Else.Count == 0 && node.Then.Count > 0)
        {
            diagnostics.Add(new RuleLintDiagnostic
            {
                Code = "ACV-LINT-004",
                Severity = RuleLintSeverity.Warning,
                RulePath = nodePath,
                Message = $"{nodePath}: expression is always false and no else/elseIf branch exists.",
                Suggestion = "Add else/elseIf handling or remove the branch."
            });
        }

        AnalyzeRuleNodes(node.Then, $"{nodePath}.then", diagnostics, outputWriters);
        for (var index = 0; index < node.ElseIf.Count; index++)
        {
            AnalyzeRuleNodes(node.ElseIf[index].Then, $"{nodePath}.elseIf[{index}].then", diagnostics, outputWriters);
        }
        AnalyzeRuleNodes(node.Else, $"{nodePath}.else", diagnostics, outputWriters);
    }

    private static void AnalyzeOutputPaths(
        IReadOnlyList<string> outputPaths,
        string nodePath,
        List<RuleLintDiagnostic> diagnostics,
        Dictionary<string, string> outputWriters)
    {
        foreach (var outputPath in outputPaths)
        {
            if (!outputWriters.TryAdd(outputPath, nodePath))
            {
                var firstWriter = outputWriters[outputPath];
                diagnostics.Add(new RuleLintDiagnostic
                {
                    Code = "ACV-LINT-005",
                    Severity = RuleLintSeverity.Warning,
                    RulePath = nodePath,
                    Message = $"{nodePath}: outputPath '{outputPath}' is also written by {firstWriter}.",
                    Suggestion = "Use unique output paths or configure collisionPolicy explicitly for intentional overlaps."
                });
            }
        }
    }

    private static bool? ParseBooleanLiteral(string? expression)
    {
        if (expression == null)
        {
            return null;
        }

        if (expression.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (expression.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }
}
