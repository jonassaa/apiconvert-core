using Apiconvert.Api.Dashboard;
using Apiconvert.Api.Organizations;
using Npgsql;

namespace Apiconvert.Infrastructure.Data;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public DashboardRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(Guid orgId, CancellationToken cancellationToken)
    {
        const string sql = @"
select
  (select count(*) from converter_logs where org_id = @orgId and received_at >= now() - interval '24 hours') as requests_24h,
  (select count(*) from converter_logs where org_id = @orgId and received_at >= now() - interval '7 days') as requests_7d,
  (select count(*) from converter_logs where org_id = @orgId and received_at >= now() - interval '7 days' and forward_status between 200 and 299) as success_7d,
  (select coalesce(avg(forward_response_ms), 0) from converter_logs where org_id = @orgId and received_at >= now() - interval '7 days') as avg_response_ms;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new DashboardMetrics();
        }

        return new DashboardMetrics
        {
            Requests24h = reader.GetInt64(0),
            Requests7d = reader.GetInt64(1),
            Success7d = reader.GetInt64(2),
            AvgResponseMs = reader.GetDecimal(3)
        };
    }

    public async Task<IReadOnlyList<DashboardRecentConverter>> GetRecentConvertersAsync(Guid orgId, int limit, CancellationToken cancellationToken)
    {
        const string sql = @"
select id, name, created_at
from converters
where org_id = @orgId
order by created_at desc
limit @limit;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("limit", limit);

        var results = new List<DashboardRecentConverter>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DashboardRecentConverter
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(2)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<DashboardRecentMember>> GetRecentMembersAsync(Guid orgId, int limit, CancellationToken cancellationToken)
    {
        const string sql = @"
select id, role::text, created_at
from organization_members
where org_id = @orgId
order by created_at desc
limit @limit;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("limit", limit);

        var results = new List<DashboardRecentMember>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DashboardRecentMember
            {
                Id = reader.GetGuid(0),
                Role = reader.GetString(1),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(2)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<DashboardRecentInvite>> GetRecentInvitesAsync(Guid orgId, int limit, CancellationToken cancellationToken)
    {
        const string sql = @"
select id, email, created_at, accepted_at
from invites
where org_id = @orgId
order by created_at desc
limit @limit;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("limit", limit);

        var results = new List<DashboardRecentInvite>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DashboardRecentInvite
            {
                Id = reader.GetGuid(0),
                Email = reader.GetString(1),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(2),
                AcceptedAt = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<DashboardRecentLog>> GetRecentLogsAsync(Guid orgId, int limit, CancellationToken cancellationToken)
    {
        const string sql = @"
select l.received_at, l.request_id, l.forward_status, l.forward_response_ms, c.name
from converter_logs l
left join converters c on c.id = l.converter_id
where l.org_id = @orgId
order by l.received_at desc
limit @limit;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("limit", limit);

        var results = new List<DashboardRecentLog>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DashboardRecentLog
            {
                ReceivedAt = reader.GetFieldValue<DateTimeOffset>(0),
                RequestId = reader.GetGuid(1),
                ForwardStatus = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                ForwardResponseMs = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                ConverterName = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return results;
    }
}
