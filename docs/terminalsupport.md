# Terminal support

## hypr automates your terminal by default

`hypr`'s intended user experience is that it will open terminal tabs or windows on your behalf. It natively supports the following terminals:

### macOS

- **iTerm2** - Full tab and window support
- **Terminal.app** - Full tab and window support
- **tmux** - When running inside a tmux session

### Windows

- **Windows Terminal** - Full tab and window support

### Linux

- **GNOME Terminal** - Full tab and window support
- **tmux** - When running inside a tmux session

### All Platforms

- **VS Code** - Opens worktrees in VS Code (`--terminal=vscode`)
- **Cursor** - Opens worktrees in Cursor (`--terminal=cursor`)

## What to do if your terminal isn't supported or you don't want this behavior

Add this to your `.hypr.json` or set it at the user level with `hypr config`:

```json
{
  "Terminal": {
    "Mode": "echo"
  }
}
```

This will cause hypr to print commands to the console instead of having your terminal run them automatically. You can then manually run the printed commands to navigate to the worktree.

Alternatively, use `--terminal=inplace` to have hypr change the current terminal's directory directly.
