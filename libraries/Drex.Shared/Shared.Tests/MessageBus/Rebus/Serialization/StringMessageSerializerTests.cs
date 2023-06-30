using System.Text;
using Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Serialization;
using FluentAssertions;
using Rebus.Messages;

namespace Abs.CommonCore.Drex.Shared.Tests.MessageBus.Rebus.Serialization;

public class StringMessageSerializerTests
{
    [Fact]
    public async Task Serialize_GivenStringBody_ShouldSerializeBodyAndRetainHeaders()
    {
        // Arrange
        var underTest = new StringMessageSerializer();
        var bodyString = "the body";
        var bodyBytes = Encoding.UTF8.GetBytes(bodyString);
        var headers = new Dictionary<string, string> { { "key", "value" } };
        var message = new Message(headers, bodyString);

        // Act
        var result = await underTest.Serialize(message);

        // Assert
        result.Body.Should().Equal(bodyBytes);
        result.Headers.Count.Should().Be(headers.Count);
        result.Headers.ContainsKey(headers.First().Key).Should().BeTrue();
        result.Headers[headers.First().Key].Should().Be(headers.First().Value);
    }

    [Fact]
    public async Task Deserialize_GivenStringBody_ShouldDeserializeBodyAndRetainHeaders()
    {
        // Arrange
        var underTest = new StringMessageSerializer();
        var bodyString = "the body";
        var bodyBytes = Encoding.UTF8.GetBytes(bodyString);
        var headers = new Dictionary<string, string> { { "key", "value" } };
        var transportMessage = new TransportMessage(headers, bodyBytes);

        // Act
        var result = await underTest.Deserialize(transportMessage);

        // Assert
        result.Body.Should().Be(bodyString);
        result.Headers.Count.Should().Be(headers.Count);
        result.Headers.ContainsKey(headers.First().Key).Should().BeTrue();
        result.Headers[headers.First().Key].Should().Be(headers.First().Value);
    }
}
