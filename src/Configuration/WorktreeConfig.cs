namespace Hypr.Configuration;

/// <summary>
/// Configuration settings for worktree management.
/// </summary>
public class WorktreeConfig
{
    /// <summary>
    /// Path pattern for worktrees.
    /// </summary>
    public string DirectoryPattern { get; set; } = "../{repo_name}-worktrees/{branch}";

    /// <summary>
    /// Auto-fetch on operations.
    /// </summary>
    public bool AutoFetch { get; set; } = true;

    /// <summary>
    /// Branch prefix with interpolation.
    /// </summary>
    public string? BranchPrefix { get; set; }
}
