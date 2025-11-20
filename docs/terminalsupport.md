# Terminal support

## hyprwt automates your terminal by default

`hyprwt`'s intended user experience is that it will open terminal tabs on your behalf. It uses [`automate-terminal`](https://github.com/irskep/automate-terminal) to accomplish this, so check that project out to find out if your terminal is supported.

## What to do if your terminal isn't supported or you don't want this behavior

Add this to your `.hyprwt.toml` or set it at the user level with `hyprwt config`:

```
[terminal]
mode = 'echo'
```

This will cause hyprwt to print commands to the console instead of having your terminal run them automatically. You can then manually run the printed commands to navigate to the worktree.
