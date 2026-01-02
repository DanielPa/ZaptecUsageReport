namespace ZaptecUsageReport.Models;

public class ChargeSession
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public double Energy { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public double CommitMetadata { get; set; }
    public string SignedSession { get; set; } = string.Empty;
    public string ChargerId { get; set; } = string.Empty;
    public string ChargerName { get; set; } = string.Empty;
    public string InstallationId { get; set; } = string.Empty;
    public string InstallationName { get; set; } = string.Empty;
}