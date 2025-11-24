# hyprwt

Customizable git worktree manager written in .NET.

## CI/CD Pipeline

This project uses GitHub Actions for automated CI/CD:

### Continuous Integration (CI)
- **Trigger**: Push to `main` branch or pull requests
- **Platforms**: Ubuntu, Windows, macOS
- **Actions**:
  - Restore dependencies
  - Build project
  - Run tests

### Continuous Deployment (CD)
- **Trigger**: Push of version tags (`v*`)
- **Actions**:
  - Build binaries for multiple platforms (Linux x64, Windows x64, macOS x64, macOS ARM64)
  - Create NuGet package for dotnet tool
  - Create GitHub release with binaries and changelog
  - Publish to NuGet.org

### Release Management
- Uses [release-drafter](https://github.com/release-drafter/release-drafter) for automatic changelog generation
- Labels PRs with appropriate categories (feature, bug, docs, etc.) to generate meaningful changelogs

### Package Managers
Templates for publishing to various package managers are available in the `packages/` directory:
- **winget**: Windows Package Manager
- **scoop**: Windows command-line installer
- **brew**: macOS Homebrew
- **AUR**: Arch Linux User Repository

## Development

### Prerequisites
- .NET 10.0 SDK
- mise (for dependency management)

### Setup
```bash
./setup.sh
```

### Build
```bash
dotnet build
```

### Test
```bash
dotnet test
```

### Publish locally
```bash
dotnet publish src/hyprwt.csproj --runtime linux-x64 --self-contained
```