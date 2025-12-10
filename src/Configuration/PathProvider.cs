using System.Runtime.InteropServices;

namespace Hypr.Configuration;

public static class PathProvider
{
  /// <summary>
  /// Gets the global configuration file path based on the platform.
  /// </summary>
  public static string GetGlobalConfigPath()
  {
    var appDataDir = Environment.GetFolderPath(
      OperatingSystem.IsWindows()
        ? Environment.SpecialFolder.ApplicationData
        : Environment.SpecialFolder.UserProfile);

    var configDir = OperatingSystem.IsWindows()
      ? Path.Combine(appDataDir, "hypr")
      : Path.Combine(appDataDir, ".config", "hypr");

    return Path.Combine(configDir, "config.json");
  }

  /// <summary>
  /// Gets the platform-specific log file path.
  /// </summary>
  public static string GetLogFilePath()
  {
    var logDir = OperatingSystem.IsWindows()
      ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "hypr", "logs")
      : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "hypr", "logs");

    Directory.CreateDirectory(logDir);
    return Path.Combine(logDir, "hypr.log");
  }
}