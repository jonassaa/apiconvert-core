using Apiconvert.Core.Converters;
using Apiconvert.Core.Rules;
using Xunit;

namespace Apiconvert.Core.Tests;

public sealed class LintRulesCoverageTests
{
    [Fact]
    public void LintRules_DetectsAlwaysFalseBranchWithoutElse()
    {
        var rawRules = """
        {
          "rules": [
            {
              "kind": "branch",
              "expression": "false",
              "then": [
                {
                  "kind": "field",
                  "outputPaths": ["x"],
                  "source": { "type": "constant", "value": "1" }
                }
              ]
            }
          ]
        }
        """;

        var diagnostics = ConversionEngine.LintRules(rawRules);

        Assert.Contains(diagnostics, d => d.Code == "ACV-LINT-004" && d.RulePath == "rules[0]");
    }
}
