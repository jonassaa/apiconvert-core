using System.Text.Json;
using System.Text.Json.Nodes;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class RulesFormatter
{
    internal static string FormatCanonical(object? rawRules, bool pretty = true)
    {
        var normalized = RulesNormalizer.NormalizeConversionRules(rawRules);
        var canonical = CanonicalizeRules(normalized);
        return canonical.ToJsonString(new JsonSerializerOptions(JsonDefaults.Options)
        {
            WriteIndented = pretty
        });
    }

    private static JsonObject CanonicalizeRules(ConversionRules rules)
    {
        var result = new JsonObject();

        if (rules.InputFormat != DataFormat.Json)
        {
            result["inputFormat"] = JsonValue.Create(rules.InputFormat.ToString().ToLowerInvariant());
        }

        if (rules.OutputFormat != DataFormat.Json)
        {
            result["outputFormat"] = JsonValue.Create(rules.OutputFormat.ToString().ToLowerInvariant());
        }

        var ruleNodes = new JsonArray();
        foreach (var rule in rules.Rules)
        {
            ruleNodes.Add(CanonicalizeRule(rule));
        }

        result["rules"] = ruleNodes;
        return result;
    }

    private static JsonObject CanonicalizeRule(RuleNode rule)
    {
        if (rule.Kind == "field")
        {
            var result = new JsonObject
            {
                ["kind"] = "field",
                ["outputPaths"] = ToJsonArray(rule.OutputPaths),
                ["source"] = CanonicalizeSource(rule.Source ?? new ValueSource())
            };

            if (!string.IsNullOrEmpty(rule.DefaultValue))
            {
                result["defaultValue"] = rule.DefaultValue;
            }

            return result;
        }

        if (rule.Kind == "array")
        {
            var result = new JsonObject
            {
                ["kind"] = "array",
                ["inputPath"] = rule.InputPath,
                ["outputPaths"] = ToJsonArray(rule.OutputPaths)
            };

            if (rule.CoerceSingle)
            {
                result["coerceSingle"] = true;
            }

            var itemRules = new JsonArray();
            foreach (var itemRule in rule.ItemRules)
            {
                itemRules.Add(CanonicalizeRule(itemRule));
            }

            result["itemRules"] = itemRules;
            return result;
        }

        var branch = new JsonObject
        {
            ["kind"] = "branch",
            ["expression"] = rule.Expression ?? string.Empty
        };

        var thenRules = new JsonArray();
        foreach (var thenRule in rule.Then)
        {
            thenRules.Add(CanonicalizeRule(thenRule));
        }

        branch["then"] = thenRules;

        if (rule.ElseIf.Count > 0)
        {
            var elseIf = new JsonArray();
            foreach (var elseIfRule in rule.ElseIf)
            {
                var elseIfNode = new JsonObject
                {
                    ["expression"] = elseIfRule.Expression ?? string.Empty
                };
                var elseIfThen = new JsonArray();
                foreach (var thenRule in elseIfRule.Then)
                {
                    elseIfThen.Add(CanonicalizeRule(thenRule));
                }

                elseIfNode["then"] = elseIfThen;
                elseIf.Add(elseIfNode);
            }

            branch["elseIf"] = elseIf;
        }

        if (rule.Else.Count > 0)
        {
            var elseRules = new JsonArray();
            foreach (var elseRule in rule.Else)
            {
                elseRules.Add(CanonicalizeRule(elseRule));
            }

            branch["else"] = elseRules;
        }

        return branch;
    }

    private static JsonObject CanonicalizeSource(ValueSource source)
    {
        var result = new JsonObject
        {
            ["type"] = source.Type
        };

        if (!string.IsNullOrEmpty(source.Path))
        {
            result["path"] = source.Path;
        }

        if (source.Paths.Count > 0)
        {
            result["paths"] = ToJsonArray(source.Paths);
        }

        if (source.Value is not null)
        {
            result["value"] = source.Value;
        }

        if (!string.IsNullOrEmpty(source.Expression))
        {
            result["expression"] = source.Expression;
        }

        if (source.TrueValue is not null)
        {
            result["trueValue"] = source.TrueValue;
        }

        if (source.FalseValue is not null)
        {
            result["falseValue"] = source.FalseValue;
        }

        if (source.TrueSource is not null)
        {
            result["trueSource"] = CanonicalizeSource(source.TrueSource);
        }

        if (source.FalseSource is not null)
        {
            result["falseSource"] = CanonicalizeSource(source.FalseSource);
        }

        if (source.ElseIf.Count > 0)
        {
            var elseIf = new JsonArray();
            foreach (var branch in source.ElseIf)
            {
                var branchNode = new JsonObject
                {
                    ["expression"] = branch.Expression ?? string.Empty
                };

                if (branch.Source is not null)
                {
                    branchNode["source"] = CanonicalizeSource(branch.Source);
                }

                if (branch.Value is not null)
                {
                    branchNode["value"] = branch.Value;
                }

                elseIf.Add(branchNode);
            }

            result["elseIf"] = elseIf;
        }

        if (source.ConditionOutput.HasValue)
        {
            result["conditionOutput"] = source.ConditionOutput.Value.ToString().ToLowerInvariant();
        }

        if (source.MergeMode.HasValue)
        {
            result["mergeMode"] = source.MergeMode.Value.ToString()[0].ToString().ToLowerInvariant() + source.MergeMode.Value.ToString()[1..];
        }

        if (source.Separator is not null)
        {
            result["separator"] = source.Separator;
        }

        if (source.TokenIndex.HasValue)
        {
            result["tokenIndex"] = source.TokenIndex.Value;
        }

        if (source.TrimAfterSplit.HasValue)
        {
            result["trimAfterSplit"] = source.TrimAfterSplit.Value;
        }

        if (source.Transform.HasValue)
        {
            result["transform"] = source.Transform.Value switch
            {
                TransformType.ToLowerCase => "toLowerCase",
                TransformType.ToUpperCase => "toUpperCase",
                TransformType.Number => "number",
                TransformType.Boolean => "boolean",
                TransformType.Concat => "concat",
                TransformType.Split => "split",
                _ => source.Transform.Value.ToString().ToLowerInvariant()
            };
        }

        if (!string.IsNullOrWhiteSpace(source.CustomTransform))
        {
            result["customTransform"] = source.CustomTransform;
        }

        if (source.Type == "merge" && !source.MergeMode.HasValue)
        {
            result["mergeMode"] = "concat";
        }

        if (source.Type == "condition" && !source.ConditionOutput.HasValue)
        {
            result["conditionOutput"] = "branch";
        }

        if (source.Type == "transform" && !source.Transform.HasValue && string.IsNullOrWhiteSpace(source.CustomTransform))
        {
            result["transform"] = "toLowerCase";
        }

        return result;
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }
}
