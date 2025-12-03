# Troubleshooting

!!! tip "Getting Help"
    Before [filing an issue](https://github.com/irskep/hypr/issues), please run your command with the `--debug` flag to get the full output, which is very helpful for diagnosing problems.
    
    ```bash
    hypr --debug <command-that-is-failing>
    ```

## Common issues

### Terminal control on macOS

**Symptom**: `hypr` doesn't open a new terminal tab/window, or it fails to switch to an existing session on macOS.

This is almost always a permissions issue. `hypr` uses AppleScript to control your terminal, which requires you to grant access.

**Solution**:

1.  Open `System Settings`.
2.  Navigate to `Privacy & Security`.
3.  Grant your terminal application (e.g., `iTerm.app`, `Terminal.app`) permissions for both **Accessibility** and **Automation**.
