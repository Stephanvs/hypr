using Hypr.Configuration;
using Hypr.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hypr.Services;

/// <summary>
/// Manages application state and persistence.
/// Handles state directory, version check state, and cleanup tracking.
/// </summary>
public class StateService
{
    private readonly ILogger<StateService> _logger;
    private readonly string _appDir;
    private readonly string _stateFile;

    public StateService(ILogger<StateService> logger)
    {
        _logger = logger;
        _appDir = GetAppDirectory();
        _stateFile = Path.Combine(_appDir, "state.json");

        EnsureAppDirectoryExists();
    }

    /// <summary>
    /// Gets the application directory based on platform.
    /// </summary>
    private static string GetAppDirectory()
    {
        var appDataDir = Environment.GetFolderPath(
            Environment.OSVersion.Platform == PlatformID.Win32NT 
                ? Environment.SpecialFolder.ApplicationData 
                : Environment.SpecialFolder.UserProfile);
        
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => Path.Combine(appDataDir, "hypr"),
            PlatformID.Unix => Path.Combine(appDataDir, ".config", "hypr"),
            PlatformID.MacOSX => Path.Combine(appDataDir, "Library", "Application Support", "hypr"),
            _ => Path.Combine(appDataDir, ".config", "hypr")
        };
    }

    /// <summary>
    /// Gets the application directory path.
    /// </summary>
    public string AppDir => _appDir;

    /// <summary>
    /// Ensures the application directory exists.
    /// </summary>
    private void EnsureAppDirectoryExists()
    {
        if (!Directory.Exists(_appDir))
        {
            Directory.CreateDirectory(_appDir);
            _logger.LogDebug("Created app directory: {AppDir}", _appDir);
        }
    }

    /// <summary>
    /// Loads state from JSON file.
    /// </summary>
    public AppState LoadState()
    {
        if (!File.Exists(_stateFile))
        {
            _logger.LogDebug("No state file found, returning empty state");
            return new AppState();
        }

        try
        {
            var json = File.ReadAllText(_stateFile);
            var state = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppState);
            _logger.LogDebug("State loaded successfully");
            return state ?? new AppState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load state file");
            return new AppState();
        }
    }

    /// <summary>
    /// Saves state to JSON file.
    /// </summary>
    public void SaveState(AppState state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, AppJsonSerializerContext.Default.AppState);
            File.WriteAllText(_stateFile, json);
            _logger.LogDebug("State saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save state file");
        }
    }

    /// <summary>
    /// Gets the last version check timestamp.
    /// </summary>
    public DateTime? GetLastVersionCheck()
    {
        var state = LoadState();
        return state.LastVersionCheck;
    }

    /// <summary>
    /// Updates the last version check timestamp.
    /// </summary>
    public void UpdateLastVersionCheck()
    {
        var state = LoadState();
        state.LastVersionCheck = DateTime.UtcNow;
        SaveState(state);
    }

    /// <summary>
    /// Checks if version check should be performed (once per day).
    /// </summary>
    public bool ShouldCheckVersion()
    {
        var lastCheck = GetLastVersionCheck();
        if (lastCheck == null)
            return true;

        return (DateTime.UtcNow - lastCheck.Value).TotalHours >= 24;
    }
}

/// <summary>
/// Application state data.
/// </summary>
public class AppState
{
    /// <summary>
    /// Last time version was checked.
    /// </summary>
    public DateTime? LastVersionCheck { get; set; }

    /// <summary>
    /// Worktrees that have been cleaned up (for tracking).
    /// </summary>
    public List<string> CleanedUpWorktrees { get; set; } = new();

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
