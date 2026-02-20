using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

var casesRoot = args.Length > 0 ? args[0] : LocateCasesRoot();
if (!Directory.Exists(casesRoot))
{
    Console.Error.WriteLine($"Cases root does not exist: {casesRoot}");
    Environment.Exit(1);
    return;
}

var caseDirectories = Directory.GetDirectories(casesRoot)
    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    .ToList();

var results = new List<CaseResult>();
foreach (var caseDirectory in caseDirectories)
{
    results.Add(RunCase(caseDirectory));
}

var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
{
    WriteIndented = true
});
Console.WriteLine(json);

static CaseResult RunCase(string caseDirectory)
{
    var caseName = Path.GetFileName(caseDirectory);
    try
    {
        var rulesPath = Path.Combine(caseDirectory, "rules.json");
        var inputPath = GetSingleFile(caseDirectory, "input");
        var outputPath = GetSingleFile(caseDirectory, "output");

        var rulesJson = File.ReadAllText(rulesPath);
        var inputText = File.ReadAllText(inputPath);

        var rules = ConversionEngine.NormalizeConversionRules(rulesJson);
        var inputFormat = ParseFormatFromExtension(Path.GetExtension(inputPath));
        var outputFormat = ParseFormatFromExtension(Path.GetExtension(outputPath));

        object? inputValue = inputFormat.HasValue
            ? ParsePayloadOrThrow(inputText, inputFormat.Value)
            : inputText;

        var result = ConversionEngine.ApplyConversion(inputValue, rules);
        var outputText = outputFormat.HasValue
            ? ConversionEngine.FormatPayload(result.Output, outputFormat.Value, pretty: outputFormat.Value is DataFormat.Xml)
            : Convert.ToString(result.Output) ?? string.Empty;

        return new CaseResult
        {
            CaseName = caseName,
            OutputText = NormalizeOutput(outputText, outputFormat),
            Errors = result.Errors,
            Warnings = result.Warnings
        };
    }
    catch (Exception ex)
    {
        return new CaseResult
        {
            CaseName = caseName,
            OutputText = string.Empty,
            Errors = [$"runner failure: {ex.Message}"],
            Warnings = []
        };
    }
}

static object? ParsePayloadOrThrow(string text, DataFormat format)
{
    var (value, error) = ConversionEngine.ParsePayload(text, format);
    if (error is null)
    {
        return value;
    }

    throw new InvalidOperationException(error);
}

static string NormalizeOutput(string text, DataFormat? format)
{
    var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    if (format is DataFormat.Json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(normalized) ? "{}" : normalized);
        return JsonSerializer.Serialize(document.RootElement);
    }

    if (format is DataFormat.Xml)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var document = XDocument.Parse(normalized, LoadOptions.None);
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            NewLineHandling = NewLineHandling.Replace,
            NewLineChars = "\n"
        };

        using var writer = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(writer, settings))
        {
            document.Save(xmlWriter);
        }

        return writer.ToString().Trim();
    }

    return normalized;
}

static string GetSingleFile(string caseDirectory, string baseName)
{
    var matches = Directory.GetFiles(caseDirectory, $"{baseName}.*");
    if (matches.Length == 0)
    {
        throw new InvalidOperationException($"Case '{Path.GetFileName(caseDirectory)}' must contain {baseName}.<ext> file.");
    }

    if (matches.Length > 1)
    {
        throw new InvalidOperationException($"Case '{Path.GetFileName(caseDirectory)}' must contain exactly one {baseName}.<ext> file.");
    }

    return matches[0];
}

static DataFormat? ParseFormatFromExtension(string extension)
{
    return extension.ToLowerInvariant() switch
    {
        ".json" => DataFormat.Json,
        ".xml" => DataFormat.Xml,
        ".txt" => DataFormat.Query,
        _ => null
    };
}

static string LocateCasesRoot()
{
    var baseDirectory = AppContext.BaseDirectory;
    var candidate = Path.Combine(baseDirectory, "cases");
    if (Directory.Exists(candidate))
    {
        return candidate;
    }

    var current = new DirectoryInfo(baseDirectory);
    while (current is not null)
    {
        var testsRoot = Path.Combine(current.FullName, "tests", "cases");
        if (Directory.Exists(testsRoot))
        {
            return testsRoot;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not locate tests/cases.");
}

file sealed class CaseResult
{
    public string CaseName { get; init; } = string.Empty;

    public string OutputText { get; init; } = string.Empty;

    public List<string> Errors { get; init; } = [];

    public List<string> Warnings { get; init; } = [];
}
