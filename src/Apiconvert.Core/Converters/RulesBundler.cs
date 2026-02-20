using System.Text.Json;
using System.Text.Json.Nodes;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class RulesBundler
{
    internal static ConversionRules BundleRules(string entryRulesPath, RuleBundleOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(entryRulesPath))
        {
            throw new ArgumentException("Entry rules path is required.", nameof(entryRulesPath));
        }

        var baseDirectory = string.IsNullOrWhiteSpace(options?.BaseDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(options.BaseDirectory);
        var entryPath = Path.GetFullPath(Path.Combine(baseDirectory, entryRulesPath));
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return BundleFile(entryPath, visited, []);
    }

    private static ConversionRules BundleFile(
        string filePath,
        HashSet<string> visited,
        IReadOnlyList<string> chain)
    {
        var normalizedFilePath = Path.GetFullPath(filePath);
        if (chain.Contains(normalizedFilePath, StringComparer.OrdinalIgnoreCase))
        {
            var circular = string.Join(" -> ", chain.Append(normalizedFilePath).Select(Path.GetFileName));
            throw new InvalidOperationException($"Circular include detected: {circular}");
        }

        if (visited.Contains(normalizedFilePath))
        {
            return new ConversionRules();
        }

        visited.Add(normalizedFilePath);

        var raw = LoadRawRulesFile(normalizedFilePath);
        var includes = NormalizeIncludes(raw["include"]);
        var nextChain = chain.Append(normalizedFilePath).ToArray();

        var includedRules = new List<ConversionRules>();
        foreach (var include in includes)
        {
            var includeBase = Path.GetDirectoryName(normalizedFilePath) ?? string.Empty;
            var includePath = Path.GetFullPath(Path.Combine(includeBase, include));
            includedRules.Add(BundleFile(includePath, visited, nextChain));
        }

        var localObject = new JsonObject
        {
            ["inputFormat"] = raw["inputFormat"]?.DeepClone(),
            ["outputFormat"] = raw["outputFormat"]?.DeepClone(),
            ["rules"] = raw["rules"]?.DeepClone()
        };
        var localRules = RulesNormalizer.NormalizeConversionRules(localObject.ToJsonString());

        return new ConversionRules
        {
            InputFormat = localRules.InputFormat,
            OutputFormat = localRules.OutputFormat,
            Rules = includedRules.SelectMany(entry => entry.Rules).Concat(localRules.Rules).ToList(),
            ValidationErrors = includedRules
                .SelectMany(entry => entry.ValidationErrors)
                .Concat(localRules.ValidationErrors)
                .ToList()
        };
    }

    private static JsonObject LoadRawRulesFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Included rules file not found: {filePath}");
        }

        var text = File.ReadAllText(filePath);
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(text);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON in rules file {filePath}: {ex.Message}", ex);
        }

        if (node is not JsonObject jsonObject)
        {
            throw new InvalidOperationException($"Rules file must contain a JSON object: {filePath}");
        }

        return jsonObject;
    }

    private static List<string> NormalizeIncludes(JsonNode? includeNode)
    {
        if (includeNode is null)
        {
            return [];
        }

        if (includeNode is not JsonArray includeArray)
        {
            throw new InvalidOperationException("include must be an array of relative file paths.");
        }

        return includeArray
            .Select(node => node?.GetValue<string>()?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
    }
}
