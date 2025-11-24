namespace Hyprwt.Configuration;

/// <summary>
/// Configuration settings for cleanup behavior.
/// </summary>
public class CleanupConfig
{
    /// <summary>
    /// Default cleanup mode.
    /// </summary>
    public CleanupMode DefaultMode { get; set; } = CleanupMode.Interactive;
}

/// <summary>
/// Cleanup selection modes.
/// </summary>
public enum CleanupMode
{
    /// <summary>Remove all non-primary worktrees.</summary>
    All,

    /// <summary>Remove worktrees without remote branch.</summary>
    Remoteless,

    /// <summary>Remove merged worktrees.</summary>
    Merged,

    /// <summary>Interactive TUI selection.</summary>
    Interactive,

    /// <summary>Cleanup based on closed GitHub PRs.</summary>
    GitHub
}
