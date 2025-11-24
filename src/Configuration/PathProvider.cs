using System.Runtime.InteropServices;

namespace Hyprwt.Configuration;

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
      ? Path.Combine(appDataDir, "hyprwt")
      : OperatingSystem.IsMacOS()
        ? Path.Combine(appDataDir, "Library", "Application Support", "hyprwt")
        : Path.Combine(appDataDir, ".config", "hyprwt");

    return Path.Combine(configDir, "config.json");
  }
}