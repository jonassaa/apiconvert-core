using Apiconvert.Api.Converters;
using Apiconvert.Api.Inbound;
using Npgsql;
using NpgsqlTypes;

namespace Apiconvert.Infrastructure.Data;

public sealed class MappingRepository : IMappingRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public MappingRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<string?> GetLatestMappingJsonAsync(Guid converterId, CancellationToken cancellationToken)
    {
        const string sql = @"
select mapping_json
from converter_mappings
where converter_id = @converterId
order by version desc
limit 1;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("converterId", converterId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    public async Task<ConverterMappingSnapshot?> GetLatestMappingAsync(Guid converterId, CancellationToken cancellationToken)
    {
        const string sql = @"
select mapping_json, input_sample, output_sample
from converter_mappings
where converter_id = @converterId
order by version desc
limit 1;
";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("converterId", converterId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var mappingJson = reader.GetString(0);
        var inputSample = reader.IsDBNull(1) ? null : reader.GetString(1);
        var outputSample = reader.IsDBNull(2) ? null : reader.GetString(2);

        return new ConverterMappingSnapshot
        {
            MappingJson = mappingJson,
            InputSample = inputSample,
            OutputSample = outputSample
        };
    }

    public async Task<int> InsertMappingAsync(
        Guid converterId,
        string mappingJson,
        string? inputSample,
        string? outputSample,
        CancellationToken cancellationToken)
    {
        const string selectSql = @"
select version
from converter_mappings
where converter_id = @converterId
order by version desc
limit 1;
";
        int version = 1;
        await using (var selectCommand = _dataSource.CreateCommand(selectSql))
        {
            selectCommand.Parameters.AddWithValue("converterId", converterId);
            var result = await selectCommand.ExecuteScalarAsync(cancellationToken);
            if (result is int currentVersion)
            {
                version = currentVersion + 1;
            }
        }

        const string insertSql = @"
insert into converter_mappings (
  converter_id,
  mapping_json,
  input_sample,
  output_sample,
  version
) values (
  @converterId,
  @mappingJson,
  @inputSample,
  @outputSample,
  @version
);
";
        await using var insertCommand = _dataSource.CreateCommand(insertSql);
        insertCommand.Parameters.AddWithValue("converterId", converterId);
        var param = insertCommand.Parameters.Add("mappingJson", NpgsqlDbType.Jsonb);
        param.Value = mappingJson;
        insertCommand.Parameters.AddWithValue("inputSample", (object?)inputSample ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("outputSample", (object?)outputSample ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("version", version);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);

        return version;
    }
}
