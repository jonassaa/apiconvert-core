using System.Text.Json;
using System.Text.RegularExpressions;

namespace Apiconvert.Core.Tests;

public sealed class SchemaVersioningTests
{
    [Fact]
    public void CurrentSchema_MatchesLatestVersionedSchema()
    {
        var repoRoot = LocateRepoRoot();
        var schemasRoot = Path.Combine(repoRoot, "schemas", "rules");
        var versionDirectories = Directory.GetDirectories(schemasRoot, "v*")
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Where(name => Regex.IsMatch(name!, "^v\\d+\\.\\d+\\.\\d+$"))
            .OrderBy(ParseVersion)
            .ToList();

        Assert.NotEmpty(versionDirectories);

        var latestVersion = versionDirectories[^1]!;
        var latestSchemaPath = Path.Combine(schemasRoot, latestVersion, "schema.json");
        var currentSchemaPath = Path.Combine(schemasRoot, "current", "schema.json");
        Assert.True(File.Exists(latestSchemaPath), $"Missing latest versioned schema: {latestSchemaPath}");
        Assert.True(File.Exists(currentSchemaPath), $"Missing current schema alias: {currentSchemaPath}");

        var latestText = File.ReadAllText(latestSchemaPath);
        var currentText = File.ReadAllText(currentSchemaPath);
        Assert.Equal(latestText, currentText);
    }

    [Fact]
    public void LegacySchemaAlias_RemainsAvailableAndParsable()
    {
        var repoRoot = LocateRepoRoot();
        var legacySchemaPath = Path.Combine(repoRoot, "schemas", "rules", "rules.schema.json");
        Assert.True(File.Exists(legacySchemaPath), $"Missing legacy schema alias: {legacySchemaPath}");

        var schemaText = File.ReadAllText(legacySchemaPath);
        using var document = JsonDocument.Parse(schemaText);

        var root = document.RootElement;
        Assert.Equal("object", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("$defs", out _));
        Assert.True(root.TryGetProperty("properties", out _));
    }

    private static string LocateRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static Version ParseVersion(string? versionDirectory)
    {
        if (string.IsNullOrWhiteSpace(versionDirectory))
        {
            throw new InvalidOperationException("Version directory name cannot be null or empty.");
        }
        return Version.Parse(versionDirectory[1..]);
    }
}
