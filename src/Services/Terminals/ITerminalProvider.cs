using Hypr.Configuration;

namespace Hypr.Services.Terminals;

/// <summary>
/// Supported platforms for terminal providers.
/// </summary>
[Flags]
public enum Platform
{
    None = 0,
    Windows = 1,
    MacOS = 2,
    Linux = 4,
    All = Windows | MacOS | Linux
}

/// <summary>
/// Interface for terminal providers that can open worktrees in various terminal applications.
/// </summary>
public interface ITerminalProvider
{
    /// <summary>
    /// Display name of the terminal provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The platforms this provider supports.
    /// </summary>
    Platform SupportedPlatforms { get; }

    /// <summary>
    /// Priority for provider selection (higher = preferred).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if the terminal is available on the current system (e.g., installed and accessible).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Checks if the provider supports the specified terminal mode.
    /// </summary>
    /// <param name="mode">The terminal mode to check.</param>
    /// <returns>True if the mode is supported.</returns>
    bool SupportsMode(TerminalMode mode);

    /// <summary>
    /// Opens a terminal at the specified working directory.
    /// </summary>
    /// <param name="workingDirectory">The directory to open.</param>
    /// <param name="mode">The terminal mode (tab, window, etc.).</param>
    /// <param name="initCommand">Optional command to run after opening.</param>
    /// <returns>True if successful.</returns>
    bool Open(string workingDirectory, TerminalMode mode, string? initCommand = null);
}
