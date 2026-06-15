using Pigment.Core.Domain;

namespace Pigment.Core.Interfaces;

/// <summary>
/// Data-access contract for the Pigment HR integration.
/// Implemented in Pigment.Infrastructure using Dapper against SQL Server.
/// </summary>
public interface IHrDataRepository
{
    /// <summary>
    /// Executes [ResourceLink].[usp_GetHRDataForPigment] and returns all matching HR records.
    /// </summary>
    Task<IReadOnlyList<HrRecord>> GetHrDataAsync(
        string taxYear,
        string taxPeriod,
        CancellationToken cancellationToken = default);
}
