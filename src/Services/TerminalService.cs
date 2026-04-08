using Hypr.Configuration;
using Hypr.Services.Terminals;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hypr.Services;

/// <summary>
/// Terminal automation service.
/// Orchestrates terminal providers to open worktrees in various terminals and modes.
/// </summary>
public class TerminalService
{
    private static readonly Dictionary<string, string[]> ProviderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cursor"] = ["cursor"],
        ["Echo"] = ["echo"],
        ["GNOME Terminal"] = ["gnome", "gnome-terminal"],
        ["Inplace"] = ["inplace"],
        ["iTerm2"] = ["iterm", "iterm2"],
        ["Terminal.app"] = ["terminal", "terminalapp"],
        ["tmux"] = ["tmux"],
        ["VS Code"] = ["code", "vscode"],
        ["WezTerm"] = ["wez", "wezterm"],
        ["Windows Terminal"] = ["wt", "windows-terminal", "windowsterminal"],
    };

    private readonly ILogger<TerminalService> _logger;
    private readonly IEnumerable<ITerminalProvider> _providers;
    private readonly IOptionsMonitor<TerminalConfig> _terminalConfig;

    public TerminalService(
        ILogger<TerminalService> logger,
        IEnumerable<ITerminalProvider> providers,
        IOptionsMonitor<TerminalConfig> terminalConfig)
    {
        _logger = logger;
        _providers = providers;
        _terminalConfig = terminalConfig;
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
        var candidates = _providers
            .Where(p => p.SupportsMode(mode) && p.IsAvailable)
            .ToList();

        var configuredProgram = _terminalConfig.CurrentValue.Program;
        if (!string.IsNullOrWhiteSpace(configuredProgram))
        {
            var normalizedProgram = NormalizeProgramName(configuredProgram);
            var configuredProvider = candidates.FirstOrDefault(provider => MatchesConfiguredProgram(provider, normalizedProgram));

            if (configuredProvider == null)
            {
                _logger.LogWarning(
                    "Configured terminal program {Program} is not available or does not support mode {Mode}",
                    configuredProgram,
                    mode);
            }

            return configuredProvider;
        }

        return candidates
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();
    }

    private static bool MatchesConfiguredProgram(ITerminalProvider provider, string configuredProgram)
    {
        if (string.IsNullOrEmpty(configuredProgram))
        {
            return false;
        }

        return GetProgramIdentifiers(provider).Contains(configuredProgram, StringComparer.Ordinal);
    }

    private static IEnumerable<string> GetProgramIdentifiers(ITerminalProvider provider)
    {
        var normalizedName = NormalizeProgramName(provider.Name);
        if (!string.IsNullOrEmpty(normalizedName))
        {
            yield return normalizedName;
        }

        if (ProviderAliases.TryGetValue(provider.Name, out var aliases))
        {
            foreach (var alias in aliases)
            {
                var normalizedAlias = NormalizeProgramName(alias);
                if (!string.IsNullOrEmpty(normalizedAlias))
                {
                    yield return normalizedAlias;
                }
            }
        }
    }

    private static string NormalizeProgramName(string programName)
    {
        return new string(programName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
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
