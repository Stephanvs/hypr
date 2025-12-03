namespace Hypr.Configuration;

/// <summary>
/// Configuration settings for lifecycle scripts and custom commands.
/// </summary>
public class ScriptsConfig
{
    /// <summary>
    /// Script before worktree creation.
    /// </summary>
    public string? PreCreate { get; set; }

    /// <summary>
    /// Script after worktree creation.
    /// </summary>
    public string? PostCreate { get; set; }

    /// <summary>
    /// Async script after worktree creation.
    /// </summary>
    public string? PostCreateAsync { get; set; }

    /// <summary>
    /// Script after terminal session starts.
    /// </summary>
    public string? SessionInit { get; set; }

    /// <summary>
    /// Script before cleanup.
    /// </summary>
    public string? PreCleanup { get; set; }

    /// <summary>
    /// Script after cleanup.
    /// </summary>
    public string? PostCleanup { get; set; }

    /// <summary>
    /// Script before switching.
    /// </summary>
    public string? PreSwitch { get; set; }

    /// <summary>
    /// Script after switching.
    /// </summary>
    public string? PostSwitch { get; set; }

    /// <summary>
    /// Custom scripts dictionary.
    /// </summary>
    public Dictionary<string, string>? Custom { get; set; }
}
