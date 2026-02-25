using System.Net.Http.Json;
using System.Reflection;
using Serilog;

namespace SmartStandby.Core.Services;

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private const string UpdateUrl = "https://raw.githubusercontent.com/nice-copacabana/Smart-Standby/main/version.json";

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string GetCurrentVersion()
    {
        var version = typeof(UpdateService).Assembly.GetName().Version;
        // Fallback to 1.0.0 if version cannot be retrieved
        return version?.ToString(3) ?? "1.0.0";
    }

    public async Task<(bool UpdateAvailable, string? LatestVersion, string? DownloadUrl)> CheckForUpdatesAsync()
    {
        try
        {
            // This will fail in a real environment unless the URL is valid.
            // For now, it's a structural implementation.
            var remoteInfo = await _httpClient.GetFromJsonAsync<UpdateInfo>(UpdateUrl);
            if (remoteInfo == null || string.IsNullOrEmpty(remoteInfo.Version))
                return (false, null, null);

            if (Version.TryParse(GetCurrentVersion(), out var currentVersion) && 
                Version.TryParse(remoteInfo.Version, out var latestVersion))
            {
                if (latestVersion > currentVersion)
                {
                    return (true, remoteInfo.Version, remoteInfo.DownloadUrl);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Warning(ex, "Update check failed: network error.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Update check failed: unexpected error.");
        }

        return (false, null, null);
    }
}

public class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
}
