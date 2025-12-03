using System.CommandLine;

namespace Hypr;

public class DebugOption()
  : Option<bool>("--debug", "Enable debug logging.");