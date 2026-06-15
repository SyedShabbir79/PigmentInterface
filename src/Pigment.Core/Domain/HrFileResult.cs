namespace Pigment.Core.Domain;

/// <summary>
/// Returned by the file-generation endpoint: blob URL plus run metadata.
/// </summary>
public sealed record HrFileResult(
    string TaxYear,
    string TaxPeriod,
    DateTime GeneratedAtUtc,
    string FileUrl,
    int RecordCount
);
