using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static partial class MappingExecutor
{
    private static void ExecuteFieldRule(
        object? root,
        object? item,
        RuleNode rule,
        Dictionary<string, object?> output,
        List<string> errors,
        Dictionary<string, string> writeOwners,
        OutputCollisionPolicy collisionPolicy,
        string path)
    {
        var writePaths = GetWritePaths(rule);
        if (writePaths.Count == 0)
        {
            errors.Add($"{path}: outputPaths is required.");
            return;
        }

        var source = rule.Source ?? new ValueSource();
        var value = ResolveSourceValue(root, item, source, errors, $"{path}.source");
        if ((value == null || (value is string str && string.IsNullOrEmpty(str))) && !string.IsNullOrEmpty(rule.DefaultValue))
        {
            value = ParsePrimitive(rule.DefaultValue);
        }

        foreach (var writePath in writePaths)
        {
            WriteValue(
                output,
                writeOwners,
                errors,
                collisionPolicy,
                path,
                writePath,
                value);
        }
    }

    private static void ExecuteArrayRule(
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
        var value = ResolvePathValue(root, item, rule.InputPath);
        var items = value as List<object?>;
        if (items == null && rule.CoerceSingle && value != null)
        {
            items = new List<object?> { value };
        }

        if (items == null)
        {
            if (value == null)
            {
                warnings.Add($"Array mapping skipped: inputPath \"{rule.InputPath}\" not found ({path}).");
            }
            else
            {
                errors.Add($"{path}: input path did not resolve to an array ({rule.InputPath}).");
            }
            return;
        }

        var arrayWritePaths = GetWritePaths(rule);
        if (arrayWritePaths.Count == 0)
        {
            errors.Add($"{path}: outputPaths is required.");
            return;
        }

        var mappedItems = new List<object?>();
        foreach (var listItem in items)
        {
            var itemOutput = new Dictionary<string, object?>();
            ExecuteRules(
                root,
                listItem,
                rule.ItemRules,
                itemOutput,
                errors,
                warnings,
                new Dictionary<string, string>(StringComparer.Ordinal),
                collisionPolicy,
                $"{path}.itemRules",
                depth + 1);
            mappedItems.Add(itemOutput);
        }

        foreach (var outputPath in arrayWritePaths)
        {
            WriteValue(
                output,
                writeOwners,
                errors,
                collisionPolicy,
                path,
                outputPath,
                mappedItems);
        }
    }

    private static void ExecuteBranchRule(
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
        var matched = EvaluateCondition(root, item, rule.Expression, errors, path, "branch expression");
        if (matched)
        {
            ExecuteRules(
                root,
                item,
                rule.Then,
                output,
                errors,
                warnings,
                writeOwners,
                collisionPolicy,
                $"{path}.then",
                depth + 1);
            return;
        }

        for (var index = 0; index < rule.ElseIf.Count; index++)
        {
            var elseIf = rule.ElseIf[index];
            var branchPath = $"{path}.elseIf[{index}]";
            var elseIfMatched = EvaluateCondition(root, item, elseIf.Expression, errors, branchPath, "branch expression");
            if (!elseIfMatched)
            {
                continue;
            }

            ExecuteRules(
                root,
                item,
                elseIf.Then,
                output,
                errors,
                warnings,
                writeOwners,
                collisionPolicy,
                $"{branchPath}.then",
                depth + 1);
            return;
        }

        if (rule.Else.Count > 0)
        {
            ExecuteRules(
                root,
                item,
                rule.Else,
                output,
                errors,
                warnings,
                writeOwners,
                collisionPolicy,
                $"{path}.else",
                depth + 1);
        }
    }

    private static void WriteValue(
        Dictionary<string, object?> output,
        Dictionary<string, string> writeOwners,
        List<string> errors,
        OutputCollisionPolicy collisionPolicy,
        string rulePath,
        string outputPath,
        object? value)
    {
        if (!writeOwners.TryGetValue(outputPath, out var firstWriterPath))
        {
            writeOwners[outputPath] = rulePath;
            SetValueByPath(output, outputPath, value);
            return;
        }

        switch (collisionPolicy)
        {
            case OutputCollisionPolicy.LastWriteWins:
                writeOwners[outputPath] = rulePath;
                SetValueByPath(output, outputPath, value);
                return;
            case OutputCollisionPolicy.FirstWriteWins:
                return;
            case OutputCollisionPolicy.Error:
                errors.Add($"{rulePath}: output collision at \"{outputPath}\" (already written by {firstWriterPath}).");
                return;
            default:
                writeOwners[outputPath] = rulePath;
                SetValueByPath(output, outputPath, value);
                return;
        }
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
            return "$";
        }

        return path;
    }

    private static List<string> GetWritePaths(RuleNode rule)
    {
        return rule.OutputPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizeWritePath)
            .Where(path => !string.IsNullOrWhiteSpace(path) && path != "$")
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
