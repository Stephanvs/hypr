using Microsoft.Extensions.Logging;
using Octokit;

namespace Hyprwt.Services;

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
            _client = new GitHubClient(new ProductHeaderValue("hyprwt"));
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
}
