namespace Hyprwt.Models;

/// <summary>
/// Encapsulates all parameters for switching to/creating a worktree.
/// </summary>
/// <param name="Branch">The branch name or path.</param>
/// <param name="TerminalMode">How to open the terminal.</param>
/// <param name="InitScript">Script to run after session starts.</param>
/// <param name="AfterInit">Command to run after init script completes.</param>
/// <param name="IgnoreSameSession">Always create new terminal, ignore existing sessions.</param>
/// <param name="AutoConfirm">Auto-confirm all prompts.</param>
/// <param name="Debug">Enable debug logging.</param>
/// <param name="CustomScript">Custom script to run with arguments.</param>
/// <param name="FromBranch">Source branch/commit to create worktree from.</param>
/// <param name="Dir">Directory path for the new worktree.</param>
/// <param name="FromDynamicCommand">Whether this was invoked from dynamic command.</param>
public record SwitchCommand(
    string Branch,
    TerminalMode? TerminalMode = null,
    string? InitScript = null,
    string? AfterInit = null,
    bool IgnoreSameSession = false,
    bool AutoConfirm = false,
    bool Debug = false,
    string? CustomScript = null,
    string? FromBranch = null,
    string? Dir = null,
    bool FromDynamicCommand = false
);

/// <summary>
/// Encapsulates all parameters for cleaning up worktrees.
/// </summary>
/// <param name="Mode">The cleanup mode.</param>
/// <param name="DryRun">Show what would be removed without actually removing.</param>
/// <param name="AutoConfirm">Auto-confirm all prompts.</param>
/// <param name="Force">Force remove worktrees with modified files.</param>
/// <param name="Debug">Enable debug logging.</param>
/// <param name="Worktrees">Branch names or paths to clean up.</param>
public record CleanupCommand(
    CleanupMode Mode,
    bool DryRun = false,
    bool AutoConfirm = false,
    bool Force = false,
    bool Debug = false,
    List<string>? Worktrees = null
);
