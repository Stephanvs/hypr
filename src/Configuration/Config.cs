using Hyprwt.Models;

namespace Hyprwt.Configuration;

/// <summary>
/// Terminal management configuration.
/// </summary>
/// <param name="Mode">Terminal mode (tab/window/inplace/echo/vscode/cursor).</param>
/// <param name="AlwaysNew">Always create new session.</param>
/// <param name="Program">Custom terminal program.</param>
public record TerminalConfig(
    TerminalMode Mode = TerminalMode.Tab,
    bool AlwaysNew = false,
    string? Program = null
);

/// <summary>
/// Worktree management configuration.
/// </summary>
/// <param name="DirectoryPattern">Path pattern for worktrees.</param>
/// <param name="AutoFetch">Auto-fetch on operations.</param>
/// <param name="BranchPrefix">Branch prefix with interpolation.</param>
public record WorktreeConfig(
    string DirectoryPattern = "../{repo_name}-worktrees/{branch}",
    bool AutoFetch = true,
    string? BranchPrefix = null
);

/// <summary>
/// Cleanup behavior configuration.
/// </summary>
/// <param name="DefaultMode">Default cleanup mode.</param>
public record CleanupConfig(
    CleanupMode DefaultMode = CleanupMode.Interactive
);

/// <summary>
/// Lifecycle scripts and custom commands.
/// </summary>
/// <param name="PreCreate">Script before worktree creation.</param>
/// <param name="PostCreate">Script after worktree creation.</param>
/// <param name="PostCreateAsync">Async script after worktree creation.</param>
/// <param name="SessionInit">Script after terminal session starts.</param>
/// <param name="PreCleanup">Script before cleanup.</param>
/// <param name="PostCleanup">Script after cleanup.</param>
/// <param name="PreSwitch">Script before switching.</param>
/// <param name="PostSwitch">Script after switching.</param>
/// <param name="Custom">Custom scripts dictionary.</param>
public record ScriptsConfig(
    string? PreCreate = null,
    string? PostCreate = null,
    string? PostCreateAsync = null,
    string? SessionInit = null,
    string? PreCleanup = null,
    string? PostCleanup = null,
    string? PreSwitch = null,
    string? PostSwitch = null,
    Dictionary<string, string>? Custom = null
);

/// <summary>
/// User confirmation settings.
/// </summary>
/// <param name="CleanupMultiple">Confirm multiple cleanups.</param>
/// <param name="ForceOperations">Confirm force operations.</param>
public record ConfirmationsConfig(
    bool CleanupMultiple = true,
    bool ForceOperations = true
);

/// <summary>
/// Complete hyprwt configuration.
/// </summary>
public record Config
{
    public TerminalConfig Terminal { get; init; }
    public WorktreeConfig Worktree { get; init; }
    public CleanupConfig Cleanup { get; init; }
    public ScriptsConfig Scripts { get; init; }
    public ConfirmationsConfig Confirmations { get; init; }
    
    public Config() 
    {
        Terminal = new TerminalConfig();
        Worktree = new WorktreeConfig();
        Cleanup = new CleanupConfig();
        Scripts = new ScriptsConfig();
        Confirmations = new ConfirmationsConfig();
    }
    
    public Config(
        TerminalConfig? terminal = null,
        WorktreeConfig? worktree = null,
        CleanupConfig? cleanup = null,
        ScriptsConfig? scripts = null,
        ConfirmationsConfig? confirmations = null
    )
    {
        Terminal = terminal ?? new TerminalConfig();
        Worktree = worktree ?? new WorktreeConfig();
        Cleanup = cleanup ?? new CleanupConfig();
        Scripts = scripts ?? new ScriptsConfig();
        Confirmations = confirmations ?? new ConfirmationsConfig();
    }
}
