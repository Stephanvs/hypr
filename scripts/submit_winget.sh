#!/bin/bash
# Script to submit hypr package to WinGet using Komac

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== WinGet Package Submission Script ===${NC}\n"

# Check if GITHUB_TOKEN is set
if [ -z "$GITHUB_TOKEN" ]; then
    echo -e "${RED}Error: GITHUB_TOKEN environment variable is not set${NC}"
    echo -e "${YELLOW}Please set it with: export GITHUB_TOKEN=your_token_here${NC}"
    echo -e "${YELLOW}Or run: GITHUB_TOKEN=your_token ./scripts/submit_winget.sh${NC}\n"
    echo "To create a token:"
    echo "1. Go to https://github.com/settings/tokens"
    echo "2. Click 'Generate new token (classic)'"
    echo "3. Select 'public_repo' scope"
    echo "4. Copy the token"
    exit 1
fi

# Get the repository root
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# Get the latest tag
LATEST_TAG=$(git tag --sort=-v:refname | head -1)
VERSION=${LATEST_TAG#v}

echo "Repository: https://github.com/Stephanvs/hypr"
echo "Latest Tag: $LATEST_TAG"
echo "Version: $VERSION"
echo ""

# Check if the release exists
INSTALLER_URL="https://github.com/Stephanvs/hypr/releases/download/$LATEST_TAG/hypr-windows-x64.exe"
echo "Checking if installer exists at: $INSTALLER_URL"
if ! curl --output /dev/null --silent --head --fail "$INSTALLER_URL"; then
    echo -e "${RED}Error: Installer not found at $INSTALLER_URL${NC}"
    echo -e "${YELLOW}Please create a release first with the Windows x64 binary${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Installer found${NC}\n"

# Download Komac if not already present
KOMAC_DIR="/tmp/komac-hypr"
KOMAC_BIN="$KOMAC_DIR/komac"

if [ ! -f "$KOMAC_BIN" ]; then
    echo "Downloading Komac..."
    mkdir -p "$KOMAC_DIR"
    curl -L https://github.com/russellbanks/Komac/releases/download/v2.14.0/komac-2.14.0-x86_64-unknown-linux-gnu.tar.gz | tar xz -C "$KOMAC_DIR"
    chmod +x "$KOMAC_BIN"
    echo -e "${GREEN}✓ Komac downloaded${NC}\n"
else
    echo -e "${GREEN}✓ Komac already available${NC}\n"
fi

# Check if fork exists
echo "Checking if you have forked microsoft/winget-pkgs..."
if ! curl -s -H "Authorization: token $GITHUB_TOKEN" https://api.github.com/repos/Stephanvs/winget-pkgs > /dev/null 2>&1; then
    echo -e "${RED}Error: Fork not found${NC}"
    echo -e "${YELLOW}Please fork https://github.com/microsoft/winget-pkgs to your account first${NC}"
    echo "Visit: https://github.com/microsoft/winget-pkgs and click Fork"
    exit 1
fi
echo -e "${GREEN}✓ Fork found${NC}\n"

# Ask for confirmation
echo -e "${YELLOW}Ready to submit package to WinGet:${NC}"
echo "  Identifier: Stephanvs.hypr"
echo "  Version: $VERSION"
echo "  Installer URL: $INSTALLER_URL"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 0
fi

# Try to generate manifest first (dry run)
echo -e "\n${YELLOW}Testing manifest generation...${NC}\n"
if "$KOMAC_BIN" new \
  Stephanvs.hypr \
  --version "$VERSION" \
  --urls "$INSTALLER_URL" \
  --publisher "Stephan van Stekelenburg" \
  --package-name "hypr" \
  --short-description "A modern, highly customizable CLI tool that supercharges your git worktree workflow" \
  --license MIT \
  --package-url "https://github.com/Stephanvs/hypr" \
  --dry-run; then

  echo -e "\n${GREEN}Manifest generation successful! Submitting to WinGet...${NC}\n"
  "$KOMAC_BIN" new \
    Stephanvs.hypr \
    --version "$VERSION" \
    --urls "$INSTALLER_URL" \
    --publisher "Stephan van Stekelenburg" \
    --package-name "hypr" \
    --short-description "A modern, highly customizable CLI tool that supercharges your git worktree workflow" \
    --license MIT \
    --package-url "https://github.com/Stephanvs/hypr" \
    --submit \
    --token "$GITHUB_TOKEN"
else
  echo -e "\n${RED}Manifest generation failed. This might be due to unsupported file type.${NC}"
  echo -e "${YELLOW}Alternative: Change your release to use .zip instead of .tar.gz${NC}"
  echo -e "${YELLOW}Or manually create the WinGet manifest files.${NC}"
  exit 1
fi

echo -e "\n${GREEN}=== Submission Complete! ===${NC}"
echo "Check your pull request at: https://github.com/microsoft/winget-pkgs/pulls"
echo "You can close the issue at: https://github.com/microsoft/winget-pkgs/issues/322859"
