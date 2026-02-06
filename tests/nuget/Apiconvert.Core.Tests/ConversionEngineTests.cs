using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class ConversionEngineTests
{
    [Fact]
    public void ApplyConversion_MapsFieldPath()
    {
        var input = new Dictionary<string, object?>
        {
            ["name"] = "Ada"
        };

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "user.name",
                    Source = new ValueSource { Type = "path", Path = "name" }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        var user = Assert.IsType<Dictionary<string, object?>>(output["user"]);
        Assert.Equal("Ada", user["name"]);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ApplyConversion_UsesDefaultValueWhenMissing()
    {
        var input = new Dictionary<string, object?>();

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "user.name",
                    Source = new ValueSource { Type = "path", Path = "name" },
                    DefaultValue = "Unknown"
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        var user = Assert.IsType<Dictionary<string, object?>>(output["user"]);
        Assert.Equal("Unknown", user["name"]);
    }

    [Fact]
    public void ApplyConversion_ConcatsTransformWithConstants()
    {
        var input = new Dictionary<string, object?>
        {
            ["first"] = "Ada",
            ["last"] = "Lovelace"
        };

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "fullName",
                    Source = new ValueSource
                    {
                        Type = "transform",
                        Transform = TransformType.Concat,
                        Path = "first,const:-,last"
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("Ada-Lovelace", output["fullName"]);
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
    public void FormatPayload_Stream_WritesJson()
    {
        var input = new Dictionary<string, object?> { ["name"] = "Ada" };
        using var stream = new MemoryStream();

        ConversionEngine.FormatPayload(input, DataFormat.Json, stream, pretty: false);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        Assert.Equal("""{"name":"Ada"}""", json);
    }

    [Fact]
    public void ParsePayload_JsonNode_ParsesJson()
    {
        var node = JsonNode.Parse("""{"name":"Ada","active":true}""");

        var (value, error) = ConversionEngine.ParsePayload(node);

        Assert.Null(error);
        var output = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.Equal("Ada", output["name"]);
        Assert.Equal(true, output["active"]);
    }

    [Fact]
    public void ParsePayload_JsonNode_ReturnsErrorForNonJsonFormat()
    {
        var node = JsonNode.Parse("""{"name":"Ada"}""");

        var (value, error) = ConversionEngine.ParsePayload(node, DataFormat.Xml);

        Assert.Null(value);
        Assert.Equal("JsonNode input is only supported for DataFormat.Json.", error);
    }

    [Fact]
    public void NormalizeConversionRules_ParsesJsonRules()
    {
        var json = """
        {
          "version": 2,
          "inputFormat": "json",
          "outputFormat": "json",
          "fieldMappings": [
            {
              "outputPath": "user.name",
              "source": { "type": "path", "path": "name" }
            }
          ],
          "arrayMappings": []
        }
        """;

        var rules = ConversionEngine.NormalizeConversionRules(json);

        Assert.Equal(2, rules.Version);
        Assert.Single(rules.FieldMappings);
        Assert.Empty(rules.ArrayMappings);
    }

    [Fact]
    public void NormalizeConversionRules_ParsesLegacyRules()
    {
        var json = """
        {
          "version": 1,
          "rows": [
            {
              "outputPath": "user.name",
              "sourceType": "path",
              "sourceValue": "name",
              "defaultValue": "Unknown"
            }
          ]
        }
        """;

        var rules = ConversionEngine.NormalizeConversionRules(json);

        Assert.Single(rules.FieldMappings);
        Assert.Equal("user.name", rules.FieldMappings[0].OutputPath);
        Assert.Equal("Unknown", rules.FieldMappings[0].DefaultValue);
    }

    [Fact]
    public void NormalizeConversionRules_ReturnsEmptyOnInvalidJson()
    {
        var rules = ConversionEngine.NormalizeConversionRules("{not-valid-json");

        Assert.Empty(rules.FieldMappings);
        Assert.Empty(rules.ArrayMappings);
    }

    [Fact]
    public void ApplyConversion_ReturnsInputWhenNoRules()
    {
        var input = new Dictionary<string, object?> { ["name"] = "Ada" };

        var result = ConversionEngine.ApplyConversion(input, new ConversionRules());

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("Ada", output["name"]);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ApplyConversion_AddsErrorsForInvalidArrayMappings()
    {
        var input = new Dictionary<string, object?> { ["items"] = "not-an-array" };
        var rules = new ConversionRules
        {
            ArrayMappings = new List<ArrayRule>
            {
                new()
                {
                    InputPath = "items",
                    OutputPath = "",
                    ItemMappings = new List<FieldRule>()
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ApplyConversion_MapsArrayItemsAndCoercesSingle()
    {
        var input = new Dictionary<string, object?>
        {
            ["items"] = new Dictionary<string, object?> { ["name"] = "Ada" }
        };

        var rules = new ConversionRules
        {
            ArrayMappings = new List<ArrayRule>
            {
                new()
                {
                    InputPath = "items",
                    OutputPath = "users",
                    CoerceSingle = true,
                    ItemMappings = new List<FieldRule>
                    {
                        new()
                        {
                            OutputPath = "name",
                            Source = new ValueSource { Type = "path", Path = "name" }
                        }
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        var users = Assert.IsType<List<object?>>(output["users"]);
        var first = Assert.IsType<Dictionary<string, object?>>(users[0]);
        Assert.Equal("Ada", first["name"]);
    }

    [Fact]
    public void ApplyConversion_ConditionSourceResolvesTrueFalse()
    {
        var input = new Dictionary<string, object?> { ["flag"] = true };
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "status",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "flag", Operator = ConditionOperator.Equals, Value = "true" },
                        TrueValue = "enabled",
                        FalseValue = "disabled"
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal("enabled", output["status"]);
    }

    [Fact]
    public void ApplyConversion_TransformNumberAndBoolean()
    {
        var input = new Dictionary<string, object?>
        {
            ["count"] = "42",
            ["flag"] = "yes"
        };

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "count",
                    Source = new ValueSource { Type = "transform", Path = "count", Transform = TransformType.Number }
                },
                new()
                {
                    OutputPath = "flag",
                    Source = new ValueSource { Type = "transform", Path = "flag", Transform = TransformType.Boolean }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal(42d, output["count"]);
        Assert.Equal(true, output["flag"]);
    }

    [Fact]
    public void ParseAndFormatXml_RoundTripsRoot()
    {
        var xml = "<root><user><name>Ada</name><age>37</age></user></root>";
        var (value, error) = ConversionEngine.ParsePayload(xml, DataFormat.Xml);

        Assert.Null(error);

        var formatted = ConversionEngine.FormatPayload(value, DataFormat.Xml, pretty: false);

        Assert.Contains("<root>", formatted);
        Assert.Contains("<name>Ada</name>", formatted);
    }

    [Fact]
    public void ParseJson_HandlesArrayAndNumbers()
    {
        var (value, error) = ConversionEngine.ParsePayload("""{"nums":[1,2.5]}""", DataFormat.Json);

        Assert.Null(error);

        var obj = Assert.IsType<Dictionary<string, object?>>(value);
        var nums = Assert.IsType<List<object?>>(obj["nums"]);
        Assert.Equal(1d, Assert.IsType<double>(nums[0]));
        Assert.Equal(2.5d, Assert.IsType<double>(nums[1]));
    }

    [Fact]
    public void FormatQueryString_SerializesNestedObjects()
    {
        var input = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = "Ada",
                ["tags"] = new List<object?> { "a", "b" }
            }
        };

        var formatted = ConversionEngine.FormatPayload(input, DataFormat.Query, pretty: false);

        Assert.Contains("user.name=Ada", formatted);
        Assert.Contains("user.tags=a", formatted);
        Assert.Contains("user.tags=b", formatted);
    }

    [Fact]
    public void ParsePayload_ReturnsErrorOnInvalidXml()
    {
        var (value, error) = ConversionEngine.ParsePayload("<root><bad></root>", DataFormat.Xml);

        Assert.Null(value);
        Assert.NotNull(error);
    }

    [Fact]
    public void EndToEnd_JsonToJson_FieldAndArrayRules()
    {
        var inputJson = """
        {
          "user": { "first": "Ada", "last": "Lovelace" },
          "items": [
            { "sku": "A1", "qty": 2 },
            { "sku": "B2", "qty": 1 }
          ]
        }
        """;

        var rules = new ConversionRules
        {
            InputFormat = DataFormat.Json,
            OutputFormat = DataFormat.Json,
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "profile.fullName",
                    Source = new ValueSource
                    {
                        Type = "transform",
                        Transform = TransformType.Concat,
                        Path = "user.first,const:-,user.last"
                    }
                }
            },
            ArrayMappings = new List<ArrayRule>
            {
                new()
                {
                    InputPath = "items",
                    OutputPath = "lines",
                    ItemMappings = new List<FieldRule>
                    {
                        new()
                        {
                            OutputPath = "code",
                            Source = new ValueSource { Type = "path", Path = "sku" }
                        },
                        new()
                        {
                            OutputPath = "quantity",
                            Source = new ValueSource { Type = "path", Path = "qty" }
                        }
                    }
                }
            }
        };

        var (value, error) = ConversionEngine.ParsePayload(inputJson, DataFormat.Json);

        Assert.Null(error);

        var result = ConversionEngine.ApplyConversion(value, rules);
        var outputJson = ConversionEngine.FormatPayload(result.Output, DataFormat.Json, pretty: false);

        Assert.Contains("\"fullName\":\"Ada-Lovelace\"", outputJson);
        Assert.Contains("\"lines\"", outputJson);
        Assert.Contains("\"code\":\"A1\"", outputJson);
        Assert.Contains("\"quantity\":2", outputJson);
    }

    [Fact]
    public void EndToEnd_XmlToJson_WithAttributesAndText()
    {
        var inputXml = """
        <order id="42">
          <customer>
            <name>Ada</name>
          </customer>
          <total>19.95</total>
        </order>
        """;

        var rules = new ConversionRules
        {
            InputFormat = DataFormat.Xml,
            OutputFormat = DataFormat.Json,
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "orderId",
                    Source = new ValueSource { Type = "path", Path = "order.@_id" }
                },
                new()
                {
                    OutputPath = "customerName",
                    Source = new ValueSource { Type = "path", Path = "order.customer.name" }
                },
                new()
                {
                    OutputPath = "total",
                    Source = new ValueSource { Type = "transform", Path = "order.total", Transform = TransformType.Number }
                }
            }
        };

        var (value, error) = ConversionEngine.ParsePayload(inputXml, DataFormat.Xml);

        Assert.Null(error);

        var result = ConversionEngine.ApplyConversion(value, rules);
        var outputJson = ConversionEngine.FormatPayload(result.Output, DataFormat.Json, pretty: false);

        Assert.Contains("\"orderId\":42", outputJson);
        Assert.Contains("\"customerName\":\"Ada\"", outputJson);
        Assert.Contains("\"total\":19.95", outputJson);
    }

    [Fact]
    public void EndToEnd_QueryToJson_WithConditionAndDefaults()
    {
        var inputQuery = "user.name=Ada&user.active=yes&meta.source=web";

        var rules = new ConversionRules
        {
            InputFormat = DataFormat.Query,
            OutputFormat = DataFormat.Json,
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "name",
                    Source = new ValueSource { Type = "path", Path = "user.name" }
                },
                new()
                {
                    OutputPath = "isActive",
                    Source = new ValueSource
                    {
                        Type = "transform",
                        Path = "user.active",
                        Transform = TransformType.Boolean
                    }
                },
                new()
                {
                    OutputPath = "source",
                    Source = new ValueSource { Type = "path", Path = "meta.source" },
                    DefaultValue = "unknown"
                },
                new()
                {
                    OutputPath = "tier",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule
                        {
                            Path = "user.active",
                            Operator = ConditionOperator.Equals,
                            Value = "yes"
                        },
                        TrueValue = "pro",
                        FalseValue = "free"
                    }
                }
            }
        };

        var (value, error) = ConversionEngine.ParsePayload(inputQuery, DataFormat.Query);

        Assert.Null(error);

        var result = ConversionEngine.ApplyConversion(value, rules);
        var outputJson = ConversionEngine.FormatPayload(result.Output, DataFormat.Json, pretty: false);

        Assert.Contains("\"name\":\"Ada\"", outputJson);
        Assert.Contains("\"isActive\":true", outputJson);
        Assert.Contains("\"source\":\"web\"", outputJson);
        Assert.Contains("\"tier\":\"pro\"", outputJson);
    }

    [Fact]
    public void EndToEnd_JsonToQuery_FlattensNestedOutput()
    {
        var inputJson = """
        {
          "user": { "id": 7, "name": "Ada" },
          "tags": ["a", "b"]
        }
        """;

        var rules = new ConversionRules
        {
            InputFormat = DataFormat.Json,
            OutputFormat = DataFormat.Query,
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "user.id",
                    Source = new ValueSource { Type = "path", Path = "user.id" }
                },
                new()
                {
                    OutputPath = "user.name",
                    Source = new ValueSource { Type = "path", Path = "user.name" }
                },
                new()
                {
                    OutputPath = "tags",
                    Source = new ValueSource { Type = "path", Path = "tags" }
                }
            }
        };

        var (value, error) = ConversionEngine.ParsePayload(inputJson, DataFormat.Json);

        Assert.Null(error);

        var result = ConversionEngine.ApplyConversion(value, rules);
        var outputQuery = ConversionEngine.FormatPayload(result.Output, DataFormat.Query, pretty: false);

        Assert.Contains("user.id=7", outputQuery);
        Assert.Contains("user.name=Ada", outputQuery);
        Assert.Contains("tags=a", outputQuery);
        Assert.Contains("tags=b", outputQuery);
    }

    [Fact]
    public void NormalizeConversionRules_NormalizesDefaults()
    {
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new() { OutputPath = "name", DefaultValue = null! }
            },
            ArrayMappings = new List<ArrayRule>
            {
                new()
                {
                    InputPath = "items",
                    OutputPath = "items",
                    ItemMappings = new List<FieldRule>
                    {
                        new() { OutputPath = "code", DefaultValue = null! }
                    }
                }
            }
        };

        var normalized = ConversionEngine.NormalizeConversionRules(rules);

        Assert.Equal(string.Empty, normalized.FieldMappings[0].DefaultValue);
        Assert.Equal(string.Empty, normalized.ArrayMappings[0].ItemMappings[0].DefaultValue);
    }

    [Fact]
    public void NormalizeConversionRules_LegacyTransformRow()
    {
        var json = """
        {
          "version": 1,
          "rows": [
            {
              "outputPath": "user.name",
              "sourceType": "transform",
              "sourceValue": "name",
              "transformType": "toUpperCase"
            }
          ]
        }
        """;

        var rules = ConversionEngine.NormalizeConversionRules(json);

        Assert.Single(rules.FieldMappings);
        Assert.Equal("transform", rules.FieldMappings[0].Source.Type);
        Assert.Equal(TransformType.ToUpperCase, rules.FieldMappings[0].Source.Transform);
    }

    [Fact]
    public void ApplyConversion_UsesConstantSourceAndRootPath()
    {
        var input = new Dictionary<string, object?> { ["name"] = "Ada" };
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "constValue",
                    Source = new ValueSource { Type = "constant", Value = "42" }
                },
                new()
                {
                    OutputPath = "root",
                    Source = new ValueSource { Type = "path", Path = "$" }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        Assert.Equal(42d, output["constValue"]);
        Assert.Same(input, output["root"]);
    }

    [Fact]
    public void ApplyConversion_AddsErrorForMissingOutputPath()
    {
        var input = new Dictionary<string, object?> { ["name"] = "Ada" };
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "",
                    Source = new ValueSource { Type = "path", Path = "name" }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.Single(result.Errors);
    }

    [Fact]
    public void ApplyConversion_EvaluatesAllConditionOperators()
    {
        var input = new Dictionary<string, object?>
        {
            ["name"] = "Ada",
            ["count"] = 5d,
            ["list"] = new List<object?> { "a", "b" }
        };

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "exists",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "name", Operator = ConditionOperator.Exists },
                        TrueValue = "y",
                        FalseValue = "n"
                    }
                },
                new()
                {
                    OutputPath = "equals",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "name", Operator = ConditionOperator.Equals, Value = "Ada" },
                        TrueValue = "y",
                        FalseValue = "n"
                    }
                },
                new()
                {
                    OutputPath = "notEquals",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "name", Operator = ConditionOperator.NotEquals, Value = "Bob" },
                        TrueValue = "y",
                        FalseValue = "n"
                    }
                },
                new()
                {
                    OutputPath = "includes",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "list", Operator = ConditionOperator.Includes, Value = "b" },
                        TrueValue = "y",
                        FalseValue = "n"
                    }
                },
                new()
                {
                    OutputPath = "gt",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "count", Operator = ConditionOperator.Gt, Value = "3" },
                        TrueValue = "y",
                        FalseValue = "n"
                    }
                },
                new()
                {
                    OutputPath = "lt",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "count", Operator = ConditionOperator.Lt, Value = "10" },
                        TrueValue = "y",
                        FalseValue = "n"
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("y", output["exists"]);
        Assert.Equal("y", output["equals"]);
        Assert.Equal("y", output["notEquals"]);
        Assert.Equal("y", output["includes"]);
        Assert.Equal("y", output["gt"]);
        Assert.Equal("y", output["lt"]);
    }

    [Fact]
    public void ApplyConversion_TransformsLowerUpperAndEmptyNumber()
    {
        var input = new Dictionary<string, object?>
        {
            ["lower"] = "ADA",
            ["upper"] = "ada",
            ["empty"] = ""
        };

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "lower",
                    Source = new ValueSource { Type = "transform", Path = "lower", Transform = TransformType.ToLowerCase }
                },
                new()
                {
                    OutputPath = "upper",
                    Source = new ValueSource { Type = "transform", Path = "upper", Transform = TransformType.ToUpperCase }
                },
                new()
                {
                    OutputPath = "emptyNumber",
                    Source = new ValueSource { Type = "transform", Path = "empty", Transform = TransformType.Number }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("ada", output["lower"]);
        Assert.Equal("ADA", output["upper"]);
        Assert.Equal(string.Empty, output["emptyNumber"]);
    }

    [Fact]
    public void ParseQueryString_ParsesArraysAndEmptyValues()
    {
        var (value, error) = ConversionEngine.ParsePayload("items[0]=a&items[1]=b&flag", DataFormat.Query);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var items = Assert.IsType<List<object?>>(output["items"]);
        Assert.Equal("a", items[0]);
        Assert.Equal("b", items[1]);
        Assert.Equal(string.Empty, output["flag"]);
    }

    [Fact]
    public void GetValueByPath_HandlesIndexedAndNumericSegments()
    {
        var input = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["name"] = "Ada" },
                new Dictionary<string, object?> { ["name"] = "Bob" }
            }
        };

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "first",
                    Source = new ValueSource { Type = "path", Path = "items[0].name" }
                },
                new()
                {
                    OutputPath = "second",
                    Source = new ValueSource { Type = "path", Path = "items.1.name" }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("Ada", output["first"]);
        Assert.Equal("Bob", output["second"]);
    }

    [Fact]
    public void FormatXml_HandlesEmptyAndMultipleRoots()
    {
        var empty = ConversionEngine.FormatPayload(null, DataFormat.Xml, pretty: false);
        Assert.Contains("<root", empty);

        var multi = new Dictionary<string, object?>
        {
            ["one"] = "1",
            ["two"] = "2"
        };
        var formatted = ConversionEngine.FormatPayload(multi, DataFormat.Xml, pretty: false);

        Assert.Contains("<root>", formatted);
        Assert.Contains("<one>1</one>", formatted);
        Assert.Contains("<two>2</two>", formatted);
    }

    [Fact]
    public void ParseQueryString_RepeatedKeysBecomeLists()
    {
        var (value, error) = ConversionEngine.ParsePayload("tag=a&tag=b", DataFormat.Query);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var tags = Assert.IsType<List<object?>>(output["tag"]);
        Assert.Equal("a", tags[0]);
        Assert.Equal("b", tags[1]);
    }

    [Fact]
    public void ParseQueryString_BracketArraysMapToLists()
    {
        var (value, error) = ConversionEngine.ParsePayload("user[roles][]=admin&user[roles][]=user", DataFormat.Query);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var user = Assert.IsType<Dictionary<string, object?>>(output["user"]);
        var roles = Assert.IsType<List<object?>>(user["roles"]);
        Assert.Equal("admin", roles[0]);
        Assert.Equal("user", roles[1]);
    }

    [Fact]
    public void NormalizeConversionRules_ReturnsEmptyForJsonArray()
    {
        using var doc = JsonDocument.Parse("[]");

        var rules = ConversionEngine.NormalizeConversionRules(doc.RootElement);

        Assert.Empty(rules.FieldMappings);
        Assert.Empty(rules.ArrayMappings);
    }

    [Fact]
    public void NormalizeConversionRules_ReturnsEmptyForInvalidRuleShape()
    {
        var json = """{ "version": "nope", "fieldMappings": {} }""";

        var rules = ConversionEngine.NormalizeConversionRules(json);

        Assert.Empty(rules.FieldMappings);
        Assert.Empty(rules.ArrayMappings);
    }

    [Fact]
    public void NormalizeConversionRules_LegacyConstantRow()
    {
        var json = """
        {
          "version": 1,
          "rows": [
            {
              "outputPath": "status",
              "sourceType": "constant",
              "sourceValue": "ready"
            }
          ]
        }
        """;

        var rules = ConversionEngine.NormalizeConversionRules(json);

        Assert.Single(rules.FieldMappings);
        Assert.Equal("constant", rules.FieldMappings[0].Source.Type);
        Assert.Equal("ready", rules.FieldMappings[0].Source.Value);
    }

    [Fact]
    public void ApplyConversion_ArrayRuleMissingOutputPathAddsError()
    {
        var input = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["name"] = "Ada" }
            }
        };

        var rules = new ConversionRules
        {
            ArrayMappings = new List<ArrayRule>
            {
                new()
                {
                    InputPath = "items",
                    OutputPath = " ",
                    ItemMappings = new List<FieldRule>
                    {
                        new()
                        {
                            OutputPath = "name",
                            Source = new ValueSource { Type = "path", Path = "name" }
                        }
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ApplyConversion_ConditionSourceWithoutConditionReturnsNull()
    {
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "status",
                    Source = new ValueSource { Type = "condition" }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?>(), rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Null(output["status"]);
    }

    [Fact]
    public void ApplyConversion_UnknownSourceTypeSetsNull()
    {
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "value",
                    Source = new ValueSource { Type = "mystery" }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(new Dictionary<string, object?>(), rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Null(output["value"]);
    }

    [Fact]
    public void ApplyConversion_IncludesStringCondition()
    {
        var input = new Dictionary<string, object?> { ["name"] = "Ada Lovelace" };
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "hasAda",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "name", Operator = ConditionOperator.Includes, Value = "Ada" },
                        TrueValue = "yes",
                        FalseValue = "no"
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("yes", output["hasAda"]);
    }

    [Fact]
    public void ApplyConversion_BooleanTransformHandlesBoolAndObject()
    {
        var input = new Dictionary<string, object?>
        {
            ["flag"] = true,
            ["value"] = 1
        };

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "flag",
                    Source = new ValueSource { Type = "transform", Path = "flag", Transform = TransformType.Boolean }
                },
                new()
                {
                    OutputPath = "value",
                    Source = new ValueSource { Type = "transform", Path = "value", Transform = TransformType.Boolean }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal(true, output["flag"]);
        Assert.Equal(true, output["value"]);
    }

    [Fact]
    public void ApplyConversion_NumberTransformHandlesNonConvertibleValues()
    {
        var input = new Dictionary<string, object?>
        {
            ["value"] = new DateTime(2020, 1, 1)
        };

        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "value",
                    Source = new ValueSource { Type = "transform", Path = "value", Transform = TransformType.Number }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.True(double.IsNaN((double)output["value"]!));
    }

    [Fact]
    public void ParseXml_MergesDuplicateChildrenIntoList()
    {
        var xml = "<root><item>1</item><item>2</item></root>";
        var (value, error) = ConversionEngine.ParsePayload(xml, DataFormat.Xml);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var root = Assert.IsType<Dictionary<string, object?>>(output["root"]);
        var items = Assert.IsType<List<object?>>(root["item"]);
        Assert.Equal(1d, items[0]);
        Assert.Equal(2d, items[1]);
    }

    [Fact]
    public void FormatXml_WritesAttributesAndText()
    {
        var input = new Dictionary<string, object?>
        {
            ["root"] = new Dictionary<string, object?>
            {
                ["@_id"] = 1,
                ["#text"] = "Ada"
            }
        };

        var xml = ConversionEngine.FormatPayload(input, DataFormat.Xml, pretty: false);

        Assert.Contains("id=\"1\"", xml);
        Assert.Contains(">Ada</", xml);
    }

    [Fact]
    public void FormatXml_WritesObjectChildLists()
    {
        var input = new Dictionary<string, object?>
        {
            ["root"] = new Dictionary<string, object?>
            {
                ["item"] = new List<object?> { "a", "b" }
            }
        };

        var xml = ConversionEngine.FormatPayload(input, DataFormat.Xml, pretty: false);

        Assert.Contains("<item>a</item>", xml);
        Assert.Contains("<item>b</item>", xml);
    }

    [Fact]
    public void ParseQueryKey_EmptyKeyFallsBackToRawKey()
    {
        var (value, error) = ConversionEngine.ParsePayload("=value", DataFormat.Query);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.True(output.ContainsKey(string.Empty));
        Assert.Equal("value", output[string.Empty]);
    }

    [Fact]
    public void ParseQueryString_DuplicateArrayIndexCreatesList()
    {
        var (value, error) = ConversionEngine.ParsePayload("items[0]=a&items[0]=b&items[0]=c", DataFormat.Query);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var items = Assert.IsType<List<object?>>(output["items"]);
        var first = Assert.IsType<List<object?>>(items[0]);
        Assert.Equal("a", first[0]);
        Assert.Equal("b", first[1]);
        Assert.Equal("c", first[2]);
    }

    [Fact]
    public void FormatQueryString_SkipsEmptyKey()
    {
        var input = new Dictionary<string, object?>
        {
            [""] = "value",
            ["ok"] = "yes"
        };

        var output = ConversionEngine.FormatPayload(input, DataFormat.Query, pretty: false);

        Assert.Equal("ok=yes", output);
    }

    [Fact]
    public void ParseJson_HandlesStringAndNull()
    {
        var (value, error) = ConversionEngine.ParsePayload("""{"name":"Ada","none":null}""", DataFormat.Json);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.Equal("Ada", output["name"]);
        Assert.Null(output["none"]);
    }

    [Fact]
    public void NormalizeConversionRules_ReturnsEmptyForInvalidLegacyRows()
    {
        var json = """{ "rows": "not-a-list" }""";
        using var doc = JsonDocument.Parse(json);

        var rules = ConversionEngine.NormalizeConversionRules(doc.RootElement);

        Assert.Empty(rules.FieldMappings);
        Assert.Empty(rules.ArrayMappings);
    }

    [Fact]
    public void NormalizeConversionRules_ObjectWithInvalidVersionAndRowsReturnsEmpty()
    {
        var json = """{ "version": "bad", "rows": "not-a-list" }""";
        using var doc = JsonDocument.Parse(json);

        var rules = ConversionEngine.NormalizeConversionRules(doc.RootElement);

        Assert.Empty(rules.FieldMappings);
        Assert.Empty(rules.ArrayMappings);
    }

    [Fact]
    public void ApplyConversion_PathWithDollarBracketUsesRoot()
    {
        var input = new List<object?> { "first" };
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "value",
                    Source = new ValueSource { Type = "path", Path = "$[0]" }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Null(output["value"]);
    }

    [Fact]
    public void ApplyConversion_ConditionOperatorDefaultReturnsFalse()
    {
        var input = new Dictionary<string, object?> { ["name"] = "Ada" };
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "match",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "name", Operator = (ConditionOperator)999 },
                        TrueValue = "yes",
                        FalseValue = "no"
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("no", output["match"]);
    }

    [Fact]
    public void ApplyConversion_IncludesConditionWithNonCollectionReturnsFalse()
    {
        var input = new Dictionary<string, object?> { ["value"] = 10 };
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "match",
                    Source = new ValueSource
                    {
                        Type = "condition",
                        Condition = new ConditionRule { Path = "value", Operator = ConditionOperator.Includes, Value = "1" },
                        TrueValue = "yes",
                        FalseValue = "no"
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("no", output["match"]);
    }

    [Fact]
    public void ApplyConversion_TransformDefaultReturnsValue()
    {
        var input = new Dictionary<string, object?> { ["value"] = "Ada" };
        var rules = new ConversionRules
        {
            FieldMappings = new List<FieldRule>
            {
                new()
                {
                    OutputPath = "value",
                    Source = new ValueSource { Type = "transform", Path = "value", Transform = (TransformType)999 }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);
        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);

        Assert.Equal("Ada", output["value"]);
    }

    [Fact]
    public void ParseXml_ThirdDuplicateChildAddsToList()
    {
        var xml = "<root><item>1</item><item>2</item><item>3</item></root>";
        var (value, error) = ConversionEngine.ParsePayload(xml, DataFormat.Xml);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var root = Assert.IsType<Dictionary<string, object?>>(output["root"]);
        var items = Assert.IsType<List<object?>>(root["item"]);
        Assert.Equal(3d, items[2]);
    }

    [Fact]
    public void ParseQueryString_ReusesArrayIndexWhenTypeChanges()
    {
        var (value, error) = ConversionEngine.ParsePayload("items[0]=a&items[0][0]=b", DataFormat.Query);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var items = Assert.IsType<List<object?>>(output["items"]);
        var first = Assert.IsType<List<object?>>(items[0]);
        Assert.Equal("b", first[0]);
    }

    [Fact]
    public void ParseQueryString_ThirdRepeatedKeyAddsToList()
    {
        var (value, error) = ConversionEngine.ParsePayload("tag=a&tag=b&tag=c", DataFormat.Query);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var tags = Assert.IsType<List<object?>>(output["tag"]);
        Assert.Equal("c", tags[2]);
    }

    [Fact]
    public void FormatQueryString_HandlesNullValuesAndNullListItems()
    {
        var input = new Dictionary<string, object?>
        {
            ["empty"] = null,
            ["items"] = new List<object?> { "a", null }
        };

        var output = ConversionEngine.FormatPayload(input, DataFormat.Query, pretty: false);

        Assert.Contains("empty=", output);
        Assert.Contains("items=a", output);
        Assert.Contains("items=", output);
    }

    [Fact]
    public void ParseJson_HandlesTrueFalse()
    {
        var (value, error) = ConversionEngine.ParsePayload("""{"t":true,"f":false}""", DataFormat.Json);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        Assert.Equal(true, output["t"]);
        Assert.Equal(false, output["f"]);
    }

    [Fact]
    public void FormatQueryString_SerializesComplexListItems()
    {
        var value = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["id"] = 1 }
            }
        };

        var output = ConversionEngine.FormatPayload(value, DataFormat.Query, pretty: false);

        Assert.Contains("items=%7B", output);
        Assert.Contains("%22id%22%3A1", output);
    }

    [Fact]
    public void FormatQueryString_ThrowsWhenValueIsNotObject()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ConversionEngine.FormatPayload("not-an-object", DataFormat.Query, pretty: false));
    }

    [Fact]
    public void ParseXml_HandlesAttributesAndText()
    {
        var xml = "<root id=\"1\">Ada</root>";
        var (value, error) = ConversionEngine.ParsePayload(xml, DataFormat.Xml);

        Assert.Null(error);

        var output = Assert.IsType<Dictionary<string, object?>>(value);
        var root = Assert.IsType<Dictionary<string, object?>>(output["root"]);
        Assert.Equal(1d, root["@_id"]);
        Assert.Equal("Ada", root["#text"]);
    }

    [Fact]
    public void FormatXml_ExpandsListValues()
    {
        var input = new Dictionary<string, object?>
        {
            ["item"] = new List<object?> { "a", "b" }
        };

        var xml = ConversionEngine.FormatPayload(input, DataFormat.Xml, pretty: false);

        Assert.Contains("<item>a</item>", xml);
        Assert.Contains("<item>b</item>", xml);
    }

    [Fact]
    public void ApplyConversion_ResolvesRootPathInArrayItem()
    {
        var input = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?> { ["id"] = 7 },
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["name"] = "Ada" }
            }
        };

        var rules = new ConversionRules
        {
            ArrayMappings = new List<ArrayRule>
            {
                new()
                {
                    InputPath = "items",
                    OutputPath = "lines",
                    ItemMappings = new List<FieldRule>
                    {
                        new()
                        {
                            OutputPath = "name",
                            Source = new ValueSource { Type = "path", Path = "name" }
                        },
                        new()
                        {
                            OutputPath = "userId",
                            Source = new ValueSource { Type = "path", Path = "$.user.id" }
                        }
                    }
                }
            }
        };

        var result = ConversionEngine.ApplyConversion(input, rules);

        var output = Assert.IsType<Dictionary<string, object?>>(result.Output);
        var lines = Assert.IsType<List<object?>>(output["lines"]);
        var first = Assert.IsType<Dictionary<string, object?>>(lines[0]);
        Assert.Equal("Ada", first["name"]);
        Assert.Equal(7, first["userId"]);
    }
}
