using System.Text;
using System.Text.Json;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class ConversionEngineTests
{
    [Fact]
    public void NormalizeConversionRules_ParsesRulesArray()
    {
        var json = """
        {
          "version": 2,
          "inputFormat": "json",
          "outputFormat": "json",
          "rules": [
            {
              "kind": "field",
              "outputPaths": ["user.name"],
              "source": { "type": "path", "path": "name" }
            }
          ]
        }
        """;

        var rules = ConversionEngine.NormalizeConversionRules(json);

        Assert.Equal(2, rules.Version);
        Assert.Single(rules.Rules);
        Assert.Equal("field", rules.Rules[0].Kind);
        Assert.Empty(rules.ValidationErrors);
    }

    [Fact]
    public void NormalizeConversionRules_RejectsLegacyProperties()
    {
        var json = """
        {
          "version": 2,
          "fieldMappings": [
            {
              "outputPath": "user.name",
              "source": { "type": "path", "path": "name" }
            }
          ]
        }
        """;

        var rules = ConversionEngine.NormalizeConversionRules(json);

        Assert.Contains(rules.ValidationErrors, error => error.Contains("legacy property 'fieldMappings'"));
    }

    [Fact]
    public void ApplyConversion_ExecutesBranchThenElseIfElse()
    {
        var input = new Dictionary<string, object?>
        {
            ["score"] = 72d
        };

        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "branch",
                    Expression = "path(score) >= 90",
                    Then =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["grade"],
                            Source = new ValueSource { Type = "constant", Value = "A" }
                        }
                    ],
                    ElseIf =
                    [
                        new BranchElseIfRule
                        {
                            Expression = "path(score) >= 80",
                            Then =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["grade"],
                                    Source = new ValueSource { Type = "constant", Value = "B" }
                                }
                            ]
                        },
                        new BranchElseIfRule
                        {
                            Expression = "path(score) >= 70",
                            Then =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["grade"],
                                    Source = new ValueSource { Type = "constant", Value = "C" }
                                }
                            ]
                        }
                    ],
                    Else =
                    [
                        new RuleNode
                        {
                            Kind = "field",
                            OutputPaths = ["grade"],
                            Source = new ValueSource { Type = "constant", Value = "F" }
                        }
                    ]
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("C", output["grade"]);
    }

    [Fact]
    public void ApplyConversion_BranchInsideArrayItemRules_UsesRootAndItemScope()
    {
        var input = new Dictionary<string, object?>
        {
            ["meta"] = new Dictionary<string, object?> { ["source"] = "api" },
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["qty"] = 1d },
                new Dictionary<string, object?> { ["qty"] = 2d }
            }
        };

        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "array",
                    InputPath = "items",
                    OutputPaths = ["items"],
                    ItemRules =
                    [
                        new RuleNode
                        {
                            Kind = "branch",
                            Expression = "path($.meta.source) == 'api' && path(qty) >= 2",
                            Then =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["priority"],
                                    Source = new ValueSource { Type = "constant", Value = "high" }
                                }
                            ],
                            Else =
                            [
                                new RuleNode
                                {
                                    Kind = "field",
                                    OutputPaths = ["priority"],
                                    Source = new ValueSource { Type = "constant", Value = "normal" }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        var items = Assert.IsType<List<object?>>(output["items"]);
        var first = Assert.IsType<Dictionary<string, object?>>(items[0]);
        var second = Assert.IsType<Dictionary<string, object?>>(items[1]);
        Assert.Equal("normal", first["priority"]);
        Assert.Equal("high", second["priority"]);
    }

    [Fact]
    public void ApplyConversion_AddsValidationErrorsFromLegacyPayload()
    {
        var input = new Dictionary<string, object?> { ["name"] = "Ada" };
        var rawRules = """
        {
          "version": 2,
          "fieldMappings": [
            {
              "outputPath": "user.name",
              "source": { "type": "path", "path": "name" }
            }
          ]
        }
        """;

        var result = ConversionEngine.ApplyConversion(input, rawRules);

        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("legacy property 'fieldMappings'"));
    }

    [Fact]
    public void ApplyConversion_ConditionSourceStillWorks()
    {
        var input = new Dictionary<string, object?> { ["flag"] = true };
        var rules = new ConversionRules
        {
            Rules =
            [
                new RuleNode
                {
                    Kind = "field",
                    OutputPaths = ["status"],
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Expression = "path(flag) == true",
                        TrueValue = "enabled",
                        FalseValue = "disabled"
                    }
                }
            ]
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Empty(result.Errors);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("enabled", output["status"]);
    }

    [Fact]
    public void ApplyConversion_RuleRecursionDepthExceeded_AddsError()
    {
        var root = new RuleNode { Kind = "branch", Expression = "true", Then = [] };
        var cursor = root;
        for (var index = 0; index < 70; index++)
        {
            var child = new RuleNode { Kind = "branch", Expression = "true", Then = [] };
            cursor.Then.Add(child);
            cursor = child;
        }

        var rules = new ConversionRules { Rules = [root] };

        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?>(), rules);

        Assert.Contains(result.Errors, error => error.Contains("rule recursion limit exceeded"));
    }

    [Fact]
    public void ParseAndFormatQueryString_AreConsistent()
    {
        var (value, error) = ConversionEngine.ParsePayload("user.name=Ada&user.age=37", DataFormat.Query);

        Assert.Null(error);

        var formatted = ConversionEngine.FormatPayload(value, DataFormat.Query, pretty: false);

        Assert.Equal("user.age=37&user.name=Ada", formatted);
    }

    [Fact]
    public void ParsePayload_Stream_ParsesJson()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{"name":"Ada"}"""));

        var (value, error) = ConversionEngine.ParsePayload(stream, DataFormat.Json);

        Assert.Null(error);
        var output = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.Equal("Ada", output["name"]);
    }

    [Fact]
    public void SharedCases_AllPass()
    {
        var casesRoot = LocateCasesRoot();
        var caseDirs = Directory.GetDirectories(casesRoot).OrderBy(path => path, StringComparer.Ordinal).ToList();
        Assert.NotEmpty(caseDirs);

        foreach (var caseDir in caseDirs)
        {
            var caseName = Path.GetFileName(caseDir);
            var rulesPath = Path.Combine(caseDir, "rules.json");
            Assert.True(File.Exists(rulesPath), $"Case '{caseName}' is missing rules.json.");

            var inputPath = FindSingleFile(caseDir, "input", caseName);
            var outputPath = FindSingleFile(caseDir, "output", caseName);

            var rulesText = File.ReadAllText(rulesPath);
            var inputText = File.ReadAllText(inputPath);
            var expectedText = File.ReadAllText(outputPath);

            var inputFormat = ExtensionToFormat(Path.GetExtension(inputPath));
            var outputFormat = ExtensionToFormat(Path.GetExtension(outputPath));

            var (inputValue, parseError) = ConversionEngine.ParsePayload(inputText, inputFormat);
            Assert.Null(parseError);

            var result = ConversionEngine.ApplyConversion(inputValue, rulesText);
            Assert.True(result.Errors.Count == 0, $"Case '{caseName}' failed: {string.Join("; ", result.Errors)}");

            var actualText = outputFormat == DataFormat.Query
                ? ConversionEngine.FormatPayload(result.Output, outputFormat, pretty: false)
                : ConversionEngine.FormatPayload(result.Output, outputFormat, pretty: true);

            Assert.Equal(
                NormalizeOutput(expectedText, outputFormat),
                NormalizeOutput(actualText, outputFormat));
        }
    }

    private static string LocateCasesRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.GetFullPath(Path.Combine(baseDir, "../../../../../../tests/cases"));
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var testsCases = Path.Combine(current.FullName, "tests", "cases");
            if (Directory.Exists(testsCases))
            {
                return testsCases;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate tests/cases.");
    }

    private static string FindSingleFile(string caseDirectory, string basename, string caseName)
    {
        var matches = Directory.GetFiles(caseDirectory, $"{basename}.*", SearchOption.TopDirectoryOnly);
        Assert.True(matches.Length == 1, $"Case '{caseName}' must include exactly one {basename}.* file.");
        return matches[0];
    }

    private static DataFormat ExtensionToFormat(string extension)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "json" => DataFormat.Json,
            "xml" => DataFormat.Xml,
            "txt" => DataFormat.Query,
            _ => throw new InvalidOperationException($"Unsupported extension '{extension}'.")
        };
    }

    private static string NormalizeOutput(string value, DataFormat format)
    {
        var normalized = value.Replace("\r\n", "\n").Trim();
        if (format != DataFormat.Json)
        {
            return normalized;
        }

        using var doc = JsonDocument.Parse(normalized.Length == 0 ? "{}" : normalized);
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = false });
    }
}
