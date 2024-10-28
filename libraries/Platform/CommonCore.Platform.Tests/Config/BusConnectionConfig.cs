using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Platform.Config;
using Xunit;

namespace Abs.CommonCore.Platform.Tests.Config;

public class BusConnectionConfigTests
{
    [Fact]
    public void ToConnectionString_ValidConfig_ReturnsCorrectConnectionString()
    {
        // Arrange
        var config = new BusConnectionConfig
        {
            Host = "localhost",
            Port = 5672,
            Protocol = BusConnectionProtocol.Amqp,
            Username = "user",
            Password = "password"
        };

        // Act
        var connectionString = config.ToConnectionString();

        // Assert
        Assert.Equal("amqp://user:password@localhost:5672/", connectionString);
    }

    [Fact]
    public void ToConnectionString_UnknownProtocol_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new BusConnectionConfig
        {
            Host = "localhost",
            Port = 5672,
            Protocol = BusConnectionProtocol.Unknown,
            Username = "user",
            Password = "password"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(config.ToConnectionString);
    }

    [Fact]
    public void FromConnectionString_ValidConnectionString_ReturnsCorrectConfig()
    {
        // Arrange
        var connectionString = "amqp://user:password@localhost:5672/";

        // Act
        var config = BusConnectionConfig.FromConnectionString(connectionString);

        // Assert
        Assert.Equal("localhost", config.Host);
        Assert.Equal(5672, config.Port);
        Assert.Equal(BusConnectionProtocol.Amqp, config.Protocol);
        Assert.Equal("user", config.Username);
        Assert.Equal("password", new System.Net.NetworkCredential(string.Empty, config.Password).Password);
    }

    [Fact]
    public void FromConnectionString_NullOrEmptyConnectionString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BusConnectionConfig.FromConnectionString(null));
        Assert.Throws<ArgumentNullException>(() => BusConnectionConfig.FromConnectionString(string.Empty));
    }
}
