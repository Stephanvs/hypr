namespace Hypr.Configuration;

/// <summary>
/// Configuration settings for worktree management.
/// </summary>
public class WorktreeConfig
{
    public string GetPath(string repoPath, string branch, DirectoryInfo? directory = null)
    {
        if (directory != null) return directory.FullName;
        var path = DirectoryPattern.Replace("{repo_name}", Path.GetFileName(repoPath)).Replace("{branch}", branch);
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(repoPath, path));
    }

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
