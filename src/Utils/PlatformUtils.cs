using System.Runtime.InteropServices;

namespace Hyprwt.Utils;

/// <summary>
/// Platform-specific utilities for cross-platform support.
/// </summary>
public static class PlatformUtils
{


    /// <summary>
    /// Checks if we're running on Windows.
    /// </summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Checks if we're running on macOS.
    /// </summary>
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Checks if we're running on Linux.
    /// </summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
