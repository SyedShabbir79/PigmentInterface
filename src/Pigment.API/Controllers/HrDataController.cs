using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pigment.Core.Domain;
using Pigment.Core.Interfaces;

namespace Pigment.API.Controllers;

/// <summary>
/// Exposes HR payroll data from ResourceLink to the Pigment planning system.
/// </summary>
[ApiController]
[Route("api/v1/hr")]
[Authorize(Policy = "PigmentUser")]
[Produces("application/json")]
public sealed class HrDataController : ControllerBase
{
    private readonly IHrDataService _hrDataService;
    private readonly ILogger<HrDataController> _logger;

    public HrDataController(
        IHrDataService hrDataService,
        ILogger<HrDataController> logger)
    {
        _hrDataService = hrDataService;
        _logger        = logger;
    }

    // ----------------------------------------------------------------
    // GET /api/v1/hr/json?taxYear=2025&taxPeriod=04
    // ----------------------------------------------------------------
    /// <summary>
    /// Returns all HR records for the specified payroll period as JSON.
    /// </summary>
    /// <param name="taxYear">Four-digit tax year, e.g. 2025</param>
    /// <param name="taxPeriod">One or two-digit tax period, e.g. 04</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("json")]
    [ProducesResponseType(typeof(HrJsonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHrDataJson(
        [FromQuery, Required, RegularExpression(@"^\d{4}$",  ErrorMessage = "taxYear must be a 4-digit number.")]  string taxYear,
        [FromQuery, Required, RegularExpression(@"^\d{1,2}$", ErrorMessage = "taxPeriod must be 1-2 digits.")]      string taxPeriod,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "HrData JSON request — TaxYear={TaxYear}, TaxPeriod={TaxPeriod}, User={User}",
            taxYear, taxPeriod, User.Identity?.Name);

        var records = await _hrDataService.GetHrDataAsync(taxYear, taxPeriod, cancellationToken);

        return Ok(new HrJsonResponse(
            TaxYear:     taxYear,
            TaxPeriod:   taxPeriod,
            GeneratedAt: DateTime.UtcNow,
            Count:       records.Count,
            Data:        records));
    }

    // ----------------------------------------------------------------
    // GET /api/v1/hr/file?taxYear=2025&taxPeriod=04
    // ----------------------------------------------------------------
    /// <summary>
    /// Generates a pipe-delimited HR data file, uploads it to Azure Blob Storage,
    /// and returns the blob URL plus run metadata.
    /// </summary>
    /// <param name="taxYear">Four-digit tax year, e.g. 2025</param>
    /// <param name="taxPeriod">One or two-digit tax period, e.g. 04</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("file")]
    [ProducesResponseType(typeof(HrFileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHrDataFile(
        [FromQuery, Required, RegularExpression(@"^\d{4}$",  ErrorMessage = "taxYear must be a 4-digit number.")]  string taxYear,
        [FromQuery, Required, RegularExpression(@"^\d{1,2}$", ErrorMessage = "taxPeriod must be 1-2 digits.")]      string taxPeriod,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "HrData FILE request — TaxYear={TaxYear}, TaxPeriod={TaxPeriod}, User={User}",
            taxYear, taxPeriod, User.Identity?.Name);

        var result = await _hrDataService.GenerateAndStoreHrFileAsync(taxYear, taxPeriod, cancellationToken);

        return Ok(result);
    }
}

// ---------------------------------------------------------------
// Response envelope for the JSON endpoint
// ---------------------------------------------------------------
/// <summary>Wrapper returned by GET /api/v1/hr/json</summary>
public sealed record HrJsonResponse(
    string TaxYear,
    string TaxPeriod,
    DateTime GeneratedAt,
    int Count,
    IReadOnlyList<HrRecord> Data
);
