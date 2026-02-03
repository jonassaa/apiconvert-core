using Apiconvert.Api.Admin;
using Npgsql;
using NpgsqlTypes;

namespace Apiconvert.Infrastructure.Data;

public sealed class ConverterAdminRepository : IConverterAdminRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ConverterAdminRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Guid> CreateAsync(AdminConverterCreateRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"
insert into converters (
  org_id,
  name,
  inbound_path,
  forward_url,
  forward_method,
  forward_headers_json,
  enabled,
  log_requests_enabled,
  inbound_auth_mode,
  inbound_auth_header_name,
  inbound_auth_username,
  inbound_auth_value_hash,
  inbound_auth_value_last4,
  inbound_secret_hash,
  inbound_secret_last4,
  log_retention_days,
  log_body_max_bytes,
  log_headers_max_bytes,
  log_redact_sensitive_headers,
  inbound_response_mode
) values (
  @orgId,
  @name,
  @inboundPath,
  @forwardUrl,
  @forwardMethod::forward_method,
  @forwardHeaders,
  @enabled,
  @logRequestsEnabled,
  @inboundAuthMode,
  @inboundAuthHeaderName,
  @inboundAuthUsername,
  @inboundAuthValueHash,
  @inboundAuthValueLast4,
  @inboundSecretHash,
  @inboundSecretLast4,
  @logRetentionDays,
  @logBodyMaxBytes,
  @logHeadersMaxBytes,
  @logRedactSensitiveHeaders,
  @inboundResponseMode
) returning id;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", request.OrgId);
        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.AddWithValue("inboundPath", request.InboundPath);
        command.Parameters.AddWithValue("forwardUrl", request.ForwardUrl);
        command.Parameters.AddWithValue("forwardMethod", (object?)request.ForwardMethod ?? DBNull.Value);
        Apiconvert.Infrastructure.Data.ConverterLogRepository.AddJsonParameter(command, "forwardHeaders", request.ForwardHeaders);
        command.Parameters.AddWithValue("enabled", request.Enabled);
        command.Parameters.AddWithValue("logRequestsEnabled", request.LogRequestsEnabled);
        command.Parameters.AddWithValue("inboundAuthMode", (object?)request.InboundAuthMode ?? DBNull.Value);
        command.Parameters.AddWithValue("inboundAuthHeaderName", (object?)request.InboundAuthHeaderName ?? DBNull.Value);
        command.Parameters.AddWithValue("inboundAuthUsername", (object?)request.InboundAuthUsername ?? DBNull.Value);
        command.Parameters.AddWithValue("inboundAuthValueHash", (object?)request.InboundAuthValueHash ?? DBNull.Value);
        command.Parameters.AddWithValue("inboundAuthValueLast4", (object?)request.InboundAuthValueLast4 ?? DBNull.Value);
        command.Parameters.AddWithValue("inboundSecretHash", (object?)request.InboundSecretHash ?? DBNull.Value);
        command.Parameters.AddWithValue("inboundSecretLast4", (object?)request.InboundSecretLast4 ?? DBNull.Value);
        command.Parameters.AddWithValue("logRetentionDays", (object?)request.LogRetentionDays ?? DBNull.Value);
        command.Parameters.AddWithValue("logBodyMaxBytes", (object?)request.LogBodyMaxBytes ?? DBNull.Value);
        command.Parameters.AddWithValue("logHeadersMaxBytes", (object?)request.LogHeadersMaxBytes ?? DBNull.Value);
        command.Parameters.AddWithValue("logRedactSensitiveHeaders", (object?)request.LogRedactSensitiveHeaders ?? DBNull.Value);
        command.Parameters.AddWithValue("inboundResponseMode", (object?)request.InboundResponseMode ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : Guid.Empty;
    }

    public async Task<bool> UpdateAsync(Guid orgId, Guid converterId, Dictionary<string, object?> updates, CancellationToken cancellationToken)
    {
        if (updates.Count == 0)
        {
            return true;
        }

        var setClauses = new List<string>();
        await using var command = _dataSource.CreateCommand();
        var index = 0;
        foreach (var entry in updates)
        {
            var paramName = $"p{index}";
            if (entry.Key == "forward_method")
            {
                setClauses.Add($"{entry.Key} = @{paramName}::forward_method");
            }
            else
            {
                setClauses.Add($"{entry.Key} = @{paramName}");
            }
            if (entry.Value is Dictionary<string, string> headers)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(headers);
                var param = command.Parameters.Add(paramName, NpgsqlDbType.Jsonb);
                param.Value = json;
            }
            else if (entry.Value is Dictionary<string, object?> objHeaders)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(objHeaders);
                var param = command.Parameters.Add(paramName, NpgsqlDbType.Jsonb);
                param.Value = json;
            }
            else if (entry.Value == null)
            {
                command.Parameters.AddWithValue(paramName, DBNull.Value);
            }
            else
            {
                command.Parameters.AddWithValue(paramName, entry.Value);
            }
            index++;
        }

        command.CommandText = $"update converters set {string.Join(", ", setClauses)} where id = @converterId and org_id = @orgId";
        command.Parameters.AddWithValue("converterId", converterId);
        command.Parameters.AddWithValue("orgId", orgId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "delete from converters where id = @converterId and org_id = @orgId");
        command.Parameters.AddWithValue("converterId", converterId);
        command.Parameters.AddWithValue("orgId", orgId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "select 1 from converters where id = @converterId and org_id = @orgId limit 1");
        command.Parameters.AddWithValue("converterId", converterId);
        command.Parameters.AddWithValue("orgId", orgId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }
}

public sealed class OrgMembershipRepository : IOrgMembershipRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public OrgMembershipRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<string?> GetRoleAsync(Guid orgId, Guid userId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "select role::text from organization_members where org_id = @orgId and user_id = @userId limit 1");
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }
}

public sealed class ConverterLogQueryRepository : IConverterLogQueryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ConverterLogQueryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<ConverterLogSummary>> GetRecentAsync(Guid converterId, int limit, CancellationToken cancellationToken)
    {
        const string sql = @"
select received_at, forward_status, forward_response_ms, request_id
from converter_logs
where converter_id = @converterId
order by received_at desc
limit @limit;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("converterId", converterId);
        command.Parameters.AddWithValue("limit", limit);

        var results = new List<ConverterLogSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ConverterLogSummary
            {
                ReceivedAt = reader.GetFieldValue<DateTime>(0),
                ForwardStatus = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                ForwardResponseMs = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                RequestId = reader.GetGuid(3)
            });
        }

        return results;
    }
}
