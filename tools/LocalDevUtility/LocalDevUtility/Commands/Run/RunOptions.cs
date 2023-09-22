// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

using Abs.CommonCore.LocalDevUtility.Commands.Shared;

namespace Abs.CommonCore.LocalDevUtility.Commands.Run;

public class RunOptions : ComposeOptions
{
    public RunMode? Mode { get; set; }
    public bool? Reset { get; set; }
    public bool? Background { get; set; }
    public bool? AbortOnContainerExit { get; set; }
    public string? DrexSiteConfigFileNameOverride { get; set; }
    public bool? Verbose { get; set; }
    public bool? FlatLogs { get; set; }
}
