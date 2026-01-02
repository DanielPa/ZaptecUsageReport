namespace ZaptecUsageReport.Models;

public class ChargeHistoryResponse
{
    public List<ChargeSession> Data { get; set; } = new();
    public int Pages { get; set; }
}