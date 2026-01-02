namespace ZaptecUsageReport.Models;

public class UserChargeReport
{
    public string GroupAsString { get; set; } = string.Empty;
    public UserDetails? UserDetails { get; set; }
    public double TotalChargeSessionCount { get; set; }
    public double TotalChargeSessionEnergy { get; set; }
    public double TotalChargeSessionDuration { get; set; }
}
