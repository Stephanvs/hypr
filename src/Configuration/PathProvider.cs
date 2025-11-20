using System.Runtime.InteropServices;

namespace Hyprwt.Configuration;

public static class PathProvider
{
  /// <summary>
  /// Gets the default configuration directory based on the platform.
  /// </summary>
  /// <returns>The configuration directory path.</returns>
  public static string GetDefaultConfigDirectory()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      // Windows: %APPDATA%\hyprwt
      var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      return Path.Combine(appData, "hyprwt");
    }
    else
    {
      // Linux/macOS: ~/.config/hyprwt
      var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      return Path.Combine(home, ".config", "hyprwt");
    }
  }
}