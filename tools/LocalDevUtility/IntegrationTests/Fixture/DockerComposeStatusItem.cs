namespace Abs.CommonCore.LocalDevUtility.IntegrationTests.Fixture;

public class DockerComposeStatusItem
{
    /*
     * {
    "ID": "10fa8c0e2ced81dd35ec60378fd5a7c147a4fa18cbdd4ef2c71f468117b96aac",
    "Name": "drex-message-service-i",
    "Image": "ghcr.io/abs-wavesight/cc-drex-message-service:windows-2019",
    "Command": "dotnet Abs.CommonCore.Drex.Console.dll",
    "Project": "abs-cc",
    "Service": "cc.drex-message-service",
    "Created": 1689348922,
    "State": "running",
    "Status": "Up 2 minutes (healthy)",
    "Health": "healthy",
    "ExitCode": 0,
    "Publishers": [
      {
        "URL": "0.0.0.0",
        "TargetPort": 5000,
        "PublishedPort": 12345,
        "Protocol": "tcp"
      }
    ]
  }
     */
    public string? Name { get; set; }
    public string? Project { get; set; }
    public string? Service { get; set; }
    public string? State { get; set; }
    public string? Status { get; set; }
    public string? Health { get; set; }
    public string? Networks { get; set; }
    public int ExitCode { get; set; }
}
