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

- Global: `~/.config/hyprwt/config.json` (Linux/macOS) or `%APPDATA%\hyprwt\config.json` (Windows)
- Project: `hyprwt.json` or `.hyprwt.json` (in repo root)
- Backward compatible: Reads `hyprwt.json` and `.hyprwt.json`

**Environment variables**

- `HYPRWT_TERMINAL_MODE` - Terminal mode (tab/window/inplace/echo/vscode/cursor)
- `HYPRWT_WORKTREE_AUTO_FETCH` - Auto fetch (true/false)
- `HYPRWT_CLEANUP_DEFAULT_MODE` - Default cleanup mode

**Example config**
```json
{
    "terminal": {
      "Mode": "tab",
      "AlwaysNew": false
    },
    "worktree": {
      "DirectoryPattern": "../{repo_name}-worktrees/{branch}",
      "AutoFetch": true
    },
    "cleanup": {
      "DefaultMode": "interactive"
    },
    "scripts": {
      "SessionInit": "source .env",
      "PostCreate": "npm install"
    }
}
```

