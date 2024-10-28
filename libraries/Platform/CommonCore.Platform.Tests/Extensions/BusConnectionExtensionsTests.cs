using Abs.CommonCore.Contracts.Json.Drex;
using Abs.CommonCore.Platform.Extensions;
using Xunit;

namespace Abs.CommonCore.Platform.Tests.Extensions;

public class BusConnectionExtensionsTests
{
    [Fact]
    public void ToConnectionString_ValidConfig_ReturnsCorrectConnectionString()
    {
        // Arrange
        var config = new BusConnection
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
    public void ToConnectionStringWithVHost_ValidConfig_ReturnsCorrectConnectionString()
    {
        // Arrange
        var config = new BusConnection
        {
            Host = "localhost",
            Port = 5672,
            Protocol = BusConnectionProtocol.Amqp,
            Username = "user",
            Password = "password",
            VHost = "test_vhost"
        };

        // Act
        var connectionString = config.ToConnectionString();

        // Assert
        Assert.Equal("amqp://user:password@localhost:5672/test_vhost", connectionString);
    }

    [Fact]
    public void ToConnectionString_UnknownProtocol_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new BusConnection
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
        var config = connectionString.FromConnectionString();

        // Assert
        Assert.Equal("localhost", config.Host);
        Assert.Equal(5672, config.Port);
        Assert.Equal(BusConnectionProtocol.Amqp, config.Protocol);
        Assert.Equal("user", config.Username);
        Assert.Equal("password", new System.Net.NetworkCredential(string.Empty, config.Password).Password);
    }

    [Fact]
    public void FromConnectionString_ValidConnectionStringWithVHost_ReturnsCorrectConfig()
    {
        // Arrange
        var connectionString = "amqp://user:password@localhost:5672/test";

        // Act
        var config = connectionString.FromConnectionString();

        // Assert
        Assert.Equal("localhost", config.Host);
        Assert.Equal(5672, config.Port);
        Assert.Equal(BusConnectionProtocol.Amqp, config.Protocol);
        Assert.Equal("user", config.Username);
        Assert.Equal("password", config.Password);
        Assert.Equal("test", config.VHost);
    }

    [Fact]
    public void FromConnectionString_NullOrEmptyConnectionString_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(((string)null).FromConnectionString);
        Assert.Throws<ArgumentNullException>("".FromConnectionString);
    }
}
