namespace ZaptecUsageReport.Models;

public class ArchivedSessionUser
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
}

public class ArchivedSession
{
    public string Id { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string ChargerId { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public double Energy { get; set; }
    public ArchivedSessionUser? AuthorizedUser { get; set; }
    public string? TokenName { get; set; }
    public string? SessionSignature { get; set; }
}

public class ArchivedSessionsResponse
{
    public List<ArchivedSession> Sessions { get; set; } = new();
    public string? Cursor { get; set; }
    public bool HasMore { get; set; }
}
