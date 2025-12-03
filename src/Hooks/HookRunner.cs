using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Hypr.Hooks;

/// <summary>
/// Runs lifecycle hooks and custom scripts.
/// </summary>
public class HookRunner(ILogger<HookRunner> logger)
{
    /// <summary>
    /// Runs a hook script if it's defined.
    /// </summary>
    /// <param name="hookScript">The script to run.</param>
    /// <param name="workingDirectory">Working directory for the script.</param>
    /// <param name="async">Whether to run asynchronously (fire and forget).</param>
    /// <param name="timeout">Timeout in seconds (0 for no timeout).</param>
    /// <returns>True if script ran successfully (or was skipped), false on error.</returns>
    public bool RunHook(string? hookScript, string workingDirectory, bool async = false, int timeout = 30)
    {
        if (string.IsNullOrWhiteSpace(hookScript))
        {
            logger.LogDebug("No hook script defined, skipping");
            return true;
        }

        try
        {
            logger.LogInformation("Running hook: {Script}", hookScript);

            if (async)
            {
                // Fire and forget
                _ = Task.Run(() => ExecuteScript(hookScript, workingDirectory, timeout));
                return true;
            }
            else
            {
                // Wait for completion
                return ExecuteScript(hookScript, workingDirectory, timeout);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run hook script");
            return false;
        }
    }

    /// <summary>
    /// Runs a custom script with argument interpolation.
    /// </summary>
    /// <param name="scriptTemplate">Script template with placeholders.</param>
    /// <param name="workingDirectory">Working directory.</param>
    /// <param name="args">Arguments to interpolate.</param>
    /// <returns>True if successful.</returns>
    public bool RunCustomScript(string scriptTemplate, string workingDirectory, Dictionary<string, string>? args = null)
    {
        try
        {
            var script = scriptTemplate;

            // Interpolate arguments if provided
            if (args != null)
            {
                foreach (var kvp in args)
                {
                    script = script.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            logger.LogInformation("Running custom script: {Script}", script);
            return ExecuteScript(script, workingDirectory, 60);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run custom script");
            return false;
        }
    }

    private bool ExecuteScript(string script, string workingDirectory, int timeoutSeconds)
    {
        var shell = GetShell();
        var shellArg = GetShellArg();

        var psi = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = $"{shellArg} \"{script}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            logger.LogError("Failed to start process");
            return false;
        }

        bool completed;
        if (timeoutSeconds > 0)
        {
            completed = process.WaitForExit(timeoutSeconds * 1000);
        }
        else
        {
            process.WaitForExit();
            completed = true;
        }

        if (!completed)
        {
            logger.LogWarning("Script timed out after {Timeout}s", timeoutSeconds);
            process.Kill();
            return false;
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (process.ExitCode != 0)
        {
            logger.LogError("Script failed with exit code {ExitCode}", process.ExitCode);
            if (!string.IsNullOrEmpty(error))
            {
                logger.LogError("Error output: {Error}", error);
            }
            return false;
        }

        if (!string.IsNullOrEmpty(output))
        {
            logger.LogDebug("Script output: {Output}", output);
        }

        return true;
    }

    private static string GetShell()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "cmd.exe"
            : "/bin/bash";
    }

    private static string GetShellArg()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "/c"
            : "-c";
    }
}
