# hyprwt 🚀

> **A better git worktree experience**

`hyprwt` is a modern, highly customizable CLI tool that supercharges your git worktree workflow. It makes creating, switching, and managing worktrees effortless, with first-class terminal integration and powerful automation hooks.

## Why `hyprwt`?

Git worktrees are amazing for parallel development, but managing them manually is tedious. `hyprwt` solves this:

*   **⚡ Instant Context Switching**: `hyprwt feature-branch` handles everything—fetching, creating the worktree, and opening it.
*   **🖥️ Terminal Integrated**: Automatically opens your worktree in a new tab or window (supports iTerm2, Tmux, Ghostty, VS Code, and more).
*   **✨ Interactive TUI**: built-in interactive menu for selecting and managing worktrees.
*   **🧹 Smart Cleanup**: `hyprwt cleanup` intelligently finds and deletes worktrees for merged or closed branches (including GitHub integration).
*   **🔗 Lifecycle Hooks**: Run scripts automatically on create, switch, or cleanup (e.g., `bun install` or copying `.env` files).

## 📦 Installation

### Package Managers (Recommended)

Support for various package managers is available:

*   **Homebrew (macOS)**: `brew install hyprwt`
*   **Winget (Windows)**: `winget install hyprwt`
*   **Scoop (Windows)**: `scoop install hyprwt`
*   **AUR (Arch Linux)**: `yay -S hyprwt`

### .NET Tool (Alternative)

```bash
dotnet tool install --global hyprwt
```

## ⚡ Usage

### Create & Switch
Create a new worktree for a feature branch and open it instantly in a new tab:

```bash
# Creates a worktree for 'my-feature' and opens it
hyprwt my-feature
```

### Cleanup
Clean up old worktrees. `hyprwt` checks if branches are merged or if their PRs are closed.

```bash
hyprwt cleanup
```

## 🛠️ Configuration

`hyprwt` is highly configurable via a global or project-local `hyprwt.json` file.

**Example `hyprwt.json`:**

```json
{
  "worktree": {
    "directoryPattern": "../{repo_name}-worktrees/{branch}"
  },
  "terminal": {
    "mode": "tab"
  },
  "scripts": {
    "sessionInit": "bun install && cp ../main/.env ."
  }
}
```

See [example_config.json](example_config.json) for a comprehensive list of options.

## 🏗️ Development

**Prerequisites:**
- .NET 10.0 SDK

**Build & Run:**
```bash
# Setup dependencies
./setup.sh

# Build
dotnet build

# Run tests
dotnet test
```
