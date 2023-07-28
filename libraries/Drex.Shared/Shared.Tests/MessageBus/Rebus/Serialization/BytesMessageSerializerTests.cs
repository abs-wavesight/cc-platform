using Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Serialization;
using FluentAssertions;
using Rebus.Messages;

namespace Abs.CommonCore.Drex.Shared.Tests.MessageBus.Rebus.Serialization;

public class BytesMessageSerializerTests
{
    [Fact]
    public async Task Serialize_GivenBytesBody_ShouldSerializeBodyAndRetainHeaders()
    {
        // Arrange
        var underTest = new BytesMessageSerializer();
        var bodyBytes = new byte[5] { 2, 5, 8, 6, 54 };
        var headers = new Dictionary<string, string> { { "key", "value" } };
        var message = new Message(headers, bodyBytes);

        // Act
        var result = await underTest.Serialize(message);

        // Assert
        result.Body.Should().Equal(bodyBytes);
        result.Headers.Count.Should().Be(headers.Count);
        result.Headers.ContainsKey(headers.First().Key).Should().BeTrue();
        result.Headers[headers.First().Key].Should().Be(headers.First().Value);
    }

    [Fact]
    public async Task Deserialize_GivenBytesBody_ShouldDeserializeBodyAndRetainHeaders()
    {
        // Arrange
        var underTest = new BytesMessageSerializer();
        var bodyBytes = new byte[5] { 2, 5, 8, 6, 54 };
        var headers = new Dictionary<string, string> { { "key", "value" } };
        var transportMessage = new TransportMessage(headers, bodyBytes);

        // Act
        var result = await underTest.Deserialize(transportMessage);

        // Assert
        result.Body.Should().Be(bodyBytes);
        result.Headers.Count.Should().Be(headers.Count);
        result.Headers.ContainsKey(headers.First().Key).Should().BeTrue();
        result.Headers[headers.First().Key].Should().Be(headers.First().Value);
    }
}
