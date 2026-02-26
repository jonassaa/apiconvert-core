using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class RulesBundlerCoverageTests
{
    [Fact]
    public void BundleRules_UsesBaseDirectoryOptionAndDeduplicatesIncludes()
    {
        var root = Directory.CreateTempSubdirectory("apiconvert-bundle-options-");
        var sharedPath = Path.Combine(root.FullName, "shared.rules.json");
        var entryPath = Path.Combine(root.FullName, "entry.rules.json");

        File.WriteAllText(sharedPath, """
        {
          "inputFormat": "json",
          "outputFormat": "json",
          "rules": [
            {
              "kind": "field",
              "outputPaths": ["x"],
              "source": { "type": "constant", "value": "1" }
            }
          ]
        }
        """);

        File.WriteAllText(entryPath, """
        {
          "inputFormat": "json",
          "outputFormat": "json",
          "include": ["./shared.rules.json", "./shared.rules.json"],
          "rules": [
            {
              "kind": "field",
              "outputPaths": ["y"],
              "source": { "type": "constant", "value": "2" }
            }
          ]
        }
        """);

        var bundled = ConversionEngine.BundleRules("entry.rules.json", new RuleBundleOptions
        {
            BaseDirectory = root.FullName
        });

        Assert.Equal(root.FullName, new RuleBundleOptions { BaseDirectory = root.FullName }.BaseDirectory);
        Assert.Equal(2, bundled.Rules.Count);
        Assert.Equal("x", bundled.Rules[0].OutputPaths[0]);
        Assert.Equal("y", bundled.Rules[1].OutputPaths[0]);
    }

    [Fact]
    public void BundleRules_ThrowsForMissingInvalidAndNonObjectFiles()
    {
        var root = Directory.CreateTempSubdirectory("apiconvert-bundle-errors-");
        var missingIncludeEntry = Path.Combine(root.FullName, "missing-entry.rules.json");
        var invalidJsonEntry = Path.Combine(root.FullName, "invalid-entry.rules.json");
        var arrayRootEntry = Path.Combine(root.FullName, "array-entry.rules.json");

        File.WriteAllText(missingIncludeEntry, """{ "include": ["./nope.rules.json"], "rules": [] }""");
        File.WriteAllText(invalidJsonEntry, """{ "include": ["./broken.rules.json"], "rules": [] }""");
        File.WriteAllText(Path.Combine(root.FullName, "broken.rules.json"), """{ not-json }""");
        File.WriteAllText(arrayRootEntry, """[]""");

        var missing = Assert.Throws<FileNotFoundException>(() => ConversionEngine.BundleRules(missingIncludeEntry));
        Assert.Contains("Included rules file not found", missing.Message, StringComparison.Ordinal);

        var invalid = Assert.Throws<InvalidOperationException>(() => ConversionEngine.BundleRules(invalidJsonEntry));
        Assert.Contains("Invalid JSON in rules file", invalid.Message, StringComparison.Ordinal);

        var nonObject = Assert.Throws<InvalidOperationException>(() => ConversionEngine.BundleRules(arrayRootEntry));
        Assert.Contains("Rules file must contain a JSON object", nonObject.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BundleRules_ThrowsWhenIncludeIsNotArray()
    {
        var root = Directory.CreateTempSubdirectory("apiconvert-bundle-include-");
        var entryPath = Path.Combine(root.FullName, "entry.rules.json");
        File.WriteAllText(entryPath, """{ "include": "./shared.rules.json", "rules": [] }""");

        var error = Assert.Throws<InvalidOperationException>(() => ConversionEngine.BundleRules(entryPath));
        Assert.Contains("include must be an array", error.Message, StringComparison.Ordinal);
    }
}
