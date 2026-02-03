using Apiconvert.Api.Inbound;

namespace Apiconvert.Api.Admin;

public sealed class ConverterAdminService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrgMembershipRepository _membershipRepository;
    private readonly IConverterAdminRepository _converterRepository;
    private readonly IMappingRepository _mappingRepository;
    private readonly IConverterLogQueryRepository _logRepository;

    public ConverterAdminService(
        IOrganizationRepository organizationRepository,
        IOrgMembershipRepository membershipRepository,
        IConverterAdminRepository converterRepository,
        IMappingRepository mappingRepository,
        IConverterLogQueryRepository logRepository)
    {
        _organizationRepository = organizationRepository;
        _membershipRepository = membershipRepository;
        _converterRepository = converterRepository;
        _mappingRepository = mappingRepository;
        _logRepository = logRepository;
    }

    public async Task<ServiceResult<Guid>> CreateConverterAsync(
        Guid userId,
        AdminConverterCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _organizationRepository.ExistsAsync(request.OrgId, cancellationToken))
        {
            return ServiceResult<Guid>.Fail("Organization not found", "org_not_found");
        }

        if (!await IsAdminAsync(request.OrgId, userId, cancellationToken))
        {
            return ServiceResult<Guid>.Fail("Only admins can create converters", "forbidden");
        }

        var converterId = await _converterRepository.CreateAsync(request, cancellationToken);
        return ServiceResult<Guid>.Success(converterId);
    }

    public async Task<ServiceResult<bool>> UpdateConverterAsync(
        Guid userId,
        Guid orgId,
        Guid converterId,
        Dictionary<string, object?> updates,
        CancellationToken cancellationToken)
    {
        if (!await _organizationRepository.ExistsAsync(orgId, cancellationToken))
        {
            return ServiceResult<bool>.Fail("Organization not found", "org_not_found");
        }

        if (!await IsAdminAsync(orgId, userId, cancellationToken))
        {
            return ServiceResult<bool>.Fail("Only admins can update converters", "forbidden");
        }

        if (!await _converterRepository.ExistsAsync(orgId, converterId, cancellationToken))
        {
            return ServiceResult<bool>.Fail("Converter not found", "not_found");
        }

        if (updates.Count == 0)
        {
            return ServiceResult<bool>.Success(true);
        }

        var updated = await _converterRepository.UpdateAsync(orgId, converterId, updates, cancellationToken);
        return updated
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail("Converter not found", "not_found");
    }

    public async Task<ServiceResult<bool>> DeleteConverterAsync(
        Guid userId,
        Guid orgId,
        Guid converterId,
        CancellationToken cancellationToken)
    {
        if (!await _organizationRepository.ExistsAsync(orgId, cancellationToken))
        {
            return ServiceResult<bool>.Fail("Organization not found", "org_not_found");
        }

        if (!await IsAdminAsync(orgId, userId, cancellationToken))
        {
            return ServiceResult<bool>.Fail("Only admins can delete converters", "forbidden");
        }

        var deleted = await _converterRepository.DeleteAsync(orgId, converterId, cancellationToken);
        return deleted
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail("Converter not found", "not_found");
    }

    public async Task<ServiceResult<int>> SaveMappingAsync(
        Guid userId,
        AdminMappingSaveRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _organizationRepository.ExistsAsync(request.OrgId, cancellationToken))
        {
            return ServiceResult<int>.Fail("Organization not found", "org_not_found");
        }

        if (!await IsAdminAsync(request.OrgId, userId, cancellationToken))
        {
            return ServiceResult<int>.Fail("Only admins can update mappings", "forbidden");
        }

        var exists = await _converterRepository.ExistsAsync(request.OrgId, request.ConverterId, cancellationToken);
        if (!exists)
        {
            return ServiceResult<int>.Fail("Converter not found", "not_found");
        }

        var version = await _mappingRepository.InsertMappingAsync(
            request.ConverterId,
            request.MappingJson,
            request.InputSample,
            request.OutputSample,
            cancellationToken);

        return ServiceResult<int>.Success(version);
    }

    public async Task<ServiceResult<IReadOnlyList<ConverterLogSummary>>> GetLogsAsync(
        Guid userId,
        Guid orgId,
        Guid converterId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (!await _organizationRepository.ExistsAsync(orgId, cancellationToken))
        {
            return ServiceResult<IReadOnlyList<ConverterLogSummary>>.Fail("Organization not found", "org_not_found");
        }

        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(role))
        {
            return ServiceResult<IReadOnlyList<ConverterLogSummary>>.Fail("Not a member", "forbidden");
        }

        var logs = await _logRepository.GetRecentAsync(converterId, limit, cancellationToken);
        return ServiceResult<IReadOnlyList<ConverterLogSummary>>.Success(logs);
    }

    private async Task<bool> IsAdminAsync(Guid orgId, Guid userId, CancellationToken cancellationToken)
    {
        var role = await _membershipRepository.GetRoleAsync(orgId, userId, cancellationToken);
        return role is "owner" or "admin";
    }
}
