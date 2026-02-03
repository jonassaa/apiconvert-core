using System.Text.Json;
using Apiconvert.Api.Converters;
using Npgsql;

namespace Apiconvert.Infrastructure.Data;

public sealed class ConverterQueryRepository : IConverterQueryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ConverterQueryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<ConverterSummary>> ListAsync(
        Guid orgId,
        string? search,
        bool? enabled,
        bool? logging,
        CancellationToken cancellationToken)
    {
        var filters = new List<string> { "org_id = @orgId" };
        var parameters = new Dictionary<string, object?> { ["orgId"] = orgId };

        if (!string.IsNullOrWhiteSpace(search))
        {
            filters.Add("(name ilike @search or forward_url ilike @search)");
            parameters["search"] = $"%{search.Trim()}%";
        }
        if (enabled.HasValue)
        {
            filters.Add("enabled = @enabled");
            parameters["enabled"] = enabled.Value;
        }
        if (logging.HasValue)
        {
            filters.Add("log_requests_enabled = @logging");
            parameters["logging"] = logging.Value;
        }

        var sql = $@"
select id, name, enabled, log_requests_enabled, forward_url
from converters
where {string.Join(" and ", filters)}
order by created_at desc;
";
        await using var command = _dataSource.CreateCommand(sql);
        foreach (var entry in parameters)
        {
            command.Parameters.AddWithValue(entry.Key, entry.Value ?? DBNull.Value);
        }

        var results = new List<ConverterSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ConverterSummary
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                Enabled = reader.GetBoolean(2),
                LogRequestsEnabled = reader.GetBoolean(3),
                ForwardUrl = reader.GetString(4)
            });
        }

        return results;
    }

    public async Task<ConverterDetail?> GetByNameAsync(Guid orgId, string normalizedName, CancellationToken cancellationToken)
    {
        const string sql = @"
select
  id,
  name,
  inbound_path,
  enabled,
  forward_url,
  forward_method::text,
  forward_headers_json,
  log_requests_enabled,
  inbound_secret_last4,
  inbound_auth_mode,
  inbound_auth_header_name,
  inbound_auth_username,
  inbound_auth_value_hash,
  inbound_auth_value_last4,
  inbound_secret_hash,
  log_retention_days,
  log_body_max_bytes,
  log_headers_max_bytes,
  log_redact_sensitive_headers,
  inbound_response_mode
from converters
where org_id = @orgId and lower(name) = @name
limit 1;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("name", normalizedName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var forwardHeadersJson = reader.IsDBNull(6) ? "{}" : reader.GetString(6);
        var forwardHeaders = DeserializeDictionary(forwardHeadersJson);

        return new ConverterDetail
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1),
            InboundPath = reader.GetString(2),
            Enabled = reader.GetBoolean(3),
            ForwardUrl = reader.GetString(4),
            ForwardMethod = reader.IsDBNull(5) ? null : reader.GetString(5),
            ForwardHeaders = forwardHeaders,
            LogRequestsEnabled = reader.GetBoolean(7),
            InboundSecretLast4 = reader.IsDBNull(8) ? null : reader.GetString(8),
            InboundAuthMode = reader.IsDBNull(9) ? null : reader.GetString(9),
            InboundAuthHeaderName = reader.IsDBNull(10) ? null : reader.GetString(10),
            InboundAuthUsername = reader.IsDBNull(11) ? null : reader.GetString(11),
            InboundAuthValueHash = reader.IsDBNull(12) ? null : reader.GetString(12),
            InboundAuthValueLast4 = reader.IsDBNull(13) ? null : reader.GetString(13),
            InboundSecretHash = reader.IsDBNull(14) ? null : reader.GetString(14),
            LogRetentionDays = reader.IsDBNull(15) ? null : reader.GetInt32(15),
            LogBodyMaxBytes = reader.IsDBNull(16) ? null : reader.GetInt32(16),
            LogHeadersMaxBytes = reader.IsDBNull(17) ? null : reader.GetInt32(17),
            LogRedactSensitiveHeaders = reader.IsDBNull(18) ? null : reader.GetBoolean(18),
            InboundResponseMode = reader.IsDBNull(19) ? null : reader.GetString(19)
        };
    }

    public async Task<ConverterDetail?> GetByIdAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
    {
        const string sql = @"
select
  id,
  name,
  inbound_path,
  enabled,
  forward_url,
  forward_method::text,
  forward_headers_json,
  log_requests_enabled,
  inbound_secret_last4,
  inbound_auth_mode,
  inbound_auth_header_name,
  inbound_auth_username,
  inbound_auth_value_hash,
  inbound_auth_value_last4,
  inbound_secret_hash,
  log_retention_days,
  log_body_max_bytes,
  log_headers_max_bytes,
  log_redact_sensitive_headers,
  inbound_response_mode
from converters
where org_id = @orgId and id = @converterId
limit 1;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("converterId", converterId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var forwardHeadersJson = reader.IsDBNull(6) ? "{}" : reader.GetString(6);
        var forwardHeaders = DeserializeDictionary(forwardHeadersJson);

        return new ConverterDetail
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1),
            InboundPath = reader.GetString(2),
            Enabled = reader.GetBoolean(3),
            ForwardUrl = reader.GetString(4),
            ForwardMethod = reader.IsDBNull(5) ? null : reader.GetString(5),
            ForwardHeaders = forwardHeaders,
            LogRequestsEnabled = reader.GetBoolean(7),
            InboundSecretLast4 = reader.IsDBNull(8) ? null : reader.GetString(8),
            InboundAuthMode = reader.IsDBNull(9) ? null : reader.GetString(9),
            InboundAuthHeaderName = reader.IsDBNull(10) ? null : reader.GetString(10),
            InboundAuthUsername = reader.IsDBNull(11) ? null : reader.GetString(11),
            InboundAuthValueHash = reader.IsDBNull(12) ? null : reader.GetString(12),
            InboundAuthValueLast4 = reader.IsDBNull(13) ? null : reader.GetString(13),
            InboundSecretHash = reader.IsDBNull(14) ? null : reader.GetString(14),
            LogRetentionDays = reader.IsDBNull(15) ? null : reader.GetInt32(15),
            LogBodyMaxBytes = reader.IsDBNull(16) ? null : reader.GetInt32(16),
            LogHeadersMaxBytes = reader.IsDBNull(17) ? null : reader.GetInt32(17),
            LogRedactSensitiveHeaders = reader.IsDBNull(18) ? null : reader.GetBoolean(18),
            InboundResponseMode = reader.IsDBNull(19) ? null : reader.GetString(19)
        };
    }

    public async Task<bool> ExistsByNameAsync(Guid orgId, string normalizedName, Guid? excludeId, CancellationToken cancellationToken)
    {
        var sql = "select 1 from converters where org_id = @orgId and lower(name) = @name";
        if (excludeId.HasValue)
        {
            sql += " and id <> @excludeId";
        }
        sql += " limit 1;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("name", normalizedName);
        if (excludeId.HasValue)
        {
            command.Parameters.AddWithValue("excludeId", excludeId.Value);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }

    public async Task<bool> ExistsByInboundPathAsync(Guid orgId, string inboundPath, Guid? excludeId, CancellationToken cancellationToken)
    {
        var sql = "select 1 from converters where org_id = @orgId and inbound_path = @path";
        if (excludeId.HasValue)
        {
            sql += " and id <> @excludeId";
        }
        sql += " limit 1;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("path", inboundPath);
        if (excludeId.HasValue)
        {
            command.Parameters.AddWithValue("excludeId", excludeId.Value);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }

    private static Dictionary<string, string> DeserializeDictionary(string json)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return parsed ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
