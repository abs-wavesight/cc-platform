using Abs.CommonCore.Contracts.Json.Drex;

namespace Abs.CommonCore.Platform.Extensions;

public static class BusConnectionExtensions
{
    public static string ToConnectionString(this BusConnection connection)
    {
        return connection.Protocol == BusConnectionProtocol.Unknown
            ? throw new InvalidOperationException("Unable to build connection string for unknown protocol")
            : new UriBuilder
            {
                Host = connection.Host,
                Port = connection.Port,
                Scheme = connection.Protocol.ToString().ToLower(),
                UserName = connection.Username,
                Password = connection.Password,
                Path = connection.VHost
            }.ToString();
    }

    public static BusConnection FromConnectionString(this string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
        }

        var uri = new UriBuilder(connectionString);
        return new BusConnection
        {
            Host = uri.Host,
            Port = uri.Port,
            Protocol = Enum.Parse<BusConnectionProtocol>(uri.Scheme, true),
            Username = uri.UserName,
            Password = uri.Password,
            VHost = uri.Path.Trim('/')
        };
    }
}
