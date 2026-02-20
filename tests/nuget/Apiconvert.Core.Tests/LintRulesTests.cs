using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Tests;

public sealed class LintRulesTests
{
    [Fact]
    public void LintRules_InvalidJson_ReturnsErrorDiagnostic()
    {
        var diagnostics = ConversionEngine.LintRules("{ not-valid-json }");

        Assert.Contains(diagnostics, d => d.Code == "ACV-LINT-001" && d.Severity == RuleLintSeverity.Error);
    }

    [Fact]
    public void LintRules_DetectsRiskyPatterns_WithFixHints()
    {
        const string rawRules = """
        {
          "inputFormat": "json",
          "outputFormat": "json",
          "rules": [
            {
              "kind": "field",
              "outputPaths": ["meta.name"],
              "source": { "type": "path", "path": "name" }
            },
            {
              "kind": "field",
              "outputPaths": ["meta.name"],
              "source": { "type": "path", "path": "nickname" }
            },
            {
              "kind": "branch",
              "expression": "true",
              "then": [
                {
                  "kind": "field",
                  "outputPaths": ["meta.flag"],
                  "source": { "type": "constant", "value": "Y" }
                }
              ],
              "else": [
                {
                  "kind": "field",
                  "outputPaths": ["meta.flag"],
                  "source": { "type": "constant", "value": "N" }
                }
              ]
            }
          ]
        }
        """;

        var diagnostics = ConversionEngine.LintRules(rawRules);

        Assert.Contains(diagnostics, d => d.Code == "ACV-LINT-002" && d.RulePath == "rules[0]");
        Assert.Contains(diagnostics, d => d.Code == "ACV-LINT-005" && d.RulePath == "rules[1]");
        Assert.Contains(diagnostics, d => d.Code == "ACV-LINT-003" && d.RulePath == "rules[2]");
        Assert.All(diagnostics, d => Assert.False(string.IsNullOrWhiteSpace(d.Suggestion)));
    }
}
