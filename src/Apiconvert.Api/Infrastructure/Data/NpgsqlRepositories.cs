using System.Text.Json;
using Apiconvert.Api.Inbound;
using Npgsql;
using NpgsqlTypes;

namespace Apiconvert.Infrastructure.Data;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public OrganizationRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<bool> ExistsAsync(Guid orgId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("select 1 from organizations where id = @orgId limit 1");
        command.Parameters.AddWithValue("orgId", orgId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }
}

public sealed class ConverterRepository : IConverterRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ConverterRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<ConverterConfig?> GetByOrgAndPathAsync(Guid orgId, string inboundPath, CancellationToken cancellationToken)
    {
        const string sql = @"
select
  id,
  org_id,
  enabled,
  forward_url,
  forward_method::text,
  forward_headers_json,
  log_requests_enabled,
  inbound_secret_hash,
  inbound_auth_mode,
  inbound_auth_header_name,
  inbound_auth_username,
  inbound_auth_value_hash,
  log_retention_days,
  log_body_max_bytes,
  log_headers_max_bytes,
  log_redact_sensitive_headers,
  inbound_response_mode
from converters
where org_id = @orgId and inbound_path = @inboundPath
limit 1;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("inboundPath", inboundPath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var forwardHeadersJson = reader.IsDBNull(5) ? "{}" : reader.GetString(5);
        var forwardHeaders = DeserializeDictionary(forwardHeadersJson);

        return new ConverterConfig
        {
            Id = reader.GetGuid(0),
            OrgId = reader.GetGuid(1),
            Enabled = reader.GetBoolean(2),
            ForwardUrl = reader.GetString(3),
            ForwardMethod = reader.IsDBNull(4) ? null : reader.GetString(4),
            ForwardHeaders = forwardHeaders,
            LogRequestsEnabled = reader.GetBoolean(6),
            InboundSecretHash = reader.IsDBNull(7) ? null : reader.GetString(7),
            InboundAuthMode = reader.IsDBNull(8) ? null : reader.GetString(8),
            InboundAuthHeaderName = reader.IsDBNull(9) ? null : reader.GetString(9),
            InboundAuthUsername = reader.IsDBNull(10) ? null : reader.GetString(10),
            InboundAuthValueHash = reader.IsDBNull(11) ? null : reader.GetString(11),
            LogRetentionDays = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            LogBodyMaxBytes = reader.IsDBNull(13) ? null : reader.GetInt32(13),
            LogHeadersMaxBytes = reader.IsDBNull(14) ? null : reader.GetInt32(14),
            LogRedactSensitiveHeaders = reader.IsDBNull(15) ? null : reader.GetBoolean(15),
            InboundResponseMode = reader.IsDBNull(16) ? null : reader.GetString(16)
        };
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

public sealed class ConverterLogRepository : IConverterLogRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ConverterLogRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InsertAsync(ConverterLogEntry entry, CancellationToken cancellationToken)
    {
        const string sql = @"
insert into converter_logs (
  converter_id,
  org_id,
  received_at,
  request_id,
  source_ip,
  method,
  path,
  headers_json,
  query_json,
  body_json,
  transformed_body_json,
  forward_url,
  forward_status,
  forward_response_ms,
  error_text,
  forward_response_headers_json,
  forward_response_body_json,
  forward_response_body_text
) values (
  @converterId,
  @orgId,
  @receivedAt,
  @requestId,
  @sourceIp,
  @method,
  @path,
  @headersJson,
  @queryJson,
  @bodyJson,
  @transformedBodyJson,
  @forwardUrl,
  @forwardStatus,
  @forwardResponseMs,
  @errorText,
  @forwardResponseHeadersJson,
  @forwardResponseBodyJson,
  @forwardResponseBodyText
);
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("converterId", entry.ConverterId);
        command.Parameters.AddWithValue("orgId", entry.OrgId);
        command.Parameters.AddWithValue("receivedAt", entry.ReceivedAt.UtcDateTime);
        command.Parameters.AddWithValue("requestId", entry.RequestId);
        command.Parameters.AddWithValue("sourceIp", (object?)entry.SourceIp ?? DBNull.Value);
        command.Parameters.AddWithValue("method", (object?)entry.Method ?? DBNull.Value);
        command.Parameters.AddWithValue("path", (object?)entry.Path ?? DBNull.Value);
        AddJsonParameter(command, "headersJson", entry.HeadersJson);
        AddJsonParameter(command, "queryJson", entry.QueryJson);
        AddJsonParameter(command, "bodyJson", entry.BodyJson);
        AddJsonParameter(command, "transformedBodyJson", entry.TransformedBodyJson);
        command.Parameters.AddWithValue("forwardUrl", (object?)entry.ForwardUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("forwardStatus", (object?)entry.ForwardStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("forwardResponseMs", (object?)entry.ForwardResponseMs ?? DBNull.Value);
        command.Parameters.AddWithValue("errorText", (object?)entry.ErrorText ?? DBNull.Value);
        AddJsonParameter(command, "forwardResponseHeadersJson", entry.ForwardResponseHeadersJson);
        AddJsonParameter(command, "forwardResponseBodyJson", entry.ForwardResponseBodyJson);
        command.Parameters.AddWithValue("forwardResponseBodyText", (object?)entry.ForwardResponseBodyText ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CleanupAsync(Guid converterId, int retentionDays, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays).UtcDateTime;
        await using var command = _dataSource.CreateCommand(
            "delete from converter_logs where converter_id = @converterId and received_at < @cutoff");
        command.Parameters.AddWithValue("converterId", converterId);
        command.Parameters.AddWithValue("cutoff", cutoff);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static void AddJsonParameter(NpgsqlCommand command, string name, object? value)
    {
        if (value == null)
        {
            command.Parameters.AddWithValue(name, DBNull.Value);
            return;
        }
        var json = JsonSerializer.Serialize(value);
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Jsonb);
        parameter.Value = json;
    }
}
