using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Apiconvert.Core.Rules;

namespace Apiconvert.Core.Converters;

internal static class RulesCacheKey
{
    internal static string Compute(ConversionRules rules)
    {
        var json = JsonSerializer.Serialize(rules, JsonDefaults.Options);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
