using Abs.CommonCore.RabbitMQ.Shared.SystemConfiguration;

namespace Abs.CommonCore.Installer.Actions.Models;
public class RmqConfiguration : IRmqAdvancedConfiguration
{
    public string RmqHost { get; set; } = "localhost";

    public string RmqUserName { get; set; } = "guest";

    public string RmqPassword { get; set; } = "guest";

    public string RmqVirtualHost { get; set; } = "/";

    public string RabbitMqConnectionString => $"https://{RmqUserName}:{RmqPassword}@{RmqHost}/{RmqVirtualHost}";
}
