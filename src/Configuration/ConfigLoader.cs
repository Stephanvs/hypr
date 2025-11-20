using Hyprwt.Models;
using Microsoft.Extensions.Logging;
using Tomlyn;
using Tomlyn.Model;

namespace Hyprwt.Configuration;

/// <summary>
/// Handles loading and merging configuration from multiple sources.
/// Implements 5-level cascade: defaults → global file → project file → env vars → CLI overrides
/// </summary>
public class ConfigLoader
{
    private readonly ILogger<ConfigLoader> _logger;
    private readonly string _appDir;
    private readonly string _globalConfigFile;
    private bool _setupDone;

    public ConfigLoader(ILogger<ConfigLoader> logger, string? appDir = null)
    {
        _logger = logger;
        _appDir = appDir ?? PathProvider.GetDefaultConfigDirectory();
        _globalConfigFile = Path.Combine(_appDir, "config.toml");
        _setupDone = false;
        _logger.LogDebug("Config loader initialized with app dir: {AppDir}", _appDir);
    }

    /// <summary>
    /// Ensure app directory exists. Called lazily when needed.
    /// </summary>
    public void Setup()
    {
        if (_setupDone)
        {
            return;
        }

        Directory.CreateDirectory(_appDir);
        _setupDone = true;
        _logger.LogDebug("Config loader setup complete: {AppDir}", _appDir);
    }

    /// <summary>
    /// Load configuration with proper precedence order.
    /// Precedence (later overrides earlier):
    /// 1. Built-in defaults
    /// 2. Global config file
    /// 3. Project config file
    /// 4. Environment variables
    /// 5. CLI overrides
    /// </summary>
    public Config LoadConfig(string? projectDir = null, Dictionary<string, object>? cliOverrides = null)
    {
        _logger.LogDebug("Loading configuration with cascading precedence");

        // 1. Start with defaults (built into Config record)
        var config = new Config();

        // 2. Load global config file
        var globalData = LoadGlobalConfig();
        config = MergeConfigs(config, globalData);

        // 3. Load project config file
        if (projectDir is not null)
        {
            var projectData = LoadProjectConfig(projectDir);
            config = MergeConfigs(config, projectData);
        }

        // 4. Apply environment variables
        var envData = LoadEnvironmentVariables();
        config = MergeConfigs(config, envData);

        // 5. Apply CLI overrides
        if (cliOverrides is { Count: > 0 })
        {
            config = ApplyCliOverrides(config, cliOverrides);
        }

        return config;
    }

    /// <summary>
    /// Load only global configuration without project config or other overrides.
    /// </summary>
    public Config LoadGlobalConfigOnly()
    {
        _logger.LogDebug("Loading global configuration only (no project config)");
        var config = new Config();
        var globalData = LoadGlobalConfig();
        return MergeConfigs(config, globalData);
    }

    private Config LoadGlobalConfig()
    {
        _logger.LogDebug("Looking for global config file at: {ConfigFile}", _globalConfigFile);
        Console.WriteLine($"Looking for global config file at: {_globalConfigFile}");
        
        if (!File.Exists(_globalConfigFile))
        {
            _logger.LogDebug("No global config file found");
            Console.WriteLine("No global config file found");
            return new Config();
        }

        try
        {
            var tomlContent = File.ReadAllText(_globalConfigFile);
            _logger.LogDebug("Global config file content: {Content}", tomlContent);
            Console.WriteLine($"Global config file content: {tomlContent}");
            var tomlTable = Toml.ToModel(tomlContent);
            var config = ParseTomlToConfig(tomlTable);
            _logger.LogDebug("Global configuration loaded successfully - Terminal Mode: {Mode}", config.Terminal.Mode);
            Console.WriteLine($"Parsed config - Terminal Mode: {config.Terminal.Mode}");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load global configuration");
            Console.WriteLine($"Failed to load global configuration: {ex.Message}");
            return new Config();
        }
    }

    private Config LoadProjectConfig(string projectDir)
    {
        var configFiles = new[] {
            Path.Combine(projectDir, "hyprwt.toml"),
            Path.Combine(projectDir, ".hyprwt.toml")
        };

        foreach (var configFile in configFiles)
        {
            if (File.Exists(configFile))
            {
                _logger.LogDebug("Found project config file: {ConfigFile}", configFile);
                try
                {
                    var tomlContent = File.ReadAllText(configFile);
                    var tomlTable = Toml.ToModel(tomlContent);
                    _logger.LogDebug("Project configuration loaded successfully");
                    return ParseTomlToConfig(tomlTable);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load project configuration from {ConfigFile}", configFile);
                    continue;
                }
            }
        }

        _logger.LogDebug("No project config file found");
        return new Config();
    }

    private Config LoadEnvironmentVariables()
    {
        var config = new Config();
        var envVars = Environment.GetEnvironmentVariables();
        var updates = new Dictionary<string, object>();

        foreach (var key in envVars.Keys)
        {
            var keyStr = key?.ToString();

            // Support HYPRWT_
            if (keyStr == null || !keyStr.StartsWith("HYPRWT_"))
                continue;

            var suffix = string.Empty;
            if (keyStr.StartsWith("HYPRWT_"))
                suffix = keyStr[7..]; // Remove "HYPRWT_"

            var value = envVars[key]?.ToString();
            if (value == null) continue;

            // Map environment variables to config paths
            var converted = ConvertEnvValue(value);
            updates[suffix] = converted;
        }

        if (updates.Count > 0)
        {
            _logger.LogDebug("Loaded configuration from {Count} environment variables", updates.Count);
            config = ApplyEnvironmentOverrides(config, updates);
        }

        return config;
    }

    private object ConvertEnvValue(string value)
    {
        // Boolean conversion
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            value == "1" ||
            value.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            value == "0" ||
            value.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Integer conversion
        if (int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        // Return as string
        return value;
    }

    private Config ParseTomlToConfig(TomlTable tomlTable)
    {
        var terminal = ParseTerminalConfig(tomlTable);
        var worktree = ParseWorktreeConfig(tomlTable);
        var cleanup = ParseCleanupConfig(tomlTable);
        var scripts = ParseScriptsConfig(tomlTable);
        var confirmations = ParseConfirmationsConfig(tomlTable);

        return new Config(terminal, worktree, cleanup, scripts, confirmations);
    }

    private TerminalConfig ParseTerminalConfig(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("terminal", out var terminalObj) || terminalObj is not TomlTable terminalTable)
            return new TerminalConfig();

        var mode = terminalTable.TryGetValue("mode", out var modeObj) && modeObj is string modeStr
            ? ParseTerminalMode(modeStr)
            : TerminalMode.Tab;

        var alwaysNew = terminalTable.TryGetValue("always_new", out var alwaysNewObj) && alwaysNewObj is true;

        var program = terminalTable.TryGetValue("program", out var programObj) && programObj is string programStr
            ? programStr
            : null;

        return new TerminalConfig(mode, alwaysNew, program);
    }

    private static WorktreeConfig ParseWorktreeConfig(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("worktree", out var worktreeObj) || worktreeObj is not TomlTable worktreeTable)
            return new WorktreeConfig();

        var dirPattern = worktreeTable.TryGetValue("directory_pattern", out var dirObj) && dirObj is string dirStr
            ? dirStr
            : "../{repo_name}-worktrees/{branch}";

        var autoFetch = !worktreeTable.TryGetValue("auto_fetch", out var fetchObj) || fetchObj is not bool fetchBool || fetchBool;

        var branchPrefix = worktreeTable.TryGetValue("branch_prefix", out var prefixObj) && prefixObj is string prefixStr
            ? prefixStr
            : null;

        return new WorktreeConfig(dirPattern, autoFetch, branchPrefix);
    }

    private CleanupConfig ParseCleanupConfig(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("cleanup", out var cleanupObj) || cleanupObj is not TomlTable cleanupTable)
            return new CleanupConfig();

        var mode = cleanupTable.TryGetValue("default_mode", out var modeObj) && modeObj is string modeStr
            ? ParseCleanupMode(modeStr)
            : CleanupMode.Interactive;

        return new CleanupConfig(mode);
    }

    private ScriptsConfig ParseScriptsConfig(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("scripts", out var scriptsObj) || scriptsObj is not TomlTable scriptsTable)
            return new ScriptsConfig();

        // Handle backward compatibility: init → session_init
        string? sessionInit = null;
        if (scriptsTable.TryGetValue("session_init", out var sessionInitObj) && sessionInitObj is string sessionInitStr)
        {
            sessionInit = sessionInitStr;
        }
        else if (scriptsTable.TryGetValue("init", out var initObj) && initObj is string initStr)
        {
            _logger.LogWarning("The 'init' script key is deprecated. Please rename it to 'session_init' in your configuration.");
            sessionInit = initStr;
        }

        var preCreate = scriptsTable.TryGetValue("pre_create", out var preCreateObj) && preCreateObj is string preCreateStr ? preCreateStr : null;
        var postCreate = scriptsTable.TryGetValue("post_create", out var postCreateObj) && postCreateObj is string postCreateStr ? postCreateStr : null;
        var postCreateAsync = scriptsTable.TryGetValue("post_create_async", out var postCreateAsyncObj) && postCreateAsyncObj is string postCreateAsyncStr ? postCreateAsyncStr : null;
        var preCleanup = scriptsTable.TryGetValue("pre_cleanup", out var preCleanupObj) && preCleanupObj is string preCleanupStr ? preCleanupStr : null;
        var postCleanup = scriptsTable.TryGetValue("post_cleanup", out var postCleanupObj) && postCleanupObj is string postCleanupStr ? postCleanupStr : null;
        var preSwitch = scriptsTable.TryGetValue("pre_switch", out var preSwitchObj) && preSwitchObj is string preSwitchStr ? preSwitchStr : null;
        var postSwitch = scriptsTable.TryGetValue("post_switch", out var postSwitchObj) && postSwitchObj is string postSwitchStr ? postSwitchStr : null;

        Dictionary<string, string>? custom = null;
        if (scriptsTable.TryGetValue("custom", out var customObj) && customObj is TomlTable customTable)
        {
            custom = new Dictionary<string, string>();
            foreach (var kvp in customTable)
            {
                if (kvp.Value is string customStr)
                    custom[kvp.Key] = customStr;
            }
        }

        return new ScriptsConfig(preCreate, postCreate, postCreateAsync, sessionInit, preCleanup, postCleanup, preSwitch, postSwitch, custom);
    }

    private ConfirmationsConfig ParseConfirmationsConfig(TomlTable tomlTable)
    {
        if (!tomlTable.TryGetValue("confirmations", out var confirmationsObj) || confirmationsObj is not TomlTable confirmationsTable)
            return new ConfirmationsConfig();

        var cleanupMultiple = !confirmationsTable.TryGetValue("cleanup_multiple", out var cleanupObj) || cleanupObj is not bool cleanupBool || cleanupBool;

        var forceOperations = !confirmationsTable.TryGetValue("force_operations", out var forceObj) || forceObj is not bool forceBool || forceBool;

        return new ConfirmationsConfig(cleanupMultiple, forceOperations);
    }

    private static TerminalMode ParseTerminalMode(string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "tab" => TerminalMode.Tab,
            "window" => TerminalMode.Window,
            "inplace" => TerminalMode.Inplace,
            "echo" => TerminalMode.Echo,
            "vscode" => TerminalMode.VSCode,
            "cursor" => TerminalMode.Cursor,
            _ => TerminalMode.Tab
        };
    }

    private static CleanupMode ParseCleanupMode(string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "all" => CleanupMode.All,
            "remoteless" => CleanupMode.Remoteless,
            "merged" => CleanupMode.Merged,
            "interactive" => CleanupMode.Interactive,
            "github" => CleanupMode.GitHub,
            _ => CleanupMode.Interactive
        };
    }

    private Config MergeConfigs(Config baseConfig, Config overrideConfig)
    {
        // Merge each section, with override taking precedence
        _logger.LogDebug("Merging configs - Base Terminal Mode: {BaseMode}, Override Terminal Mode: {OverrideMode}", 
            baseConfig.Terminal.Mode, overrideConfig.Terminal.Mode);
            
        var result = new Config(
            overrideConfig.Terminal ?? baseConfig.Terminal,
            overrideConfig.Worktree ?? baseConfig.Worktree,
            overrideConfig.Cleanup ?? baseConfig.Cleanup,
            overrideConfig.Scripts ?? baseConfig.Scripts,
            overrideConfig.Confirmations ?? baseConfig.Confirmations
        );
        
        _logger.LogDebug("Merged result Terminal Mode: {ResultMode}", result.Terminal.Mode);
        return result;
    }

    private Config ApplyCliOverrides(Config config, Dictionary<string, object> overrides)
    {
        // Apply CLI overrides to config
        // This is simplified - in reality you'd parse the dictionary and update specific fields
        _logger.LogDebug("Applying CLI overrides");
        return config; // TODO: Implement CLI override logic
    }

    private Config ApplyEnvironmentOverrides(Config config, Dictionary<string, object> updates)
    {
        // Apply environment variable updates
        // This is simplified - in reality you'd map env vars to config fields
        _logger.LogDebug("Applying environment variable overrides");
        return config; // TODO: Implement env var override logic
    }

    /// <summary>
    /// Check if user has explicitly configured a cleanup mode.
    /// </summary>
    public bool HasUserConfiguredCleanupMode()
    {
        if (!File.Exists(_globalConfigFile))
            return false;

        try
        {
            var tomlContent = File.ReadAllText(_globalConfigFile);
            var tomlTable = Toml.ToModel(tomlContent);
            return tomlTable.ContainsKey("cleanup") &&
                   tomlTable["cleanup"] is TomlTable cleanupTable &&
                   cleanupTable.ContainsKey("default_mode");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Save just the cleanup mode preference, preserving other settings.
    /// </summary>
    public void SaveCleanupMode(CleanupMode mode)
    {
        Setup(); // Ensure directory exists
        _logger.LogDebug("Saving cleanup mode preference: {Mode}", mode);

        TomlTable existingData;
        if (File.Exists(_globalConfigFile))
        {
            try
            {
                var tomlContent = File.ReadAllText(_globalConfigFile);
                existingData = Toml.ToModel(tomlContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load existing config, will create new");
                existingData = new TomlTable();
            }
        }
        else
        {
            existingData = new TomlTable();
        }

        // Update just the cleanup mode
        if (!existingData.ContainsKey("cleanup"))
        {
            existingData["cleanup"] = new TomlTable();
        }

        var cleanupTable = (TomlTable)existingData["cleanup"];
        cleanupTable["default_mode"] = mode.ToString().ToLowerInvariant();

        // Save back
        try
        {
            var tomlString = Toml.FromModel(existingData);
            File.WriteAllText(_globalConfigFile, tomlString);
            _logger.LogDebug("Cleanup mode preference saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cleanup mode preference");
            throw;
        }
    }

    /// <summary>
    /// Save configuration to global config file.
    /// </summary>
    public void SaveConfig(Config config)
    {
        Setup(); // Ensure directory exists
        _logger.LogDebug("Saving global configuration");

        try
        {
            var tomlTable = ConfigToToml(config);
            var tomlString = Toml.FromModel(tomlTable);
            File.WriteAllText(_globalConfigFile, tomlString);
            _logger.LogDebug("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            throw;
        }
    }

    private static TomlTable ConfigToToml(Config config)
    {
        var table = new TomlTable();

        // Terminal
        var terminalTable = new TomlTable
        {
            ["mode"] = config.Terminal.Mode.ToString().ToLowerInvariant(),
            ["always_new"] = config.Terminal.AlwaysNew
        };
        if (config.Terminal.Program != null)
            terminalTable["program"] = config.Terminal.Program;
        table["terminal"] = terminalTable;

        // Worktree
        var worktreeTable = new TomlTable
        {
            ["directory_pattern"] = config.Worktree.DirectoryPattern,
            ["auto_fetch"] = config.Worktree.AutoFetch
        };
        if (config.Worktree.BranchPrefix != null)
            worktreeTable["branch_prefix"] = config.Worktree.BranchPrefix;
        table["worktree"] = worktreeTable;

        // Cleanup
        var cleanupTable = new TomlTable
        {
            ["default_mode"] = config.Cleanup.DefaultMode.ToString().ToLowerInvariant()
        };
        table["cleanup"] = cleanupTable;

        // Scripts (only if any are set)
        var scriptsTable = new TomlTable();
        if (config.Scripts.PreCreate != null) scriptsTable["pre_create"] = config.Scripts.PreCreate;
        if (config.Scripts.PostCreate != null) scriptsTable["post_create"] = config.Scripts.PostCreate;
        if (config.Scripts.PostCreateAsync != null) scriptsTable["post_create_async"] = config.Scripts.PostCreateAsync;
        if (config.Scripts.SessionInit != null) scriptsTable["session_init"] = config.Scripts.SessionInit;
        if (config.Scripts.PreCleanup != null) scriptsTable["pre_cleanup"] = config.Scripts.PreCleanup;
        if (config.Scripts.PostCleanup != null) scriptsTable["post_cleanup"] = config.Scripts.PostCleanup;
        if (config.Scripts.PreSwitch != null) scriptsTable["pre_switch"] = config.Scripts.PreSwitch;
        if (config.Scripts.PostSwitch != null) scriptsTable["post_switch"] = config.Scripts.PostSwitch;
        if (config.Scripts.Custom != null && config.Scripts.Custom.Count > 0)
        {
            var customTable = new TomlTable();
            foreach (var kvp in config.Scripts.Custom)
                customTable[kvp.Key] = kvp.Value;
            scriptsTable["custom"] = customTable;
        }
        if (scriptsTable.Count > 0)
            table["scripts"] = scriptsTable;

        // Confirmations
        var confirmationsTable = new TomlTable
        {
            ["cleanup_multiple"] = config.Confirmations.CleanupMultiple,
            ["force_operations"] = config.Confirmations.ForceOperations
        };
        table["confirmations"] = confirmationsTable;

        return table;
    }
}
