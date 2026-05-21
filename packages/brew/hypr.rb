class Hypr < Formula
  desc "Customizable git worktree manager"
  homepage "https://github.com/Stephanvs/hypr"
  version "0.3.2"
  license "MIT"

  on_macos do
    on_arm do
      url "https://github.com/Stephanvs/hypr/releases/download/v0.3.2/hypr-macos-arm64.tar.gz"
      sha256 "66b5f2eab5f5b3bd82cca383fe10531c31eea0e4d5a0de66645786674766ec4a"
    end

    on_intel do
      url "https://github.com/Stephanvs/hypr/releases/download/v0.3.2/hypr-macos-x64.tar.gz"
      sha256 "1acac94b8807b043456df8bec80522719c20fc2a4c543dcab06e06d9d666b854"
    end
  end

  on_linux do
    on_arm do
      url "https://github.com/Stephanvs/hypr/releases/download/v0.3.2/hypr-linux-arm64.tar.gz"
      sha256 "ac57df319ab5c4607a16497b78e06b85bd6cd7baf49b4fafe8e22c9a36de9047"
    end

    on_intel do
      url "https://github.com/Stephanvs/hypr/releases/download/v0.3.2/hypr-linux-x64.tar.gz"
      sha256 "5ce8663b1b32b433883d9ee90b3290f26a3a55e5fd24ae23cdfa915cac018842"
    end
  end

  def install
    bin.install "hypr"
  end

  test do
    assert_match "Usage", shell_output("#{bin}/hypr --help")
  end
end
