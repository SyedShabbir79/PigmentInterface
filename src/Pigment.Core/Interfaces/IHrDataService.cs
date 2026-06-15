using Pigment.Core.Domain;

namespace Pigment.Core.Interfaces;

/// <summary>
/// Application-service contract for the Pigment HR integration.
/// </summary>
public interface IHrDataService
{
    /// <summary>
    /// Returns all HR records for the given payroll period as a typed list.
    /// </summary>
    Task<IReadOnlyList<HrRecord>> GetHrDataAsync(
        string taxYear,
        string taxPeriod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pipe-delimited text file from HR data,
    /// uploads it to Azure Blob Storage, and returns metadata including the blob URL.
    /// </summary>
    Task<HrFileResult> GenerateAndStoreHrFileAsync(
        string taxYear,
        string taxPeriod,
        CancellationToken cancellationToken = default);
}
