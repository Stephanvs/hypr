using Hypr.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Hypr.Configuration;

namespace Hypr.Services;

/// <summary>
/// Terminal automation service.
/// Handles opening new terminal sessions in various terminals and modes.
/// </summary>
public class TerminalService
{
    private readonly ILogger<TerminalService> _logger;
    private readonly StateService _stateService;

    public TerminalService(ILogger<TerminalService> logger, StateService stateService)
    {
        _logger = logger;
        _stateService = stateService;
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

        return mode switch
        {
            TerminalMode.Echo => HandleEchoMode(worktreePath),
            TerminalMode.Inplace => HandleInplaceMode(worktreePath),
            TerminalMode.Tab => HandleTabMode(worktreePath, sessionInit, afterInit),
            TerminalMode.Window => HandleWindowMode(worktreePath, sessionInit, afterInit),
            TerminalMode.VSCode => HandleVSCodeMode(worktreePath),
            TerminalMode.Cursor => HandleCursorMode(worktreePath),
            _ => false
        };
    }

    /// <summary>
    /// Echo mode - just print the cd command.
    /// </summary>
    private bool HandleEchoMode(string worktreePath)
    {
        Console.WriteLine($"cd {worktreePath}");
        return true;
    }

    /// <summary>
    /// Inplace mode - change directory in current session.
    /// This only works if called via eval or source.
    /// </summary>
    private bool HandleInplaceMode(string worktreePath)
    {
        try
        {
            Directory.SetCurrentDirectory(worktreePath);
            Console.WriteLine($"Changed directory to {worktreePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change directory");
            return false;
        }
    }

    /// <summary>
    /// Tab mode - open in new tab.
    /// </summary>
    private bool HandleTabMode(string worktreePath, string? sessionInit, string? afterInit)
    {
        if (PlatformUtils.IsMacOS)
        {
            return OpenInMacOSTerminal(worktreePath, newWindow: false, sessionInit, afterInit);
        }
        else if (PlatformUtils.IsLinux)
        {
            return OpenInLinuxTerminal(worktreePath, newWindow: false, sessionInit, afterInit);
        }
        else if (PlatformUtils.IsWindows)
        {
            return OpenInWindowsTerminal(worktreePath, newTab: true, sessionInit, afterInit);
        }

        _logger.LogWarning("Tab mode not supported on this platform");
        return HandleEchoMode(worktreePath);
    }

    /// <summary>
    /// Window mode - open in new window.
    /// </summary>
    private bool HandleWindowMode(string worktreePath, string? sessionInit, string? afterInit)
    {
        if (PlatformUtils.IsMacOS)
        {
            return OpenInMacOSTerminal(worktreePath, newWindow: true, sessionInit, afterInit);
        }
        else if (PlatformUtils.IsLinux)
        {
            return OpenInLinuxTerminal(worktreePath, newWindow: true, sessionInit, afterInit);
        }
        else if (PlatformUtils.IsWindows)
        {
            return OpenInWindowsTerminal(worktreePath, newTab: false, sessionInit, afterInit);
        }

        _logger.LogWarning("Window mode not supported on this platform");
        return HandleEchoMode(worktreePath);
    }

    /// <summary>
    /// VSCode mode - open in VSCode.
    /// </summary>
    private bool HandleVSCodeMode(string worktreePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "code",
                ArgumentList = { worktreePath },
                UseShellExecute = false
            };

            Process.Start(psi);
            _logger.LogInformation("Opened {Path} in VSCode", worktreePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open VSCode");
            return false;
        }
    }

    /// <summary>
    /// Cursor mode - open in Cursor editor.
    /// </summary>
    private bool HandleCursorMode(string worktreePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cursor",
                ArgumentList = { worktreePath },
                UseShellExecute = false
            };

            Process.Start(psi);
            _logger.LogInformation("Opened {Path} in Cursor", worktreePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Cursor");
            return false;
        }
    }

    /// <summary>
    /// Opens in macOS terminal (iTerm2 or Terminal.app).
    /// </summary>
    private bool OpenInMacOSTerminal(string worktreePath, bool newWindow, string? sessionInit, string? afterInit)
    {
        try
        {
            // Try iTerm2 first
            if (IsITerm2Available())
            {
                return OpenInITerm2(worktreePath, newWindow, sessionInit, afterInit);
            }

            // Fallback to Terminal.app
            return OpenInTerminalApp(worktreePath, newWindow, sessionInit, afterInit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open macOS terminal");
            return false;
        }
    }

    /// <summary>
    /// Checks if iTerm2 is available.
    /// </summary>
    private bool IsITerm2Available()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = "-e 'application \"iTerm\" is running'",
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

    /// <summary>
    /// Opens in iTerm2 using AppleScript.
    /// </summary>
    private bool OpenInITerm2(string worktreePath, bool newWindow, string? sessionInit, string? afterInit)
    {
        var command = BuildShellCommand(worktreePath, sessionInit, afterInit);
        var escapedCommand = command.Replace("\"", "\\\"");

        var script = newWindow
            ? $@"tell application ""iTerm""
    create window with default profile
    tell current session of current window
        write text ""cd '{worktreePath}' && {escapedCommand}""
    end tell
end tell"
            : $@"tell application ""iTerm""
    tell current window
        create tab with default profile
        tell current session
            write text ""cd '{worktreePath}' && {escapedCommand}""
        end tell
    end tell
end tell";

        return RunAppleScript(script);
    }

    /// <summary>
    /// Opens in Terminal.app using AppleScript.
    /// </summary>
    private bool OpenInTerminalApp(string worktreePath, bool newWindow, string? sessionInit, string? afterInit)
    {
        var command = BuildShellCommand(worktreePath, sessionInit, afterInit);
        var escapedCommand = command.Replace("\"", "\\\"");

        var script = $@"tell application ""Terminal""
    do script ""cd '{worktreePath}' && {escapedCommand}""
    activate
end tell";

        return RunAppleScript(script);
    }

    /// <summary>
    /// Runs an AppleScript command.
    /// </summary>
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

    /// <summary>
    /// Opens in Linux terminal (tries tmux, gnome-terminal, etc.).
    /// </summary>
    private bool OpenInLinuxTerminal(string worktreePath, bool newWindow, string? sessionInit, string? afterInit)
    {
        // Try tmux first (most common in development)
        if (IsTmuxAvailable())
        {
            return OpenInTmux(worktreePath, sessionInit, afterInit);
        }

        // Try gnome-terminal
        if (IsCommandAvailable("gnome-terminal"))
        {
            return OpenInGnomeTerminal(worktreePath, newWindow, sessionInit, afterInit);
        }

        // Fallback to xterm
        _logger.LogWarning("No supported terminal found, falling back to echo mode");
        return HandleEchoMode(worktreePath);
    }

    /// <summary>
    /// Checks if tmux is available and we're in a tmux session.
    /// </summary>
    private bool IsTmuxAvailable()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TMUX")) &&
               IsCommandAvailable("tmux");
    }

    /// <summary>
    /// Opens in tmux.
    /// </summary>
    private bool OpenInTmux(string worktreePath, string? sessionInit, string? afterInit)
    {
        try
        {
            var command = BuildShellCommand(worktreePath, sessionInit, afterInit);

            var psi = new ProcessStartInfo
            {
                FileName = "tmux",
                ArgumentList = { "new-window", "-c", worktreePath, command },
                UseShellExecute = false
            };

            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open tmux window");
            return false;
        }
    }

    /// <summary>
    /// Opens in GNOME Terminal.
    /// </summary>
    private bool OpenInGnomeTerminal(string worktreePath, bool newWindow, string? sessionInit, string? afterInit)
    {
        try
        {
            var command = BuildShellCommand(worktreePath, sessionInit, afterInit);
            var flag = newWindow ? "--window" : "--tab";

            var psi = new ProcessStartInfo
            {
                FileName = "gnome-terminal",
                ArgumentList = { flag, "--working-directory", worktreePath, "--", "bash", "-c", command },
                UseShellExecute = false
            };

            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open GNOME Terminal");
            return false;
        }
    }

    /// <summary>
    /// Opens in Windows Terminal.
    /// </summary>
    private bool OpenInWindowsTerminal(string worktreePath, bool newTab, string? sessionInit, string? afterInit)
    {
        try
        {
            var command = BuildShellCommand(worktreePath, sessionInit, afterInit);

            var psi = new ProcessStartInfo
            {
                FileName = "wt",
                ArgumentList = { newTab ? "new-tab" : "new-window", "-d", worktreePath },
                UseShellExecute = false
            };

            if (!string.IsNullOrEmpty(command))
            {
                psi.ArgumentList.Add(command);
            }

            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Windows Terminal");
            return false;
        }
    }

    /// <summary>
    /// Builds the shell command to execute in the new terminal.
    /// </summary>
    private string BuildShellCommand(string worktreePath, string? sessionInit, string? afterInit)
    {
        var commands = new List<string>();

        // Always cd to worktree
        commands.Add($"cd '{worktreePath}'");

        // Run session init script if provided
        if (!string.IsNullOrEmpty(sessionInit))
        {
            commands.Add(sessionInit);
        }

        // Run after init command if provided
        if (!string.IsNullOrEmpty(afterInit))
        {
            commands.Add(afterInit);
        }

        // Start interactive shell
        commands.Add("exec $SHELL");

        return string.Join(" && ", commands);
    }

    /// <summary>
    /// Checks if a command is available in PATH.
    /// </summary>
    private bool IsCommandAvailable(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = PlatformUtils.IsWindows ? "where" : "which",
                Arguments = command,
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
