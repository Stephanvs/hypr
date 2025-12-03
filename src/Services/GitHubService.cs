using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Hypr.Services;

public enum PrStatus
{
    Open,
    Closed,
    Merged,
    None
}

public record PrInfo(string State, string Number, string HeadRefName);

/// <summary>
/// JSON source generation context for GitHubService.
/// </summary>
[JsonSerializable(typeof(List<PrInfo>))]
internal partial class GitHubJsonContext : JsonSerializerContext
{
}

/// <summary>
/// GitHub API service for hyprwt.
/// Handles GitHub operations like fetching PR status.
/// </summary>
public class GitHubService(ILogger<GitHubService> logger)
{
    private GitHubClient? _client;

    /// <summary>
    /// Initializes the GitHub client with authentication.
    /// </summary>
    private void EnsureClient()
    {
        if (_client != null)
            return;

        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("GITHUB_TOKEN not set, GitHub features will be limited");
            _client = new GitHubClient(new ProductHeaderValue("hypr"));
            return;
        }

        _client = new GitHubClient(new ProductHeaderValue("hyprwt"))
        {
            Credentials = new Credentials(token)
        };

        logger.LogDebug("GitHub client initialized with authentication");
    }

    /// <summary>
    /// Checks if a pull request is open.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="prNumber">Pull request number.</param>
    /// <returns>True if the PR is open.</returns>
    public async Task<bool> IsPrOpen(string owner, string repo, int prNumber)
    {
        try
        {
            EnsureClient();
            var pr = await _client!.PullRequest.Get(owner, repo, prNumber);
            return pr.State == ItemState.Open;
        }
        catch (NotFoundException)
        {
            logger.LogWarning("Pull request #{PrNumber} not found", prNumber);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check PR status");
            return false;
        }
    }

    /// <summary>
    /// Gets the pull request for a specific branch.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="branch">Branch name.</param>
    /// <returns>PR number if found, otherwise null.</returns>
    public async Task<int?> GetPrForBranch(string owner, string repo, string branch)
    {
        try
        {
            EnsureClient();
            var request = new PullRequestRequest
            {
                State = ItemStateFilter.All,
                Head = $"{owner}:{branch}"
            };

            var prs = await _client!.PullRequest.GetAllForRepository(owner, repo, request);

            if (prs.Count == 0)
                return null;

            // Return the first PR (most recent)
            return prs[0].Number;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get PR for branch {Branch}", branch);
            return null;
        }
    }

    /// <summary>
    /// Gets the pull request information.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="prNumber">Pull request number.</param>
    /// <returns>Pull request information, or null if not found.</returns>
    public async Task<PullRequest?> GetPullRequest(string owner, string repo, int prNumber)
    {
        try
        {
            EnsureClient();
            return await _client!.PullRequest.Get(owner, repo, prNumber);
        }
        catch (NotFoundException)
        {
            logger.LogWarning("Pull request #{PrNumber} not found", prNumber);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get pull request");
            return null;
        }
    }

    /// <summary>
    /// Checks if a branch has a closed PR.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="branch">Branch name.</param>
    /// <returns>True if the branch has a closed PR.</returns>
    public async Task<bool> HasClosedPr(string owner, string repo, string branch)
    {
        try
        {
            var prNumber = await GetPrForBranch(owner, repo, branch);
            if (prNumber == null)
                return false;

            var isOpen = await IsPrOpen(owner, repo, prNumber.Value);
            return !isOpen;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check for closed PR");
            return false;
        }
    }

    public async Task<string?> GetGitHubUsername()
    {
        try
        {
            var (exitCode, stdout, _) = await RunProcessAsync(
              "gh",
              ["api", "user", "--jq", ".login"],
              Environment.CurrentDirectory,
              TimeSpan.FromSeconds(10));

            return exitCode == 0
              ? stdout.Trim()
              : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get GitHub username");
            return null;
        }
    }

    /// <summary>
    /// Checks if gh CLI is available and authenticated.
    /// </summary>
    /// <returns>True if gh CLI is available and authenticated.</returns>
    public async Task<bool> IsGitHubCliAvailable()
    {
        try
        {
            var (exitCode, stdout, stderr) = await RunProcessAsync(
                "gh", ["auth", "status"],
                Environment.CurrentDirectory,
                TimeSpan.FromSeconds(10));

            return exitCode == 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check gh CLI availability");
            return false;
        }
    }

    public async Task<PrStatus> GetPullRequestStatusForBranch(string repoPath, string branch)
    {
        var (exitCode, stdout, stderr) = await RunProcessAsync(
          "gh", ["pr", "list", "--head", branch, "--state", "all", "--json", "state,number,headRefName", "--limit", "10"],
          repoPath,
          TimeSpan.FromSeconds(30));

        if (exitCode != 0)
        {
            logger.LogError("Failed to get PRs for branch {Branch}: {Error}", branch, stderr);
            return PrStatus.None;
        }

        try
        {
            var prs = JsonSerializer.Deserialize<List<PrInfo>>(stdout, GitHubJsonContext.Default.Options);

            switch (prs)
            {
                case { Count: 0 }:
                    logger.LogDebug("No PRs found for branch {Branch}", branch);
                    return PrStatus.None;
                case null:
                    logger.LogDebug("Failed to deserialize PRs for branch {Branch}", branch);
                    return PrStatus.None;
            }

            foreach (var pr in prs)
            {
                if (pr?.State?.Equals("MERGED", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return PrStatus.Merged;
                }

                if (pr?.State?.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return PrStatus.Closed;
                }
            }

            return PrStatus.Open;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize PRs for branch {Branch}", branch);
            return PrStatus.None;
        }
    }


    /// <summary>
    /// Runs a process and captures its stdout and stderr.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="args">Arguments for the command.</param>
    /// <param name="repoRoot">The working directory for the process.</param>
    /// <param name="timeout">Optional timeout for the process.</param>
    /// <returns>A tuple containing exitCode, stdout and stderr.</returns>
    public static async Task<(int exitCode, string stdout, string stderr)> RunProcessAsync(string command, string[] args, string repoRoot, TimeSpan? timeout = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", args.Select(arg => $"\"{arg}\"")),
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (timeout.HasValue)
        {
            var exitTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(exitTask, Task.Delay(timeout.Value));

            if (completedTask != exitTask)
            {
                process.Kill();
                throw new TimeoutException("Timeout exceeded, process killed");
            }
        }
        else
        {
            await process.WaitForExitAsync();
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (process.ExitCode, stdout, stderr);
    }
}