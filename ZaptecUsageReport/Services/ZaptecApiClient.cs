using RestSharp;
using System.Text.Json;
using ZaptecUsageReport.Models;

namespace ZaptecUsageReport.Services;

public class ZaptecApiClient
{
    private readonly RestClient _client;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public ZaptecApiClient(string baseUrl)
    {
        var options = new RestClientOptions(baseUrl)
        {
            ThrowOnAnyError = false
        };
        _client = new RestClient(options);
    }

    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        var request = new RestRequest("/oauth/token", Method.Post);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "password");
        request.AddParameter("username", username);
        request.AddParameter("password", password);
        request.AddParameter("scope", "openid");

        var response = await _client.ExecuteAsync(request);

        if (!response.IsSuccessful || response.Content == null)
        {
            throw new Exception($"Authentication failed: {response.ErrorMessage ?? response.StatusCode.ToString()}");
        }

        var tokenResponse = JsonSerializer.Deserialize<AuthTokenResponse>(response.Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        if (tokenResponse == null)
        {
            throw new Exception("Failed to parse authentication response");
        }

        _accessToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Refresh 1 minute before expiry

        return true;
    }

    public async Task<InstallationReport?> GetInstallationReportAsync(string installationId, DateTime fromDate, DateTime toDate)
    {
        if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry)
        {
            throw new Exception("Not authenticated or token expired. Please authenticate first.");
        }

        var request = new RestRequest("/api/chargehistory/installationreport", Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {_accessToken}");

        // Format dates as ISO 8601 without timezone suffix (like Python's isoformat())
        var fromDateStr = $"{fromDate.Year:D4}-{fromDate.Month:D2}-{fromDate.Day:D2}T00:00:00";
        var toDateStr = $"{toDate.Year:D4}-{toDate.Month:D2}-{toDate.Day:D2}T23:59:59";

        var requestBody = new
        {
            fromDate = fromDateStr,
            endDate = toDateStr,
            installationId = installationId,
            groupBy = 2,
            reportFormat = 1
        };

        request.AddJsonBody(requestBody);

        var response = await _client.ExecuteAsync(request);

        if (!response.IsSuccessful || response.Content == null)
        {
            throw new Exception($"Failed to get installation report: {response.ErrorMessage ?? response.StatusCode.ToString()}");
        }

        var report = JsonSerializer.Deserialize<InstallationReport>(response.Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return report;
    }

    public async Task<List<ChargeSession>> GetChargeHistoryAsync(string? installationId = null, DateTime? fromDate = null, DateTime? toDate = null, int pageSize = 200)
    {
        if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry)
        {
            throw new Exception("Not authenticated or token expired. Please authenticate first.");
        }

        var allSessions = new List<ChargeSession>();
        string? cursor = null;

        do
        {
            var request = new RestRequest("/api/sessions/archived", Method.Get);
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {_accessToken}");

            request.AddParameter("PageSize", Math.Min(pageSize, 200));

            if (!string.IsNullOrEmpty(installationId))
            {
                request.AddParameter("InstallationId", installationId);
            }

            if (fromDate.HasValue)
            {
                var fromDateStr = $"{fromDate.Value.Year:D4}-{fromDate.Value.Month:D2}-{fromDate.Value.Day:D2}T00:00:00";
                request.AddParameter("From", fromDateStr);
            }

            if (toDate.HasValue)
            {
                var toDateStr = $"{toDate.Value.Year:D4}-{toDate.Value.Month:D2}-{toDate.Value.Day:D2}T23:59:59";
                request.AddParameter("To", toDateStr);
            }

            if (cursor != null)
            {
                request.AddParameter("Cursor", cursor);
            }

            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful || response.Content == null)
            {
                throw new Exception($"Failed to get charge history: {response.ErrorMessage ?? response.StatusCode.ToString()}");
            }

            var archivedResponse = JsonSerializer.Deserialize<ArchivedSessionsResponse>(response.Content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (archivedResponse == null)
            {
                break;
            }

            allSessions.AddRange(archivedResponse.Sessions.Select(MapToChargeSession));
            cursor = archivedResponse.HasMore ? archivedResponse.Cursor : null;

        } while (cursor != null);

        return allSessions;
    }

    private static ChargeSession MapToChargeSession(ArchivedSession s) => new()
    {
        Id = s.Id,
        DeviceId = s.DeviceId ?? string.Empty,
        DeviceName = s.DeviceName ?? string.Empty,
        ChargerName = s.DeviceName ?? string.Empty,
        ChargerId = s.ChargerId,
        StartDateTime = s.StartDateTime,
        EndDateTime = s.EndDateTime ?? DateTime.MinValue,
        Energy = s.Energy,
        UserId = s.AuthorizedUser?.Id ?? string.Empty,
        UserFullName = s.AuthorizedUser?.FullName ?? string.Empty,
        UserEmail = s.AuthorizedUser?.Email ?? string.Empty,
        TokenName = s.TokenName ?? string.Empty,
        SignedSession = s.SessionSignature ?? string.Empty,
    };
}