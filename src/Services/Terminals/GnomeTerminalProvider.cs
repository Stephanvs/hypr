using System.Diagnostics;
using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Terminal provider for GNOME Terminal on Linux.
/// </summary>
public class GnomeTerminalProvider : ITerminalProvider
{
    private readonly ILogger<GnomeTerminalProvider> _logger;

    public GnomeTerminalProvider(ILogger<GnomeTerminalProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "GNOME Terminal";
    public Platform SupportedPlatforms => Platform.Linux;
    public int Priority => 50;

    public bool IsAvailable
    {
        get
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "gnome-terminal",
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
            var flag = mode == TerminalMode.Window ? "--window" : "--tab";

            var psi = new ProcessStartInfo
            {
                FileName = "gnome-terminal",
                ArgumentList = { flag, "--working-directory", workingDirectory, "--", "bash", "-c", command },
                UseShellExecute = false
            };

            Process.Start(psi);
            _logger.LogInformation("Opened {Path} in GNOME Terminal ({Mode})", workingDirectory, mode);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open GNOME Terminal");
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
