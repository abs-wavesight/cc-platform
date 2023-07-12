namespace Abs.CommonCore.LocalDevUtility.Models;

public class RunOptions
{
    public RunComponentMode? DrexService { get; set; }
    public RunComponentMode? DrexTestClient { get; set; }
    public RunComponentMode? Rabbitmq { get; set; }
    public RunComponentMode? Vector { get; set; }
    public RunComponentMode? Grafana { get; set; }
    public RunComponentMode? Loki { get; set; }

    public bool? Deps { get; set; }
    public bool? LogViz { get; set; }
    public bool? Reset { get; set; }
    public bool? Background { get; set; }
    public bool? AbortOnContainerExit { get; set; }
}
