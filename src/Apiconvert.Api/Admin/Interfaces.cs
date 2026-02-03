namespace Apiconvert.Api.Admin;

public interface IOrgMembershipRepository
{
    Task<string?> GetRoleAsync(Guid orgId, Guid userId, CancellationToken cancellationToken);
}

public interface IConverterAdminRepository
{
    Task<Guid> CreateAsync(AdminConverterCreateRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Guid orgId, Guid converterId, Dictionary<string, object?> updates, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken);
}

public interface IConverterLogQueryRepository
{
    Task<IReadOnlyList<ConverterLogSummary>> GetRecentAsync(Guid converterId, int limit, CancellationToken cancellationToken);
}
