using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Pigment.Core.Domain;
using Pigment.Core.Interfaces;
using System.Data;

namespace Pigment.Infrastructure.Repositories;

/// <summary>
/// Executes [ResourceLink].[usp_GetHRDataForPigment] via Dapper
/// and maps the result set to <see cref="HrRecord"/>.
/// </summary>
public sealed class HrDataRepository : IHrDataRepository
{
    private readonly string _connectionString;
    private readonly ILogger<HrDataRepository> _logger;

    public HrDataRepository(string connectionString, ILogger<HrDataRepository> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        _connectionString = connectionString;
        _logger           = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HrRecord>> GetHrDataAsync(
        string taxYear,
        string taxPeriod,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing usp_GetHRDataForPigment: TaxYear={TaxYear}, TaxPeriod={TaxPeriod}",
            taxYear, taxPeriod);

        await using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@TaxYear",   taxYear,   DbType.String, size: 4);
        parameters.Add("@TaxPeriod", taxPeriod, DbType.String, size: 2);

        var command = new CommandDefinition(
            commandText:    "[ResourceLink].[usp_GetHRDataForPigment]",
            parameters:     parameters,
            commandType:    CommandType.StoredProcedure,
            commandTimeout: 180,
            cancellationToken: cancellationToken);

        var results = await connection.QueryAsync<HrRecord>(command);
        var list    = results.AsList();

        _logger.LogInformation("usp_GetHRDataForPigment returned {Count} records", list.Count);
        return list;
    }
}
