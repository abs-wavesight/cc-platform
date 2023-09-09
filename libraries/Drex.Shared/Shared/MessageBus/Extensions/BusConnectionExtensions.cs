using Abs.CommonCore.Contracts.Json.Drex;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Extensions;

public static class BusConnectionExtensions
{
    public static string ToConnectionString(this BusConnection busConnection)
    {
        return busConnection.Protocol == BusConnectionProtocol.Unknown
            ? throw new Exception("Unable to build connection string for unknown protocol")
            : new UriBuilder
            {
                Host = busConnection.Host,
                Port = busConnection.Port,
                Scheme = busConnection.Protocol.ToString().ToLower(),
                UserName = busConnection.Username,
                Password = busConnection.Password,
            }.ToString();
    }

    public static BusConnection FromConnectionString(this string connectionString)
    {
        var uri = new UriBuilder(connectionString);

        return new BusConnection
        {
            Host = uri.Host,
            Port = uri.Port,
            Protocol = Enum.Parse<BusConnectionProtocol>(uri.Scheme, true),
            Username = uri.UserName,
            Password = uri.Password,
        };
    }
}
