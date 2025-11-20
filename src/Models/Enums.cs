namespace Hyprwt.Models;

/// <summary>
/// Terminal switching modes.
/// </summary>
public enum TerminalMode
{
    /// <summary>Open in new tab.</summary>
    Tab,

    /// <summary>Open in new window.</summary>
    Window,

    /// <summary>Change directory in current session.</summary>
    Inplace,

    /// <summary>Print cd command only.</summary>
    Echo,

    /// <summary>Open in VSCode.</summary>
    VSCode,

    /// <summary>Open in Cursor.</summary>
    Cursor
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
