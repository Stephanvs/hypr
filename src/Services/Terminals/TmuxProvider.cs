using System.Diagnostics;
using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Terminal provider for tmux (works when inside a tmux session).
/// </summary>
public class TmuxProvider : ITerminalProvider
{
    private readonly ILogger<TmuxProvider> _logger;

    public TmuxProvider(ILogger<TmuxProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "tmux";
    public Platform SupportedPlatforms => Platform.Linux | Platform.MacOS;
    public int Priority => 150; // High priority when in tmux session

    public bool IsAvailable
    {
        get
        {
            // Only available if we're inside a tmux session
            var tmuxEnv = Environment.GetEnvironmentVariable("TMUX");
            if (string.IsNullOrEmpty(tmuxEnv))
                return false;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "tmux",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                var process = Process.Start(psi);
                if (process == null) return false;

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool SupportsMode(TerminalMode mode) =>
        mode is TerminalMode.Tab or TerminalMode.Window;

    public bool Open(string workingDirectory, TerminalMode mode, string? initCommand = null)
    {
        try
        {
            var command = BuildCommand(workingDirectory, initCommand);

            var psi = new ProcessStartInfo
            {
                FileName = "tmux",
                ArgumentList = { "new-window", "-c", workingDirectory, command },
                UseShellExecute = false
            };

            Process.Start(psi);
            _logger.LogInformation("Opened {Path} in tmux ({Mode})", workingDirectory, mode);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open tmux window");
            return false;
        }
    }

    private static string BuildCommand(string workingDirectory, string? initCommand)
    {
        var parts = new List<string> { $"cd '{workingDirectory}'" };

        if (!string.IsNullOrEmpty(initCommand))
        {
            parts.Add(initCommand);
        }

        parts.Add("exec $SHELL");
        return string.Join(" && ", parts);
    }
}
