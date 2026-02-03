using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Apiconvert.Core.Contracts;
using Apiconvert.Core.Rules;

namespace Apiconvert.Infrastructure.Ai;

public sealed class OpenRouterConversionRulesGenerator : IConversionRulesGenerator
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterConversionRulesGenerator(HttpClient httpClient, OpenRouterOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ConversionRules> GenerateAsync(ConversionRulesGenerationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OPENROUTER_API_KEY is not configured.");
        }
        if (string.IsNullOrWhiteSpace(_options.SiteUrl))
        {
            throw new InvalidOperationException("OPENROUTER_SITE_URL is required.");
        }
        if (string.IsNullOrWhiteSpace(_options.AppName))
        {
            throw new InvalidOperationException("OPENROUTER_APP_NAME is required.");
        }

        var systemPrompt = "You are a data-mapping compiler. Infer the most likely mapping from the input sample to the output sample with the fewest rules that reproduce the expected output.\n\n" +
                           "Output ONLY the JSON object that matches the provided schema. No prose, no markdown, no extra keys.\n\n" +
                           "Rules:\n" +
                           "- Prefer direct paths and renames; infer by matching equal values and similar names.\n" +
                           "- Use source {type:\"path\", path:\"...\"} to read values; reuse root via $.path when needed.\n" +
                           "- Map arrays with arrayMappings; itemMappings paths are relative to each array item.\n" +
                           "- Use source {type:\"constant\", value:\"...\"} for constants.\n" +
                           "- Use source {type:\"condition\", condition:{path,operator,value}, trueValue, falseValue} for branching.\n" +
                           "- Allowed transforms: toLowerCase,toUpperCase,number,boolean,concat.\n" +
                           "- concat: \"a, const: , b\" (tokens are paths or const: literals).\n" +
                           "- Omit rules not needed to produce the output sample.\n" +
                           "- Do not invent data not present in the samples.\n" +
                           "- For XML, use $.A.B for elements and @attr for attributes.\n" +
                           "- For query format, inputs/outputs use query strings with dot notation for nesting and repeated keys for arrays.";

        var prompt = $"Create mapping rules to convert INPUT -> OUTPUT.\n\n" +
                     $"InputFormat: {ToFormatString(request.InputFormat)}\n" +
                     $"OutputFormat: {ToFormatString(request.OutputFormat)}\n\n" +
                     $"INPUT:\n{request.InputSample}\n\n" +
                     $"OUTPUT:\n{request.OutputSample}";

        var model = string.IsNullOrWhiteSpace(request.Model) ? _options.Model : request.Model;

        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = prompt }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "conversion_rules",
                    strict = true,
                    schema = JsonSerializer.Deserialize<JsonElement>(Schema)
                }
            }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_options.BaseUrl), "chat/completions"));
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        requestMessage.Headers.TryAddWithoutValidation("HTTP-Referer", _options.SiteUrl);
        requestMessage.Headers.TryAddWithoutValidation("X-Title", _options.AppName);
        requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenRouter request failed: {(int)response.StatusCode} {body}");
        }

        using var document = JsonDocument.Parse(body);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("OpenRouter returned empty content.");
        }

        var rules = JsonSerializer.Deserialize<ConversionRules>(content, JsonOptions);
        if (rules == null)
        {
            throw new InvalidOperationException("Failed to parse conversion rules.");
        }

        return rules;
    }

    private static string ToFormatString(DataFormat format)
    {
        return format switch
        {
            DataFormat.Xml => "xml",
            DataFormat.Query => "query",
            _ => "json"
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private const string Schema = """
{
  "type": "object",
  "additionalProperties": false,
  "required": ["version", "inputFormat", "outputFormat", "fieldMappings", "arrayMappings"],
  "properties": {
    "version": { "type": "integer", "const": 2 },
    "inputFormat": { "type": "string", "enum": ["json", "xml", "query"] },
    "outputFormat": { "type": "string", "enum": ["json", "xml", "query"] },
    "fieldMappings": {
      "type": "array",
      "items": { "$ref": "#/definitions/fieldRule" }
    },
    "arrayMappings": {
      "type": "array",
      "items": { "$ref": "#/definitions/arrayRule" }
    }
  },
  "definitions": {
    "conditionRule": {
      "type": "object",
      "additionalProperties": false,
      "required": ["path", "operator", "value"],
      "properties": {
        "path": { "type": "string" },
        "operator": { "type": "string", "enum": ["exists", "equals", "notEquals", "includes", "gt", "lt"] },
        "value": { "type": "string" }
      }
    },
    "valueSource": {
      "oneOf": [
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["type", "path"],
          "properties": {
            "type": { "const": "path" },
            "path": { "type": "string" }
          }
        },
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["type", "value"],
          "properties": {
            "type": { "const": "constant" },
            "value": { "type": "string" }
          }
        },
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["type", "path", "transform"],
          "properties": {
            "type": { "const": "transform" },
            "path": { "type": "string" },
            "transform": { "type": "string", "enum": ["toLowerCase", "toUpperCase", "number", "boolean", "concat"] }
          }
        },
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["type", "condition", "trueValue", "falseValue"],
          "properties": {
            "type": { "const": "condition" },
            "condition": { "$ref": "#/definitions/conditionRule" },
            "trueValue": { "type": "string" },
            "falseValue": { "type": "string" }
          }
        }
      ]
    },
    "fieldRule": {
      "type": "object",
      "additionalProperties": false,
      "required": ["outputPath", "source", "defaultValue"],
      "properties": {
        "outputPath": { "type": "string" },
        "source": { "$ref": "#/definitions/valueSource" },
        "defaultValue": { "type": "string" }
      }
    },
    "arrayRule": {
      "type": "object",
      "additionalProperties": false,
      "required": ["inputPath", "outputPath", "itemMappings", "coerceSingle"],
      "properties": {
        "inputPath": { "type": "string" },
        "outputPath": { "type": "string" },
        "itemMappings": { "type": "array", "items": { "$ref": "#/definitions/fieldRule" } },
        "coerceSingle": { "type": "boolean" }
      }
    }
  }
}
""";
}
