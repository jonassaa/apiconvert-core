using Apiconvert.Api.Organizations;
using Npgsql;

namespace Apiconvert.Infrastructure.Data;

public sealed class OrgSettingsRepository : IOrgSettingsRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public OrgSettingsRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<OrgSummary?> GetOrgAsync(Guid orgId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "select id, name, slug from organizations where id = @orgId limit 1");
        command.Parameters.AddWithValue("orgId", orgId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new OrgSummary
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1),
            Slug = reader.GetString(2)
        };
    }

    public async Task<IReadOnlyList<OrgMember>> GetMembersAsync(Guid orgId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "select user_id, role::text from organization_members where org_id = @orgId");
        command.Parameters.AddWithValue("orgId", orgId);

        var results = new List<OrgMember>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OrgMember
            {
                UserId = reader.GetGuid(0),
                Role = reader.GetString(1)
            });
        }

        return results;
    }

    public async Task<OrgMember?> GetMemberAsync(Guid orgId, Guid memberId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "select user_id, role::text from organization_members where org_id = @orgId and user_id = @memberId limit 1");
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("memberId", memberId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new OrgMember
        {
            UserId = reader.GetGuid(0),
            Role = reader.GetString(1)
        };
    }

    public async Task<int> GetOwnerCountAsync(Guid orgId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "select count(*) from organization_members where org_id = @orgId and role = 'owner'");
        command.Parameters.AddWithValue("orgId", orgId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is long count ? (int)count : 0;
    }

    public async Task<IReadOnlyList<OrgInvite>> GetInvitesAsync(Guid orgId, CancellationToken cancellationToken)
    {
        const string sql = @"
select id, org_id, email, role::text, token, expires_at, accepted_at, created_at
from invites
where org_id = @orgId
order by created_at desc;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);

        var results = new List<OrgInvite>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OrgInvite
            {
                Id = reader.GetGuid(0),
                OrgId = reader.GetGuid(1),
                Email = reader.GetString(2),
                Role = reader.GetString(3),
                Token = reader.GetString(4),
                ExpiresAt = reader.GetFieldValue<DateTimeOffset>(5),
                AcceptedAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(7)
            });
        }

        return results;
    }

    public async Task<OrgInvite> CreateInviteAsync(
        Guid orgId,
        Guid createdBy,
        string email,
        string role,
        string token,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        const string sql = @"
insert into invites (org_id, email, role, token, expires_at, created_by)
values (@orgId, @email, @role, @token, @expiresAt, @createdBy)
returning id, org_id, email, role::text, token, expires_at, accepted_at, created_at;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("expiresAt", expiresAt.UtcDateTime);
        command.Parameters.AddWithValue("createdBy", createdBy);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return new OrgInvite
        {
            Id = reader.GetGuid(0),
            OrgId = reader.GetGuid(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3),
            Token = reader.GetString(4),
            ExpiresAt = reader.GetFieldValue<DateTimeOffset>(5),
            AcceptedAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(7)
        };
    }

    public async Task<bool> UpdateOrgNameAsync(Guid orgId, string name, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "update organizations set name = @name where id = @orgId");
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("name", name);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteOrgAsync(Guid orgId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "delete from organizations where id = @orgId");
        command.Parameters.AddWithValue("orgId", orgId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid orgId, Guid memberId, string role, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "update organization_members set role = @role where org_id = @orgId and user_id = @memberId");
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("memberId", memberId);
        command.Parameters.AddWithValue("role", role);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> RemoveMemberAsync(Guid orgId, Guid memberId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "delete from organization_members where org_id = @orgId and user_id = @memberId");
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("memberId", memberId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }
}
