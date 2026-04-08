using System.Diagnostics;
using Hypr.Configuration;
using Hypr.Utils;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Terminal provider for WezTerm.
/// Uses `wezterm cli spawn` for tabs when an existing client is reachable,
/// and falls back to opening a new window with `wezterm start`.
/// </summary>
public class WezTermProvider : ITerminalProvider
{
    private const int CommandTimeoutMs = 5000;

    private readonly ILogger<WezTermProvider> _logger;
    private readonly Lazy<WezTermCommand?> _command;

    public WezTermProvider(ILogger<WezTermProvider> logger)
    {
        _logger = logger;
        _command = new Lazy<WezTermCommand?>(ResolveLaunchCommand);
    }

    public string Name => "WezTerm";
    public Platform SupportedPlatforms => Platform.All;
    public int Priority => IsRunningInsideWezTerm() ? 125 : 40;
    public bool IsAvailable => _command.Value is not null;

    public bool SupportsMode(TerminalMode mode) =>
        mode is TerminalMode.Tab or TerminalMode.Window;

    public bool Open(string workingDirectory, TerminalMode mode, string? initCommand = null)
    {
        var command = _command.Value;
        if (command == null)
        {
            _logger.LogWarning("WezTerm is not available");
            return false;
        }

        try
        {
            if (mode == TerminalMode.Tab && TrySpawnTab(command, workingDirectory, initCommand))
            {
                _logger.LogInformation("Opened {Path} in WezTerm ({Mode})", workingDirectory, mode);
                return true;
            }

            if (!StartWindow(command, workingDirectory, initCommand))
            {
                return false;
            }

            if (mode == TerminalMode.Tab)
            {
                _logger.LogDebug("Fell back to opening a new WezTerm window because no existing client was available");
            }

            _logger.LogInformation(
                "Opened {Path} in WezTerm ({Mode})",
                workingDirectory,
                mode == TerminalMode.Tab ? TerminalMode.Window : mode);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open WezTerm");
            return false;
        }
    }

    private static bool IsRunningInsideWezTerm()
    {
        var paneId = Environment.GetEnvironmentVariable("WEZTERM_PANE");
        if (!string.IsNullOrWhiteSpace(paneId))
        {
            return true;
        }

        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        return string.Equals(termProgram, "WezTerm", StringComparison.OrdinalIgnoreCase);
    }

    private bool TrySpawnTab(WezTermCommand command, string workingDirectory, string? initCommand)
    {
        var psi = CreateProcessStartInfo(command, redirectOutput: true);
        psi.ArgumentList.Add("cli");
        psi.ArgumentList.Add("spawn");
        psi.ArgumentList.Add("--cwd");
        psi.ArgumentList.Add(workingDirectory);
        AddProgramArguments(psi, initCommand);

        return RunCommand(psi, "spawn a new tab");
    }

    private bool StartWindow(WezTermCommand command, string workingDirectory, string? initCommand)
    {
        var psi = CreateProcessStartInfo(command, redirectOutput: false);
        psi.ArgumentList.Add("start");
        psi.ArgumentList.Add("--cwd");
        psi.ArgumentList.Add(workingDirectory);
        AddProgramArguments(psi, initCommand);

        return Process.Start(psi) != null;
    }

    private bool RunCommand(ProcessStartInfo psi, string description)
    {
        using var process = Process.Start(psi);
        if (process == null)
        {
            return false;
        }

        if (!process.WaitForExit(CommandTimeoutMs))
        {
            TryKill(process);
            _logger.LogDebug("Timed out while asking WezTerm to {Description}", description);
            return false;
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (process.ExitCode == 0)
        {
            return true;
        }

        _logger.LogDebug(
            "WezTerm failed to {Description}. Exit code: {ExitCode}. Output: {Output}. Error: {Error}",
            description,
            process.ExitCode,
            output,
            error);
        return false;
    }

    private static void AddProgramArguments(ProcessStartInfo psi, string? initCommand)
    {
        if (string.IsNullOrWhiteSpace(initCommand))
        {
            return;
        }

        psi.ArgumentList.Add("--");

        if (PlatformUtils.IsWindows)
        {
            psi.ArgumentList.Add("powershell");
            psi.ArgumentList.Add("-NoExit");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add(initCommand);
            return;
        }

        psi.ArgumentList.Add("bash");
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add($"{initCommand}; exec \"${{SHELL:-bash}}\" -l");
    }

    private WezTermCommand? ResolveLaunchCommand()
    {
        foreach (var candidate in GetCommandCandidates())
        {
            if (!CanProbe(candidate))
            {
                continue;
            }

            if (Probe(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool CanProbe(WezTermCommand command)
    {
        return !Path.IsPathRooted(command.FileName) || File.Exists(command.FileName);
    }

    private bool Probe(WezTermCommand command)
    {
        try
        {
            var psi = CreateProcessStartInfo(command, redirectOutput: true);
            psi.ArgumentList.Add("--version");
            return RunCommand(psi, "report its version");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed probing WezTerm candidate {Candidate}", command.FileName);
            return false;
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(WezTermCommand command, bool redirectOutput)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command.FileName,
            UseShellExecute = false,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput,
        };

        foreach (var argument in command.PrefixArguments)
        {
            psi.ArgumentList.Add(argument);
        }

        return psi;
    }

    private static IEnumerable<WezTermCommand> GetCommandCandidates()
    {
        yield return new WezTermCommand("wezterm");

        if (PlatformUtils.IsWindows)
        {
            yield return new WezTermCommand("wezterm.exe");

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                yield return new WezTermCommand(Path.Combine(localAppData, "Programs", "WezTerm", "wezterm.exe"));
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                yield return new WezTermCommand(Path.Combine(programFiles, "WezTerm", "wezterm.exe"));
            }

            yield break;
        }

        if (PlatformUtils.IsMacOS)
        {
            yield return new WezTermCommand("/Applications/WezTerm.app/Contents/MacOS/wezterm");

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                yield return new WezTermCommand(Path.Combine(home, "Applications", "WezTerm.app", "Contents", "MacOS", "wezterm"));
            }

            yield break;
        }

        if (PlatformUtils.IsLinux)
        {
            yield return new WezTermCommand("/usr/bin/wezterm");
            yield return new WezTermCommand("/usr/local/bin/wezterm");
            yield return new WezTermCommand("flatpak", "run", "org.wezfurlong.wezterm");
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort cleanup for a timed out probe.
        }
    }

    private sealed record WezTermCommand(string FileName, params string[] PrefixArguments);
}
