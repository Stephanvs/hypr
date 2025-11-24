class Hyprwt < Formula
  desc "Customizable git worktree manager"
  homepage "https://github.com/Stephanvs/hyprwt"
  url "https://github.com/Stephanvs/hyprwt/releases/download/v0.1.0/hyprwt-macos-x64.tar.gz"
  sha256 "TODO_REPLACE_WITH_ACTUAL_SHA256"
  license "MIT"

  depends_on "dotnet@8"

  def install
    bin.install "hyprwt"
  end

  test do
    system "#{bin}/hyprwt", "--version"
  end
end