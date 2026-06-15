namespace Pigment.Infrastructure.Entities;

/// <summary>
/// Audit/run-history record stored per API call.
/// Maps to [Pigment].[PigmentRuns] table.
/// </summary>
public sealed class PigmentRunEntity
{
    public int      Id             { get; set; }
    public string   TaxYear        { get; set; } = null!;
    public string   TaxPeriod      { get; set; } = null!;
    public string   TriggeredBy    { get; set; } = null!;
    public DateTime RunDateTimeUtc { get; set; }
    public string   Status         { get; set; } = null!;
    public string?  Message        { get; set; }
    public string?  FileName       { get; set; }
    public string?  FileUrl        { get; set; }
    public int?     RecordCount    { get; set; }
}
