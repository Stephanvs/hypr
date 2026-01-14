# Configuring hypr

`hypr` is designed to work out of the box with sensible defaults, but you can customize its behavior to perfectly match your workflow. This guide covers the different ways you can configure `hypr`, from global settings to project-specific rules and one-time command-line overrides.

For a comprehensive example configuration file with comments explaining all options, see the [example_config.json](https://github.com/irskep/hypr/blob/main/example_config.json) in the repository.

## Configuration layers

`hypr` uses a hierarchical configuration system. Settings are loaded from multiple sources, and later sources override earlier ones. The order of precedence is:

1.  **Built-in Defaults**: Sensible defaults for all settings.
2.  **Global `config.json`**: User-wide settings that apply to all your projects.
3.  **Project `.hypr.json`**: Project-specific settings, defined in your repository's root.
4.  **Environment Variables**: System-wide overrides, prefixed with `HYPR_`.
5.  **Command-Line Flags**: The highest priority, for on-the-fly adjustments.

## Configuration files

### Global configuration

Your global settings are stored in a `config.json` file in a platform-appropriate directory:

- **macOS**: `~/Library/Application Support/hypr/config.json`
- **Linux**: `~/.config/hypr/config.json` (or `$XDG_CONFIG_HOME/hypr/config.json`)
- **Windows**: `%APPDATA%\hypr\config.json`

The easiest way to manage common settings is with the `hypr config` command, which launches an interactive TUI (Text-based User Interface) for the most frequently used options. For the complete set of configuration options, you can edit the config file directly.

### Project-specific configuration

For settings that should apply only to a specific project, create a `.hypr.json` file in the root of your repository. This is the ideal place to define project-wide init scripts or worktree settings.

## All configuration options

This section provides a comprehensive reference for all available configuration options, organized by section. Each option includes its TOML key, the corresponding environment variable, and any command-line flags.

---

### `terminal` - Terminal management

Controls how `hypr` interacts with your terminal.

<div class="hypr-clitable-wrapper"></div>

| Key          | Type    | Default | Description                                                                                                                                                                                                                                                                                                                            |
| ------------ | ------- | ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `mode`       | string  | `"window"` | Determines how `hypr` opens worktrees. <br> • `window`: Open in a new window (default). <br> • `tab`: Open in a new tab. <br> • `inplace`: Switch the current terminal to the worktree directory. <br> • `echo`: Output shell commands to stdout. <br> **ENV**: `HYPR_TERMINAL_MODE` <br> **CLI**: `--terminal <mode>` |
| `always_new` | boolean | `false` | If `true`, always creates a new terminal session instead of switching to an existing one for a worktree. <br> **ENV**: `HYPR_TERMINAL_ALWAYS_NEW` <br> **CLI**: `--ignore-same-session`                                                                                                                                              |
| `program`    | string  | `null`  | Force `hypr` to use a specific terminal program instead of auto-detecting one. <br> _Examples: `iterm2`, `terminal`, `tmux`_ <br> **ENV**: `HYPR_TERMINAL_PROGRAM`                                                                                                                                                                 |

---

### `worktree` - Worktree management

Defines how worktrees are created and managed.

<div class="hypr-clitable-wrapper"></div>

| Key                 | Type    | Default                               | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| ------------------- | ------- | ------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `directory_pattern` | string  | `"../{repo_name}-worktrees/{branch}"` | The template for creating worktree directory paths. Can use variables `{repo_dir}` (full repo path), `{repo_name}` (repo directory name), `{repo_parent_dir}` (parent directory of repo), `{branch}` (branch name), and environment variables like `$HOME`. Examples: `"{repo_parent_dir}/worktrees/{branch}"`, `"$HOME/worktrees/{repo_name}/{branch}"`. This can be overridden on a per-command basis using the `--dir` flag. <br> **ENV**: `HYPR_WORKTREE_DIRECTORY_PATTERN` <br> **CLI**: `--dir <path>` |
| `auto_fetch`        | boolean | `true`                                | If `true`, automatically fetches from the remote before creating new worktrees. <br> **ENV**: `HYPR_WORKTREE_AUTO_FETCH` <br> **CLI**: `--no-fetch` (to disable)                                                                                                                                                                                                                                                                                                                                             |
| `branch_prefix`     | string  | `null`                                | Automatically prefix new branch names with a template. Can use variables `{repo_name}`, `{github_username}` (if `gh` CLI is available), and environment variables. Examples: `"feature/"`, `"{github_username}/"`. When set, `hypr my-feature` creates `feature/my-feature`. Also applies when switching: `hypr my-feature` switches to `feature/my-feature` if it exists. Prevents double-prefixing if the branch name already includes the prefix. <br> **ENV**: `HYPR_WORKTREE_BRANCH_PREFIX`          |

---

### `cleanup` - Cleanup behavior

Configures the `hypr cleanup` command.

<div class="hypr-clitable-wrapper"></div>

| Key            | Type   | Default         | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| -------------- | ------ | --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `default_mode` | string | `"interactive"` | The default mode for the `cleanup` command. <br> • `interactive`: Opens a TUI to let you choose what to remove. <br> • `merged`: Selects branches that have been merged into your main branch. <br> • `remoteless`: Selects local branches that don't have an upstream remote. <br> • `all`: Non-interactively selects all merged and remoteless branches. <br> • `github`: Uses the GitHub CLI (`gh`) to identify branches with merged or closed pull requests. <br><br> **First run**: If not configured, hypr will prompt you to select your preferred mode on first use. If `gh` is available, the `github` option will be offered; otherwise, a note will mention it becomes available when `gh` is installed. <br> **ENV**: `HYPR_CLEANUP_DEFAULT_MODE` <br> **CLI**: `--mode <mode>` |

---

### `scripts` - Lifecycle hooks and scripts

See [Lifecycle Hooks](lifecyclehooks.md).

#### `scripts.custom`

Define named, reusable scripts for specialized workflows.

```json
{
  "Scripts": {
    "Custom": {
      "bugfix": "claude \"Fix the bug described in GitHub issue $1\"",
      "release": "claude \"/release\""
    }
  }
}
```

These are run _after_ the standard `session_init` script. You can invoke them with the `--custom-script` flag, and any additional arguments are passed to the script. For one-time commands, the `--after-init` flag is often simpler.

This feature is very bare-bones, and is intended to lay the groundwork for future fanciness.

---

### `confirmations` - User interface

Manage which operations require a confirmation prompt.

<div class="hypr-clitable-wrapper"></div>

| Key                | Type    | Default | Description                                                                                                                               |
| ------------------ | ------- | ------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `cleanup_multiple` | boolean | `true`  | Ask for confirmation before cleaning up multiple worktrees in non-interactive mode. <br> **ENV**: `HYPR_CONFIRMATIONS_CLEANUP_MULTIPLE` |
| `force_operations` | boolean | `true`  | Ask for confirmation when using a `--force` flag. <br> **ENV**: `HYPR_CONFIRMATIONS_FORCE_OPERATIONS`                                   |

You can skip all confirmations for a single command by using the `-y` or `--yes` flag.
