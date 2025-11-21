using System.CommandLine;

namespace Hyprwt;

public class DebugOption()
  : Option<bool>("--debug", "Enable debug logging.");