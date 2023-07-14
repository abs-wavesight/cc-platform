using Abs.CommonCore.LocalDevUtility.Commands.Shared;

namespace Abs.CommonCore.LocalDevUtility.Commands.Stop;

public class StopOptions : ComposeOptions
{
    public bool? Reset { get; set; }
}
