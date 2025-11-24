namespace Hyprwt.Configuration;

public static class PathProvider
{
  /// <summary>
  /// Gets the global configuration file path based on the platform.
  /// </summary>
  public static string GetGlobalConfigPath()
  {
    var appDataDir = Environment.GetFolderPath(
      Environment.OSVersion.Platform == PlatformID.Win32NT
        ? Environment.SpecialFolder.ApplicationData
        : Environment.SpecialFolder.UserProfile);

    var configDir = Environment.OSVersion.Platform switch
    {
      PlatformID.Win32NT => Path.Combine(appDataDir, "hyprwt"),
      PlatformID.Unix => Path.Combine(appDataDir, ".config", "hyprwt"),
      PlatformID.MacOSX => Path.Combine(appDataDir, "Library", "Application Support", "hyprwt"),
      _ => Path.Combine(appDataDir, ".config", "hyprwt")
    };

    return Path.Combine(configDir, "config.json");
  }
}