using System.Text.Json.Nodes;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class AdditionalBranchCoverageWaveTests
{
    [Fact]
    public void QueryStringConverter_ThirdDuplicateIndex_AppendsToExistingList()
    {
        var (value, error) = ConversionEngine.ParsePayload("arr[0]=a&arr[0]=b&arr[0]=c", DataFormat.Query);

        Assert.Null(error);
        var root = Assert.IsType<Dictionary<string, object?>>(value);
        var arr = Assert.IsType<List<object?>>(root["arr"]);
        var entry = Assert.IsType<List<object?>>(arr[0]);
        Assert.Equal(["a", "b", "c"], entry);
    }

    [Fact]
    public void XmlConverter_CoversListAddAndListRootFormatting()
    {
        var (value, error) = ConversionEngine.ParsePayload("<r><x>1</x><x>2</x><x>3</x></r>", DataFormat.Xml);
        Assert.Null(error);
        var root = Assert.IsType<Dictionary<string, object?>>(value);
        var r = Assert.IsType<Dictionary<string, object?>>(root["r"]);
        var x = Assert.IsType<List<object?>>(r["x"]);
        Assert.Equal(3, x.Count);

        var xml = ConversionEngine.FormatPayload(
            new Dictionary<string, object?> { ["item"] = new List<object?> { 1, 2, 3 } },
            DataFormat.Xml,
            pretty: false);

        Assert.Equal("<item><item>1</item><item>2</item><item>3</item></item>", xml);
    }

    [Fact]
    public void RulesBundler_CoversEntryPathGuardVisitedSkipAndIncludeTrim()
    {
        Assert.Throws<ArgumentException>(() => ConversionEngine.BundleRules("  "));

        var root = Directory.CreateTempSubdirectory("apiconvert-bundle-visited-");
        var commonPath = Path.Combine(root.FullName, "common.rules.json");
        var aPath = Path.Combine(root.FullName, "a.rules.json");
        var bPath = Path.Combine(root.FullName, "b.rules.json");
        var entryPath = Path.Combine(root.FullName, "entry.rules.json");

        File.WriteAllText(commonPath, """
        {
          "rules": [{ "kind": "field", "outputPaths": ["c"], "source": { "type": "constant", "value": "1" } }]
        }
        """);

        File.WriteAllText(aPath, """
        {
          "include": ["./common.rules.json", "   "],
          "rules": [{ "kind": "field", "outputPaths": ["a"], "source": { "type": "constant", "value": "1" } }]
        }
        """);

        File.WriteAllText(bPath, """
        {
          "include": ["./common.rules.json"],
          "rules": [{ "kind": "field", "outputPaths": ["b"], "source": { "type": "constant", "value": "1" } }]
        }
        """);

        File.WriteAllText(entryPath, """
        {
          "include": ["./a.rules.json", "./b.rules.json"],
          "rules": []
        }
        """);

        var bundled = ConversionEngine.BundleRules(entryPath);
        Assert.NotNull(bundled);
        Assert.NotNull(bundled.Rules);
    }

    [Fact]
    public void PayloadConverter_JsonNodeParseFailure_IsCaptured()
    {
        var node = JsonValue.Create(double.NaN);
        var (value, error) = ConversionEngine.ParsePayload(node, DataFormat.Json);

        Assert.Null(value);
        Assert.NotNull(error);
    }

    [Fact]
    public void RulesFormatter_CoversOptionalSourceFields()
    {
        var rawRules = """
        {
          "rules": [
            {
              "kind": "field",
              "to": ["x"],
              "source": {
                "type": "condition",
                "expression": "path(a) eq 1",
                "conditionOutput": "match",
                "elseIf": [
                  {
                    "expression": "path(a) eq 2",
                    "source": { "type": "constant", "value": "two" }
                  }
                ]
              }
            },
            {
              "kind": "field",
              "to": ["y"],
              "source": {
                "type": "merge",
                "paths": ["a", "b"],
                "mergeMode": "array",
                "separator": "|"
              }
            },
            {
              "kind": "field",
              "to": ["z"],
              "source": {
                "type": "transform",
                "path": "name",
                "transform": "split",
                "separator": ",",
                "tokenIndex": 1,
                "trimAfterSplit": false
              }
            }
          ]
        }
        """;

        var text = ConversionEngine.FormatConversionRules(rawRules, pretty: false);
        var root = JsonNode.Parse(text)!.AsObject();
        var cond = root["rules"]![0]!["source"]!.AsObject();
        Assert.Equal("match", cond["conditionOutput"]!.GetValue<string>());
        Assert.NotNull(cond["elseIf"]![0]!["source"]);

        var merge = root["rules"]![1]!["source"]!.AsObject();
        Assert.Equal("array", merge["mergeMode"]!.GetValue<string>());
        Assert.Equal("|", merge["separator"]!.GetValue<string>());

        var split = root["rules"]![2]!["source"]!.AsObject();
        Assert.Equal(1, split["tokenIndex"]!.GetValue<int>());
        Assert.False(split["trimAfterSplit"]!.GetValue<bool>());
    }

    [Fact]
    public void RulesNormalizer_CoversDeserializeFailureUnsupportedKindAndFragmentNameValidation()
    {
        var raw = """
        {
          "rules": 123
        }
        """;

        var fail = ConversionEngine.NormalizeConversionRules(raw);
        Assert.Contains(fail.ValidationErrors, e => e.Contains("could not be deserialized", StringComparison.Ordinal));

        var rules = new ConversionRules
        {
            Fragments = new Dictionary<string, RuleNode>
            {
                [" "] = new RuleNode(),
                ["frag"] = new RuleNode(),
                [" frag "] = new RuleNode()
            },
            Rules =
            [
                new RuleNode { Kind = "unknown" }
            ]
        };

        var normalized = ConversionEngine.NormalizeConversionRules(rules);
        Assert.Contains(normalized.ValidationErrors, e => e.Contains("fragment name is required", StringComparison.Ordinal));
        Assert.Contains(normalized.ValidationErrors, e => e.Contains("duplicate fragment name", StringComparison.Ordinal));
        Assert.Contains(normalized.ValidationErrors, e => e.Contains("unsupported kind 'unknown'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConversionEngine_CoversRunDoctorAndStreamBranches()
    {
        var doctorNoSample = ConversionEngine.RunRuleDoctor(new ConversionRules { Rules = [] });
        Assert.Contains(doctorNoSample.Findings, f => f.Code == "ACV-DOCTOR-100");

        var doctorWithRuntime = ConversionEngine.RunRuleDoctor(
            new ConversionRules
            {
                Rules =
                [
                    new RuleNode
                    {
                        Kind = "array",
                        InputPath = "missing",
                        OutputPaths = ["x"],
                        ItemRules = []
                    }
                ]
            },
            sampleInputText: "{\"ok\":true}",
            inputFormat: DataFormat.Json);

        Assert.Contains(doctorWithRuntime.Findings, f => f.Code == "ACV-DOCTOR-010");

        using var nd = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("\n\n{\"a\":1}\n\n"));
        var ndResults = new List<ConversionResult>();
        await foreach (var result in ConversionEngine.StreamConversionAsync(
                           nd,
                           new ConversionRules { Rules = [] },
                           new StreamConversionOptions { InputKind = StreamInputKind.Ndjson }))
        {
            ndResults.Add(result);
        }
        Assert.Single(ndResults);

        using var ql = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("\n\nname=Ada\n\n"));
        var qResults = new List<ConversionResult>();
        await foreach (var result in ConversionEngine.StreamConversionAsync(
                           ql,
                           new ConversionRules { Rules = [] },
                           new StreamConversionOptions { InputKind = StreamInputKind.QueryLines }))
        {
            qResults.Add(result);
        }
        Assert.Single(qResults);

        using var xml = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<root><x>1</x></root>"));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in ConversionEngine.StreamConversionAsync(
                               xml,
                               new ConversionRules { Rules = [] },
                               new StreamConversionOptions { InputKind = StreamInputKind.XmlElements, XmlItemPath = "." }))
            {
            }
        });

        var comp = ConversionEngine.CheckCompatibility(
            new ConversionRules
            {
                Rules =
                [
                    new RuleNode
                    {
                        Kind = "field",
                        OutputPaths = [],
                        Source = new ValueSource { Type = "path", Path = "name" }
                    }
                ]
            },
            "2.0.0");

        Assert.Contains(comp.Diagnostics, d => d.Code == "ACV-COMP-003");
        Assert.Contains(comp.Diagnostics, d => d.Code == "ACV-COMP-006");
    }
}
