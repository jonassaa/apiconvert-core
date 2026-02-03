using System.Net.Http.Headers;
using System.Text.Json;
using Apiconvert.Api.Organizations;

namespace Apiconvert.Infrastructure.Auth;

public sealed class SupabaseUserDirectory : IUserDirectory
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseAdminOptions _options;

    public SupabaseUserDirectory(HttpClient httpClient, SupabaseAdminOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<IReadOnlyList<UserProfile>> GetProfilesAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return Array.Empty<UserProfile>();
        }

        if (string.IsNullOrWhiteSpace(_options.Url) || string.IsNullOrWhiteSpace(_options.ServiceRoleKey))
        {
            return userIds.Select(id => new UserProfile { UserId = id }).ToList();
        }

        var results = new List<UserProfile>(userIds.Count);
        foreach (var userId in userIds)
        {
            var profile = await FetchProfileAsync(userId, cancellationToken);
            results.Add(profile);
        }

        return results;
    }

    private async Task<UserProfile> FetchProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(new Uri(_options.Url.TrimEnd('/') + "/"), $"auth/v1/admin/users/{userId}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        request.Headers.TryAddWithoutValidation("apikey", _options.ServiceRoleKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new UserProfile { UserId = userId };
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new UserProfile { UserId = userId };
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
        var name = ResolveName(root);

        return new UserProfile
        {
            UserId = userId,
            Email = email,
            Name = name
        };
    }

    private static string? ResolveName(JsonElement root)
    {
        if (!root.TryGetProperty("user_metadata", out var metadata))
        {
            return null;
        }

        if (metadata.TryGetProperty("full_name", out var fullName) && fullName.ValueKind == JsonValueKind.String)
        {
            return fullName.GetString();
        }
        if (metadata.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
        {
            return name.GetString();
        }
        if (metadata.TryGetProperty("user_name", out var userName) && userName.ValueKind == JsonValueKind.String)
        {
            return userName.GetString();
        }
        if (metadata.TryGetProperty("preferred_username", out var preferred) && preferred.ValueKind == JsonValueKind.String)
        {
            return preferred.GetString();
        }

        return null;
    }
}
