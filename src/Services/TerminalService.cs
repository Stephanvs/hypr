using Hypr.Configuration;
using Hypr.Services.Terminals;
using Microsoft.Extensions.Logging;

namespace Hypr.Services;

/// <summary>
/// Terminal automation service.
/// Orchestrates terminal providers to open worktrees in various terminals and modes.
/// </summary>
public class TerminalService
{
    private readonly ILogger<TerminalService> _logger;
    private readonly IEnumerable<ITerminalProvider> _providers;

    public TerminalService(ILogger<TerminalService> logger, IEnumerable<ITerminalProvider> providers)
    {
        _logger = logger;
        _providers = providers;
    }

    /// <summary>
    /// Opens a worktree in a terminal according to the specified mode.
    /// </summary>
    /// <param name="worktreePath">Path to the worktree.</param>
    /// <param name="mode">Terminal mode.</param>
    /// <param name="sessionInit">Optional init script to run.</param>
    /// <param name="afterInit">Command to run after init script.</param>
    /// <returns>True if successful.</returns>
    public bool OpenWorktree(string worktreePath, TerminalMode mode, string? sessionInit = null, string? afterInit = null)
    {
        _logger.LogInformation("Opening worktree at {Path} with mode {Mode}", worktreePath, mode);

        var provider = SelectProvider(mode);

        if (provider == null)
        {
            _logger.LogWarning("No terminal provider available for mode {Mode}", mode);
            return false;
        }

        _logger.LogDebug("Selected provider: {Provider}", provider.Name);

        var initCommand = BuildInitCommand(sessionInit, afterInit);
        return provider.Open(worktreePath, mode, initCommand);
    }

    /// <summary>
    /// Selects the best available provider for the given mode.
    /// </summary>
    private ITerminalProvider? SelectProvider(TerminalMode mode)
    {
        return _providers
            .Where(p => p.SupportsMode(mode) && p.IsAvailable)
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();
    }

    /// <summary>
    /// Builds the init command from session init and after init scripts.
    /// </summary>
    private static string? BuildInitCommand(string? sessionInit, string? afterInit)
    {
        var commands = new List<string>();

        if (!string.IsNullOrEmpty(sessionInit))
        {
            commands.Add(sessionInit);
        }

        if (!string.IsNullOrEmpty(afterInit))
        {
            commands.Add(afterInit);
        }

        return commands.Count > 0 ? string.Join("; ", commands) : null;
    }
}
