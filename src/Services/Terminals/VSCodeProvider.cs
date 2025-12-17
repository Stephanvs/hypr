using System.Diagnostics;
using Hypr.Configuration;
using Microsoft.Extensions.Logging;

namespace Hypr.Services.Terminals;

/// <summary>
/// Terminal provider for Visual Studio Code.
/// </summary>
public class VSCodeProvider : ITerminalProvider
{
    private readonly ILogger<VSCodeProvider> _logger;

    public VSCodeProvider(ILogger<VSCodeProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "VS Code";
    public Platform SupportedPlatforms => Platform.All;
    public int Priority => 100;

    public bool IsAvailable
    {
        get
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Utils.PlatformUtils.IsWindows ? "where" : "which",
                    Arguments = "code",
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
        mode == TerminalMode.VSCode;

    public bool Open(string workingDirectory, TerminalMode mode, string? initCommand = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "code",
                ArgumentList = { workingDirectory },
                UseShellExecute = false
            };

            Process.Start(psi);
            _logger.LogInformation("Opened {Path} in VS Code", workingDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open VS Code");
            return false;
        }
    }
}
