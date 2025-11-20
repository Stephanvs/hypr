# hyprwt - Quick Start Guide

## What is `hyprwt`?

`hyprwt` (Git Worktree) is a C# port of [autowt](https://github.com/irskep/autowt), making git worktrees easy to manage.

**Why `hyprwt`?** It's git worktrees on steroids.

## Usage

```bash
# List worktrees
hyprwt
hyprwt ls

# Switch to or create worktree
hyprwt my-feature

# Interactive switch
hyprwt switch

# Clean up merged worktrees
hyprwt cleanup

# Interactive config
hyprwt config
```

## Configuration

**Config files**
- Global: `~/.config/hyprwt/config.toml` (Linux/macOS) or `%APPDATA%\hyprwt\config.toml` (Windows)
- Project: `hyprwt.toml` or `.hyprwt.toml` (in repo root)
- Backward compatible: Reads `hyprwt.toml` and `.hyprwt.toml`

**Environment variables**
- `HYPRWT_TERMINAL_MODE` - Terminal mode (tab/window/inplace/echo/vscode/cursor)
- `HYPRWT_WORKTREE_AUTO_FETCH` - Auto fetch (true/false)
- `HYPRWT_CLEANUP_DEFAULT_MODE` - Default cleanup mode

**Example config**
```toml
[terminal]
mode = "tab"
always_new = false

[worktree]
directory_pattern = "../{repo_name}-worktrees/{branch}"
auto_fetch = true

[cleanup]
default_mode = "interactive"

[scripts]
session_init = "source .env"
post_create = "npm install"
```

