using System.Diagnostics;
using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Terminal provider for iTerm2 on macOS.
/// </summary>
public class ITerm2Provider : ITerminalProvider
{
    private readonly ILogger<ITerm2Provider> _logger;

    public ITerm2Provider(ILogger<ITerm2Provider> logger)
    {
        _logger = logger;
    }

    public string Name => "iTerm2";
    public Platform SupportedPlatforms => Platform.MacOS;
    public int Priority => 100; // Higher priority than Terminal.app

    public bool IsAvailable
    {
        get
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = "-e 'tell application \"System Events\" to (name of processes) contains \"iTerm2\"'",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                var process = Process.Start(psi);
                if (process == null) return false;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Also check if iTerm2 is installed
                return Directory.Exists("/Applications/iTerm.app") || output.Contains("true");
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
            var escapedCommand = command.Replace("\"", "\\\"");

            var script = mode == TerminalMode.Window
                ? $@"tell application ""iTerm""
    create window with default profile
    tell current session of current window
        write text ""{escapedCommand}""
    end tell
end tell"
                : $@"tell application ""iTerm""
    tell current window
        create tab with default profile
        tell current session
            write text ""{escapedCommand}""
        end tell
    end tell
end tell";

            var success = RunAppleScript(script);
            if (success)
            {
                _logger.LogInformation("Opened {Path} in iTerm2 ({Mode})", workingDirectory, mode);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open iTerm2");
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
