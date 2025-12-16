using System.Diagnostics;
using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Terminal provider for macOS Terminal.app.
/// </summary>
public class TerminalAppProvider : ITerminalProvider
{
    private readonly ILogger<TerminalAppProvider> _logger;

    public TerminalAppProvider(ILogger<TerminalAppProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "Terminal.app";
    public Platform SupportedPlatforms => Platform.MacOS;
    public int Priority => 50; // Lower priority than iTerm2

    public bool IsAvailable => true; // Terminal.app is always available on macOS

    public bool SupportsMode(TerminalMode mode) =>
        mode is TerminalMode.Tab or TerminalMode.Window;

    public bool Open(string workingDirectory, TerminalMode mode, string? initCommand = null)
    {
        try
        {
            var command = BuildCommand(workingDirectory, initCommand);
            var escapedCommand = command.Replace("\"", "\\\"");

            var script = $@"tell application ""Terminal""
    do script ""{escapedCommand}""
    activate
end tell";

            var success = RunAppleScript(script);
            if (success)
            {
                _logger.LogInformation("Opened {Path} in Terminal.app ({Mode})", workingDirectory, mode);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Terminal.app");
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

    private bool RunAppleScript(string script)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"{script.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(psi);
            if (process == null) return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run AppleScript");
            return false;
        }
    }
}
