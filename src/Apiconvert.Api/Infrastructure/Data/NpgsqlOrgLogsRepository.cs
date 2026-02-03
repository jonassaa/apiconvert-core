using Apiconvert.Api.Logs;
using Npgsql;

namespace Apiconvert.Infrastructure.Data;

public sealed class OrgLogsRepository : IOrgLogsRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public OrgLogsRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<LogConverterOption>> GetConvertersAsync(Guid orgId, CancellationToken cancellationToken)
    {
        const string sql = @"
select id, name
from converters
where org_id = @orgId
order by created_at desc;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);

        var results = new List<LogConverterOption>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new LogConverterOption
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<OrgLogEntry>> GetLogsAsync(
        Guid orgId,
        Guid? converterId,
        string? nameQuery,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken cancellationToken)
    {
        var filters = new List<string> { "l.org_id = @orgId" };
        var parameters = new Dictionary<string, object?> { ["orgId"] = orgId };

        if (converterId.HasValue)
        {
            filters.Add("l.converter_id = @converterId");
            parameters["converterId"] = converterId.Value;
        }

        if (!string.IsNullOrWhiteSpace(nameQuery))
        {
            filters.Add("c.name ilike @name");
            parameters["name"] = $"%{nameQuery.Trim()}%";
        }

        if (from.HasValue)
        {
            filters.Add("l.received_at >= @from");
            parameters["from"] = from.Value.UtcDateTime;
        }

        if (to.HasValue)
        {
            filters.Add("l.received_at <= @to");
            parameters["to"] = to.Value.UtcDateTime;
        }

        var sql = $@"
select l.received_at, l.request_id, l.forward_status, l.forward_response_ms, l.path, l.converter_id, c.name
from converter_logs l
left join converters c on c.id = l.converter_id
where {string.Join(" and ", filters)}
order by l.received_at desc
limit @limit;
";
        await using var command = _dataSource.CreateCommand(sql);
        foreach (var entry in parameters)
        {
            command.Parameters.AddWithValue(entry.Key, entry.Value ?? DBNull.Value);
        }
        command.Parameters.AddWithValue("limit", limit);

        var results = new List<OrgLogEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OrgLogEntry
            {
                ReceivedAt = reader.GetFieldValue<DateTimeOffset>(0),
                RequestId = reader.GetGuid(1),
                ForwardStatus = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                ForwardResponseMs = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Path = reader.IsDBNull(4) ? null : reader.GetString(4),
                ConverterId = reader.GetGuid(5),
                ConverterName = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return results;
    }
}
