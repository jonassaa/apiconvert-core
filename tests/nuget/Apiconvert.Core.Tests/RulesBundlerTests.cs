using Apiconvert.Core.Converters;

namespace Apiconvert.Core.Tests;

public sealed class RulesBundlerTests
{
    [Fact]
    public void BundleRules_MergesIncludesInDeterministicOrder()
    {
        var root = Directory.CreateTempSubdirectory("apiconvert-bundle-");
        var sharedPath = Path.Combine(root.FullName, "shared.rules.json");
        var entryPath = Path.Combine(root.FullName, "entry.rules.json");

        File.WriteAllText(sharedPath, """
        {
          "inputFormat": "json",
          "outputFormat": "json",
          "rules": [
            {
              "kind": "field",
              "outputPaths": ["customer.id"],
              "source": { "type": "path", "path": "id" }
            }
          ]
        }
        """);

        File.WriteAllText(entryPath, """
        {
          "include": ["./shared.rules.json"],
          "inputFormat": "json",
          "outputFormat": "json",
          "rules": [
            {
              "kind": "field",
              "outputPaths": ["customer.name"],
              "source": { "type": "path", "path": "name" }
            }
          ]
        }
        """);

        var bundled = ConversionEngine.BundleRules(entryPath);

        Assert.Equal(2, bundled.Rules.Count);
        Assert.Equal("customer.id", bundled.Rules[0].OutputPaths[0]);
        Assert.Equal("customer.name", bundled.Rules[1].OutputPaths[0]);
    }

    [Fact]
    public void BundleRules_ThrowsForCircularIncludes()
    {
        var root = Directory.CreateTempSubdirectory("apiconvert-bundle-cycle-");
        var aPath = Path.Combine(root.FullName, "a.rules.json");
        var bPath = Path.Combine(root.FullName, "b.rules.json");

        File.WriteAllText(aPath, """{ "include": ["./b.rules.json"], "rules": [] }""");
        File.WriteAllText(bPath, """{ "include": ["./a.rules.json"], "rules": [] }""");

        var ex = Assert.Throws<InvalidOperationException>(() => ConversionEngine.BundleRules(aPath));
        Assert.Contains("Circular include detected", ex.Message);
    }
}
