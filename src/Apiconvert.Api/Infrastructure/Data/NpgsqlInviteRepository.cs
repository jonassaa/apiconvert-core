using Apiconvert.Api.Organizations;
using Npgsql;

namespace Apiconvert.Infrastructure.Data;

public sealed class InviteRepository : IInviteRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public InviteRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<InviteDetails?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        const string sql = @"
select i.id,
       i.org_id,
       i.email,
       i.role::text,
       i.token,
       i.expires_at,
       i.accepted_at,
       o.name
from invites i
join organizations o on o.id = i.org_id
where i.token = @token
limit 1;
";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("token", token);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new InviteDetails
        {
            Id = reader.GetGuid(0),
            OrgId = reader.GetGuid(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3),
            Token = reader.GetString(4),
            ExpiresAt = reader.GetFieldValue<DateTimeOffset>(5),
            AcceptedAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            OrgName = reader.GetString(7)
        };
    }

    public async Task<bool> AcceptInviteAsync(
        Guid inviteId,
        Guid orgId,
        Guid userId,
        string role,
        DateTimeOffset acceptedAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var memberCommand = new NpgsqlCommand(@"
insert into organization_members (org_id, user_id, role)
values (@orgId, @userId, @role)
on conflict (org_id, user_id) do update set role = excluded.role;
", connection, transaction))
        {
            memberCommand.Parameters.AddWithValue("orgId", orgId);
            memberCommand.Parameters.AddWithValue("userId", userId);
            memberCommand.Parameters.AddWithValue("role", role);
            await memberCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        int updated;
        await using (var inviteCommand = new NpgsqlCommand(@"
update invites
set accepted_at = @acceptedAt
where id = @inviteId;
", connection, transaction))
        {
            inviteCommand.Parameters.AddWithValue("acceptedAt", acceptedAt.UtcDateTime);
            inviteCommand.Parameters.AddWithValue("inviteId", inviteId);
            updated = await inviteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return updated > 0;
    }
}
