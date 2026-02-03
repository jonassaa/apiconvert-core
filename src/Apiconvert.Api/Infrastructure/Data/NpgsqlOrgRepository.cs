using Apiconvert.Api.Organizations;
using Npgsql;

namespace Apiconvert.Infrastructure.Data;

public sealed class OrgRepository : IOrgRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public OrgRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<OrgMembershipSummary>> GetMembershipsAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = @"
select m.org_id, m.role::text, o.name, o.slug
from organization_members m
join organizations o on o.id = m.org_id
where m.user_id = @userId
order by m.created_at asc;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("userId", userId);

        var results = new List<OrgMembershipSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OrgMembershipSummary
            {
                OrgId = reader.GetGuid(0),
                Role = reader.GetString(1),
                Name = reader.GetString(2),
                Slug = reader.GetString(3)
            });
        }

        return results;
    }

    public async Task<IReadOnlySet<Guid>> GetFavoriteOrgIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "select org_id from organization_favorites where user_id = @userId");
        command.Parameters.AddWithValue("userId", userId);

        var results = new HashSet<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetGuid(0));
        }

        return results;
    }

    public async Task<OrgSummary> CreateOrgAsync(Guid userId, string name, CancellationToken cancellationToken)
    {
        var orgId = Guid.NewGuid();
        var slug = orgId.ToString();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var orgCommand = new NpgsqlCommand(
            "insert into organizations (id, name, slug, created_by) values (@id, @name, @slug, @createdBy)",
            connection,
            transaction))
        {
            orgCommand.Parameters.AddWithValue("id", orgId);
            orgCommand.Parameters.AddWithValue("name", name);
            orgCommand.Parameters.AddWithValue("slug", slug);
            orgCommand.Parameters.AddWithValue("createdBy", userId);
            await orgCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var memberCommand = new NpgsqlCommand(
            "insert into organization_members (org_id, user_id, role) values (@orgId, @userId, 'owner')",
            connection,
            transaction))
        {
            memberCommand.Parameters.AddWithValue("orgId", orgId);
            memberCommand.Parameters.AddWithValue("userId", userId);
            await memberCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new OrgSummary
        {
            Id = orgId,
            Name = name,
            Slug = slug
        };
    }

    public async Task AddFavoriteAsync(Guid userId, Guid orgId, CancellationToken cancellationToken)
    {
        const string sql = @"
insert into organization_favorites (org_id, user_id)
values (@orgId, @userId)
on conflict (org_id, user_id) do nothing;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RemoveFavoriteAsync(Guid userId, Guid orgId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "delete from organization_favorites where org_id = @orgId and user_id = @userId");
        command.Parameters.AddWithValue("orgId", orgId);
        command.Parameters.AddWithValue("userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
