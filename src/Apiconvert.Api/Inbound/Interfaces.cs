using Apiconvert.Api.Converters;

namespace Apiconvert.Api.Inbound;

public interface IOrganizationRepository
{
    Task<bool> ExistsAsync(Guid orgId, CancellationToken cancellationToken);
}

public interface IConverterRepository
{
    Task<ConverterConfig?> GetByOrgAndPathAsync(Guid orgId, string inboundPath, CancellationToken cancellationToken);
}

public interface IMappingRepository
{
    Task<string?> GetLatestMappingJsonAsync(Guid converterId, CancellationToken cancellationToken);
    Task<ConverterMappingSnapshot?> GetLatestMappingAsync(Guid converterId, CancellationToken cancellationToken);
    Task<int> InsertMappingAsync(Guid converterId, string mappingJson, string? inputSample, string? outputSample, CancellationToken cancellationToken);
}

public interface IConverterLogRepository
{
    Task InsertAsync(ConverterLogEntry entry, CancellationToken cancellationToken);
    Task CleanupAsync(Guid converterId, int retentionDays, CancellationToken cancellationToken);
}

public interface IForwarder
{
    Task<ForwardResult> SendAsync(ForwardRequest request, CancellationToken cancellationToken);
}

public interface IRateLimiter
{
    bool IsRateLimited(string key);
}
