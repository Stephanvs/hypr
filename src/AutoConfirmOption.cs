using System.CommandLine;

namespace Hypr;

public class AutoConfirmOption : Option<bool>
{
  public AutoConfirmOption()
    : base("--yes")
  {
    Aliases.Add("-y");
    Aliases.Add("--skip");

    Required = false;

    Description = "Auto confirm actions, eg; branch creation, deletion, etc.";

    DefaultValueFactory = _ => false;
  }
};