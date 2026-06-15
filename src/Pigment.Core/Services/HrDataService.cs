using System.Text;
using Microsoft.Extensions.Logging;
using Pigment.Core.Domain;
using Pigment.Core.Interfaces;

namespace Pigment.Core.Services;

/// <summary>
/// Orchestrates HR data retrieval, pipe-delimited file generation, and Azure Blob upload.
/// </summary>
public sealed class HrDataService : IHrDataService
{
    private readonly IHrDataRepository _repository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<HrDataService> _logger;

    // Column headers — order must match FormatRow() below.
    private static readonly string[] Headers =
    [
        "EmployeeNumber","Forename","Surname","KnownAs","ManagerNames",
        "AccountCode1","CostCentre1","CostCentrePercentage1",
        "AccountCode2","CostCentre2","CostCentrePercentage2",
        "AccountCode3","CostCentre3","CostCentrePercentage3",
        "Account","Project","ContractType",
        "PostNumber","PostClass","PostClassDesc","Role","JobTitle",
        "FTE","Grade","GradePoint",
        "OrigStartDate","StartDate","EndDate","PostEndDate","ProjectedEndDate",
        "LeaveStartDate","LeaveEndDate","Currency",
        "BasicSalary","CasualPayment","FractionalPayment","FractionalLondonAllowance",
        "MarketAllowance","LondonAllowance","OtherAllowance","HodAllowance","HolPay",
        "EightWeeksPay","KitDays","RespAllow2095","ExtraResp","RespAllow2098","TotalAllowance",
        "PensionScheme","ErsPension","ErPensionPerc","NI",
        "SSP","OSP","SMP","OMP_0112","OMP_0114",
        "StatutoryAdoptionPay","StatutoryPaternityPayAdopt","StatutoryPaternityPayBirth",
        "SSLPayment","TbhId"
    ];

    public HrDataService(
        IHrDataRepository repository,
        IBlobStorageService blobStorage,
        ILogger<HrDataService> logger)
    {
        _repository  = repository;
        _blobStorage = blobStorage;
        _logger      = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<HrRecord>> GetHrDataAsync(
        string taxYear, string taxPeriod, CancellationToken cancellationToken = default)
        => _repository.GetHrDataAsync(taxYear, taxPeriod, cancellationToken);

    /// <inheritdoc />
    public async Task<HrFileResult> GenerateAndStoreHrFileAsync(
        string taxYear, string taxPeriod, CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetHrDataAsync(taxYear, taxPeriod, cancellationToken);

        _logger.LogInformation(
            "Generating HR file for TaxYear={TaxYear}, TaxPeriod={TaxPeriod} — {Count} records",
            taxYear, taxPeriod, records.Count);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join("|", Headers));
        foreach (var r in records)
            sb.AppendLine(FormatRow(r));

        var timestamp = DateTime.UtcNow;
        var fileName  = $"pigment_hr_{taxYear}_{taxPeriod}_{timestamp:yyyyMMdd_HHmmss}.txt";
        var bytes     = Encoding.UTF8.GetBytes(sb.ToString());

        var blobUrl = await _blobStorage.UploadFileAsync(fileName, bytes, "text/plain", cancellationToken);

        _logger.LogInformation("HR file uploaded to {BlobUrl}", blobUrl);

        return new HrFileResult(
            TaxYear:        taxYear,
            TaxPeriod:      taxPeriod,
            GeneratedAtUtc: timestamp,
            FileUrl:        blobUrl,
            RecordCount:    records.Count);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string FormatRow(HrRecord r) =>
        string.Join("|",
            Esc(r.EmployeeNumber),         Esc(r.Forename),
            Esc(r.Surname),                Esc(r.KnownAs),
            Esc(r.ManagerNames),           Esc(r.AccountCode1),
            Esc(r.CostCentre1),            Esc(r.CostCentrePercentage1),
            Esc(r.AccountCode2),           Esc(r.CostCentre2),
            Esc(r.CostCentrePercentage2),  Esc(r.AccountCode3),
            Esc(r.CostCentre3),            Esc(r.CostCentrePercentage3),
            Esc(r.Account),                Esc(r.Project),
            Esc(r.ContractType),           Esc(r.PostNumber),
            Esc(r.PostClass),              Esc(r.PostClassDesc),
            Esc(r.Role),                   Esc(r.JobTitle),
            r.Fte,                         Esc(r.Grade),
            Esc(r.GradePoint),             Esc(r.OrigStartDate),
            Esc(r.StartDate),              Esc(r.EndDate),
            Esc(r.PostEndDate),            Esc(r.ProjectedEndDate),
            Esc(r.LeaveStartDate),         Esc(r.LeaveEndDate),
            r.Currency,
            r.BasicSalary,                 r.CasualPayment,
            r.FractionalPayment,           r.FractionalLondonAllowance,
            r.MarketAllowance,             r.LondonAllowance,
            r.OtherAllowance,              r.HodAllowance,
            r.HolPay,                      r.EightWeeksPay,
            r.KitDays,                     r.RespAllow2095,
            r.ExtraResp,                   r.RespAllow2098,
            r.TotalAllowance,              Esc(r.PensionScheme),
            r.ErsPension,                  Esc(r.ErPensionPerc),
            r.Ni,                          r.SspPayment,
            r.OspPayment,                  r.SmpPayment,
            r.OmpPayment0112,              r.OmpPayment0114,
            r.StatutoryAdoptionPay,        r.StatutoryPaternityPayAdopt,
            r.StatutoryPaternityPayBirth,  r.SslPayment,
            Esc(r.TbhId));

    /// <summary>Wraps values containing the pipe delimiter in double-quotes.</summary>
    private static string Esc(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Contains('|') ? $"\"{value}\"" : value;
    }
}
