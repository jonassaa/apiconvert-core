namespace Apiconvert.Api.Converters;

public interface IConverterQueryRepository
{
    Task<IReadOnlyList<ConverterSummary>> ListAsync(Guid orgId, string? search, bool? enabled, bool? logging, CancellationToken cancellationToken);
    Task<ConverterDetail?> GetByNameAsync(Guid orgId, string normalizedName, CancellationToken cancellationToken);
    Task<ConverterDetail?> GetByIdAsync(Guid orgId, Guid converterId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(Guid orgId, string normalizedName, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> ExistsByInboundPathAsync(Guid orgId, string inboundPath, Guid? excludeId, CancellationToken cancellationToken);
}
