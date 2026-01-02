namespace ZaptecUsageReport.Models;

public class InstallationReport
{
    public string InstallationName { get; set; } = string.Empty;
    public string InstallationAddress { get; set; } = string.Empty;
    public string InstallationZipCode { get; set; } = string.Empty;
    public string InstallationCity { get; set; } = string.Empty;
    public string InstallationTimeZone { get; set; } = string.Empty;
    public string GroupedBy { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<UserChargeReport> TotalUserChargerReportModel { get; set; } = new();
}