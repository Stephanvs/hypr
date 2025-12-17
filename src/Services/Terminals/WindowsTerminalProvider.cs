using System.Diagnostics;
using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Terminal provider for Windows Terminal (wt.exe).
/// </summary>
public class WindowsTerminalProvider : ITerminalProvider
{
    private readonly ILogger<WindowsTerminalProvider> _logger;

    public WindowsTerminalProvider(ILogger<WindowsTerminalProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "Windows Terminal";
    public Platform SupportedPlatforms => Platform.Windows;
    public int Priority => 100;

    public bool IsAvailable
    {
        get
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "wt",
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
            var psi = new ProcessStartInfo
            {
                FileName = "wt",
                UseShellExecute = false
            };

            var hasInitCommand = !string.IsNullOrEmpty(initCommand);

            if (hasInitCommand)
            {
                psi.ArgumentList.Add(mode == TerminalMode.Tab ? "new-tab" : "new-window");
                psi.ArgumentList.Add("-d");
                psi.ArgumentList.Add(workingDirectory);
                psi.ArgumentList.Add("powershell");
                psi.ArgumentList.Add("-NoExit");
                psi.ArgumentList.Add("-Command");
                psi.ArgumentList.Add(initCommand!);
            }
            else
            {
                psi.ArgumentList.Add(mode == TerminalMode.Tab ? "new-tab" : "new-window");
                psi.ArgumentList.Add("-d");
                psi.ArgumentList.Add(workingDirectory);
            }

            Process.Start(psi);
            _logger.LogInformation("Opened {Path} in Windows Terminal ({Mode})", workingDirectory, mode);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Windows Terminal");
            return false;
        }
    }
}
