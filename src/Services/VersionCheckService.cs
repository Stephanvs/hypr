using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace Hyprwt.Services;

/// <summary>
/// Checks for version updates from GitHub releases.
/// </summary>
public class VersionCheckService
{
    private readonly ILogger<VersionCheckService> _logger;
    private readonly StateService _stateService;
    private readonly string _currentVersion;

    public VersionCheckService(ILogger<VersionCheckService> logger, StateService stateService)
    {
        _logger = logger;
        _stateService = stateService;
        _currentVersion = GetCurrentVersion();
    }

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    private static string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "0.0.0";
    }

    /// <summary>
    /// Checks for updates from GitHub releases.
    /// </summary>
    /// <returns>Version information if update available, otherwise null.</returns>
    public async Task<VersionInfo?> CheckForUpdates()
    {
        // Only check once per day
        if (!_stateService.ShouldCheckVersion())
        {
            _logger.LogDebug("Skipping version check (checked recently)");
            return null;
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("hyprwt/1.0");

            var response = await client.GetStringAsync(
                "https://api.github.com/repos/irskep/hyprwt/releases/latest");

            var release = JsonSerializer.Deserialize<GitHubRelease>(response);
            if (release == null)
                return null;

            var latestVersion = release.tag_name?.TrimStart('v') ?? "0.0.0";

            // Update last check time
            _stateService.UpdateLastVersionCheck();

            // Compare versions
            if (IsNewerVersion(latestVersion, _currentVersion))
            {
                return new VersionInfo
                {
                    Current = _currentVersion,
                    Latest = latestVersion,
                    UpdateAvailable = true,
                    ChangelogUrl = $"https://github.com/irskep/hyprwt/releases/tag/{release.tag_name}",
                    InstallCommand = DetectInstallCommand()
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check for updates (silently continuing)");
            return null;
        }
    }

    /// <summary>
    /// Detects the installation method and returns appropriate update command.
    /// </summary>
    private string DetectInstallCommand()
    {
        // TODO: Detect how hyprwt was installed (dotnet tool, manual, package manager, etc.)
        // For now, return generic command
        return "dotnet tool update -g hyprwt";
    }

    /// <summary>
    /// Compares version strings.
    /// </summary>
    /// <returns>True if latestVersion is newer than currentVersion.</returns>
    private bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        try
        {
            var latest = new Version(latestVersion);
            var current = new Version(currentVersion);
            return latest > current;
        }
        catch
        {
            return false;
        }
    }

    private class GitHubRelease
    {
        public string? tag_name { get; set; }
        public string? html_url { get; set; }
        public string? body { get; set; }
    }
}

/// <summary>
/// Version update information.
/// </summary>
public class VersionInfo
{
    public string? Current { get; set; }
    public string? Latest { get; set; }
    public bool UpdateAvailable { get; set; }
    public string? ChangelogUrl { get; set; }
    public string? InstallCommand { get; set; }
}
