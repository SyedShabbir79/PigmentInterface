namespace Pigment.Core.Domain;

/// <summary>
/// Represents a single employee HR record returned for the Pigment integration.
/// All fields are nullable to accommodate partial data from the ResourceLink stored procedure.
/// </summary>
public sealed record HrRecord
{
    public string? EmployeeNumber              { get; init; }
    public string? Forename                    { get; init; }
    public string? Surname                     { get; init; }
    public string? KnownAs                     { get; init; }
    public string? ManagerNames                { get; init; }
    public string? AccountCode1                { get; init; }
    public string? CostCentre1                 { get; init; }
    public string? CostCentrePercentage1       { get; init; }
    public string? AccountCode2                { get; init; }
    public string? CostCentre2                 { get; init; }
    public string? CostCentrePercentage2       { get; init; }
    public string? AccountCode3                { get; init; }
    public string? CostCentre3                 { get; init; }
    public string? CostCentrePercentage3       { get; init; }
    public string? Account                     { get; init; }
    public string? Project                     { get; init; }
    public string? ContractType                { get; init; }
    public string? PostNumber                  { get; init; }
    public string? PostClass                   { get; init; }
    public string? PostClassDesc               { get; init; }
    public string? Role                        { get; init; }
    public string? JobTitle                    { get; init; }
    public decimal? Fte                        { get; init; }
    public string? Grade                       { get; init; }
    public string? GradePoint                  { get; init; }
    public string? OrigStartDate               { get; init; }
    public string? StartDate                   { get; init; }
    public string? EndDate                     { get; init; }
    public string? PostEndDate                 { get; init; }
    public string? ProjectedEndDate            { get; init; }
    public string? LeaveStartDate              { get; init; }
    public string? LeaveEndDate                { get; init; }
    public string  Currency                    { get; init; } = "GBP";
    public decimal? BasicSalary                { get; init; }
    public decimal? CasualPayment              { get; init; }
    public decimal? FractionalPayment          { get; init; }
    public decimal? FractionalLondonAllowance  { get; init; }
    public decimal? MarketAllowance            { get; init; }
    public decimal? LondonAllowance            { get; init; }
    public decimal? OtherAllowance             { get; init; }
    public decimal? HodAllowance               { get; init; }
    public decimal? HolPay                     { get; init; }
    public decimal? EightWeeksPay              { get; init; }
    public decimal? KitDays                    { get; init; }
    public decimal? RespAllow2095              { get; init; }
    public decimal? ExtraResp                  { get; init; }
    public decimal? RespAllow2098              { get; init; }
    public decimal? TotalAllowance             { get; init; }
    public string?  PensionScheme              { get; init; }
    public decimal? ErsPension                 { get; init; }
    public string?  ErPensionPerc              { get; init; }
    public decimal? Ni                         { get; init; }
    public decimal? SspPayment                 { get; init; }
    public decimal? OspPayment                 { get; init; }
    public decimal? SmpPayment                 { get; init; }
    public decimal? OmpPayment0112             { get; init; }
    public decimal? OmpPayment0114             { get; init; }
    public decimal? StatutoryAdoptionPay       { get; init; }
    public decimal? StatutoryPaternityPayAdopt { get; init; }
    public decimal? StatutoryPaternityPayBirth { get; init; }
    public decimal? SslPayment                 { get; init; }
    public string?  TbhId                      { get; init; }
}
