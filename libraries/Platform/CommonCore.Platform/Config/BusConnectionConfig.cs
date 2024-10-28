using Abs.CommonCore.Contracts.Json.Drex;

namespace Abs.CommonCore.Platform.Config;

public class BusConnectionConfig
{
    public string Host { get; set; } = null!;

    public int Port { get; set; }

    public BusConnectionProtocol Protocol { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string ToConnectionString()
    {
        return Protocol == BusConnectionProtocol.Unknown
            ? throw new InvalidOperationException("Unable to build connection string for unknown protocol")
            : new UriBuilder
            {
                Host = Host,
                Port = Port,
                Scheme = Protocol.ToString().ToLower(),
                UserName = Username,
                Password = Password,
            }.ToString();
    }

    public static BusConnectionConfig FromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
        }

        var uri = new UriBuilder(connectionString);
        return new BusConnectionConfig
        {
            Host = uri.Host,
            Port = uri.Port,
            Protocol = Enum.Parse<BusConnectionProtocol>(uri.Scheme, true),
            Username = uri.UserName,
            Password = uri.Password,
        };
    }
}
