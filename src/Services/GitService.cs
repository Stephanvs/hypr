using Hyprwt.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Hyprwt.Services;

/// <summary>
/// Git operations service for hyprwt.
/// Handles all git-related operations including worktrees, branches, and repository status.
/// </summary>
public class GitService(ILogger<GitService> logger)
{
    /// <summary>
    /// Finds the root directory of the git repository.
    /// </summary>
    /// <param name="startPath">Starting path (defaults to current directory).</param>
    /// <returns>Repository root path, or null if not in a git repo.</returns>
    public string? FindRepoRoot(string? startPath = null)
    {
        var path = startPath ?? Directory.GetCurrentDirectory();

        try
        {
            var repoPath = Repository.Discover(path);
            if (repoPath == null)
                return null;

            // Repository.Discover returns the .git directory, we want the parent
            var gitDir = new DirectoryInfo(repoPath);
            if (gitDir.Name == ".git")
                return gitDir.Parent?.FullName;

            // Handle bare repositories
            return gitDir.FullName;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find repository root");
            return null;
        }
    }

    /// <summary>
    /// Lists all worktrees in the repository.
    /// </summary>
    /// <param name="repoPath">Repository path.</param>
    /// <returns>List of worktree information.</returns>
    public List<WorktreeInfo> ListWorktrees(string repoPath)
    {
        var worktrees = new List<WorktreeInfo>();

        try
        {
            // Use git worktree list command since LibGit2Sharp doesn't have full worktree support
            var result = RunGitCommand(repoPath, "worktree", "list", "--porcelain");
            if (result.ExitCode != 0)
                return worktrees;

            var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            WorktreeInfo? current = null;
            string? currentPath = null;
            string? currentBranch = null;
            bool isPrimary = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("worktree "))
                {
                    // Save previous worktree if any
                    if (currentPath != null && currentBranch != null)
                    {
                        worktrees.Add(new WorktreeInfo(currentBranch, currentPath, IsCurrent: false, IsPrimary: isPrimary));
                    }

                    currentPath = line.Substring("worktree ".Length).Trim();
                    currentBranch = null;
                    isPrimary = false;
                }
                else if (line.StartsWith("branch "))
                {
                    var branchRef = line.Substring("branch ".Length).Trim();
                    currentBranch = branchRef.Replace("refs/heads/", "");
                }
                else if (line.StartsWith("bare"))
                {
                    isPrimary = true;
                    currentBranch = "main"; // Default for bare repo
                }
                else if (line == "")
                {
                    // Empty line separates worktrees
                    if (currentPath != null && currentBranch != null)
                    {
                        worktrees.Add(new WorktreeInfo(currentBranch, currentPath, IsCurrent: false, IsPrimary: isPrimary));
                    }
                    currentPath = null;
                    currentBranch = null;
                    isPrimary = false;
                }
            }

            // Don't forget the last worktree
            if (currentPath != null && currentBranch != null)
            {
                worktrees.Add(new WorktreeInfo(currentBranch, currentPath, IsCurrent: false, IsPrimary: isPrimary));
            }

            // Mark primary worktree
            if (worktrees.Count > 0)
            {
                worktrees[0] = worktrees[0] with { IsPrimary = true };
            }

            // Mark current worktree
            var cwd = Directory.GetCurrentDirectory();
            for (int i = 0; i < worktrees.Count; i++)
            {
                if (worktrees[i].Path == cwd)
                {
                    worktrees[i] = worktrees[i] with { IsCurrent = true };
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list worktrees");
        }

        return worktrees;
    }

    /// <summary>
    /// Creates a new worktree.
    /// </summary>
    /// <param name="repoPath">Repository path.</param>
    /// <param name="worktreePath">Path for the new worktree.</param>
    /// <param name="branch">Branch name.</param>
    /// <param name="fromBranch">Source branch/commit (optional).</param>
    /// <returns>True if successful.</returns>
    public bool CreateWorktree(string repoPath, string worktreePath, string branch, string? fromBranch = null)
    {
        try
        {
            List<string> args;

            // Check if branch exists locally
            var branchExistsLocally = BranchExistsLocally(repoPath, branch);

            if (branchExistsLocally)
            {
                // Checkout existing branch
                args = new List<string> { "worktree", "add", worktreePath, branch };
            }
            else if (fromBranch != null)
            {
                // Create new branch from specific start point
                args = new List<string> { "worktree", "add", worktreePath, "-b", branch, fromBranch };
            }
            else
            {
                // Check if branch exists remotely
                var remoteExists = BranchExistsRemotely(repoPath, branch);
                if (remoteExists)
                {
                    // Create tracking branch
                    args = new List<string> { "worktree", "add", "--track", "-b", branch, worktreePath, $"origin/{branch}" };
                }
                else
                {
                    // Create new branch from current HEAD
                    var defaultBranch = GetDefaultBranch(repoPath) ?? "main";
                    args = new List<string> { "worktree", "add", worktreePath, "-b", branch, defaultBranch };
                }
            }

            var result = RunGitCommand(repoPath, args.ToArray());
            if (result.ExitCode == 0)
            {
                logger.LogInformation("Created worktree at {Path} for branch {Branch}", worktreePath, branch);
                return true;
            }

            logger.LogError("Failed to create worktree: {Error}", result.Error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception creating worktree");
            return false;
        }
    }

    /// <summary>
    /// Removes a worktree.
    /// </summary>
    /// <param name="repoPath">Repository path.</param>
    /// <param name="worktreePath">Worktree path to remove.</param>
    /// <param name="force">Force removal even with uncommitted changes.</param>
    /// <returns>True if successful.</returns>
    public bool RemoveWorktree(string repoPath, string worktreePath, bool force = false)
    {
        try
        {
            var args = new List<string> { "worktree", "remove" };
            if (force)
                args.Add("--force");
            args.Add(worktreePath);

            var result = RunGitCommand(repoPath, args.ToArray());
            if (result.ExitCode == 0)
            {
                logger.LogInformation("Removed worktree at {Path}", worktreePath);
                return true;
            }

            logger.LogError("Failed to remove worktree: {Error}", result.Error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception removing worktree");
            return false;
        }
    }

    /// <summary>
    /// Gets the current branch name.
    /// </summary>
    /// <param name="repoPath">Repository path.</param>
    /// <returns>Current branch name, or null if detached HEAD.</returns>
    public string? GetCurrentBranch(string repoPath)
    {
        try
        {
            using var repo = new Repository(repoPath);
            if (repo.Info.IsHeadDetached)
                return null;
            return repo.Head.FriendlyName;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get current branch");
            return null;
        }
    }

    /// <summary>
    /// Gets the default branch (main or master).
    /// </summary>
    /// <param name="repoPath">Repository path.</param>
    /// <returns>Default branch name.</returns>
    public string? GetDefaultBranch(string repoPath)
    {
        try
        {
            using var repo = new Repository(repoPath);

            // Try common default branches
            if (repo.Branches["main"] != null)
                return "main";
            if (repo.Branches["master"] != null)
                return "master";

            // Try to get from remote
            var origin = repo.Network.Remotes["origin"];
            if (origin != null)
            {
                if (repo.Branches["origin/main"] != null)
                    return "main";
                if (repo.Branches["origin/master"] != null)
                    return "master";
            }

            // Fallback to first branch
            return repo.Branches.FirstOrDefault()?.FriendlyName;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get default branch");
            return "main";
        }
    }

    /// <summary>
    /// Checks if a branch exists locally.
    /// </summary>
    public bool BranchExistsLocally(string repoPath, string branch)
    {
        try
        {
            using var repo = new Repository(repoPath);
            return repo.Branches[branch] != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a branch exists on remote.
    /// </summary>
    public bool BranchExistsRemotely(string repoPath, string branch, string remote = "origin")
    {
        try
        {
            using var repo = new Repository(repoPath);
            return repo.Branches[$"{remote}/{branch}"] != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Fetches latest branches from remote.
    /// </summary>
    /// <param name="repoPath">Repository path.</param>
    /// <returns>True if successful.</returns>
    public bool FetchBranches(string repoPath)
    {
        try
        {
            var result = RunGitCommand(repoPath, "fetch", "origin");
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch branches");
            return false;
        }
    }

    /// <summary>
    /// Checks if a worktree has uncommitted changes.
    /// </summary>
    /// <param name="worktreePath">Worktree path.</param>
    /// <returns>True if there are uncommitted changes.</returns>
    public bool HasUncommittedChanges(string worktreePath)
    {
        try
        {
            using var repo = new Repository(worktreePath);
            var status = repo.RetrieveStatus();
            return status.IsDirty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check for uncommitted changes");
            return false;
        }
    }

    /// <summary>
    /// Gets branch status for cleanup decisions.
    /// </summary>
    /// <param name="repoPath">Repository path.</param>
    /// <param name="worktree">Worktree information.</param>
    /// <returns>Branch status.</returns>
    public BranchStatus GetBranchStatus(string repoPath, WorktreeInfo worktree)
    {
        try
        {
            using var repo = new Repository(repoPath);
            var branch = repo.Branches[worktree.Branch];

            if (branch == null)
            {
                return new BranchStatus(
                    worktree.Branch,
                    HasRemote: false,
                    IsMerged: false,
                    IsIdentical: false,
                    worktree.Path,
                    HasUncommittedChanges: false
                );
            }

            var hasRemote = branch.TrackedBranch != null;
            var hasUncommitted = HasUncommittedChanges(worktree.Path);

            // Check if merged
            var defaultBranch = GetDefaultBranch(repoPath);
            var isMerged = false;
            var isIdentical = false;

            if (defaultBranch != null)
            {
                var defaultBranchObj = repo.Branches[defaultBranch];
                if (defaultBranchObj != null && branch.Tip != null && defaultBranchObj.Tip != null)
                {
                    // Check if branch tip is reachable from default branch (merged)
                    var filter = new CommitFilter
                    {
                        IncludeReachableFrom = defaultBranchObj.Tip,
                        ExcludeReachableFrom = branch.Tip.Parents
                    };

                    var commits = repo.Commits.QueryBy(filter);
                    isMerged = commits.Any(c => c.Sha == branch.Tip.Sha);

                    // Check if identical (same commit)
                    isIdentical = branch.Tip.Sha == defaultBranchObj.Tip.Sha;
                }
            }

            return new BranchStatus(
                worktree.Branch,
                hasRemote,
                isMerged,
                isIdentical,
                worktree.Path,
                hasUncommitted
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get branch status for {Branch}", worktree.Branch);
            return new BranchStatus(
                worktree.Branch,
                HasRemote: false,
                IsMerged: false,
                IsIdentical: false,
                worktree.Path,
                HasUncommittedChanges: false
            );
        }
    }

    /// <summary>
    /// Runs a git command and returns the result.
    /// </summary>
    private (int ExitCode, string Output, string Error) RunGitCommand(string workingDir, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
                return (-1, "", "Failed to start process");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return (process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run git command");
            return (-1, "", ex.Message);
        }
    }
}
