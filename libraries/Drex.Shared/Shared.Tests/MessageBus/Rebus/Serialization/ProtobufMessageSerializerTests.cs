using Abs.CommonCore.Contracts.Proto.Disco.Data;
using Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Serialization;
using FluentAssertions;
using Google.Protobuf;
using Rebus.Messages;

namespace Abs.CommonCore.Drex.Shared.Tests.MessageBus.Rebus.Serialization;

public class ProtobufMessageSerializerTests
{
    [Fact]
    public async Task Serialize_GivenStringBody_ShouldSerializeBodyAndRetainHeaders()
    {
        // Arrange
        var underTest = new ProtobufMessageSerializer<DiscoDataRequest>();
        var request = new DiscoDataRequest
        {
            RequestId = "123",
            Destination = Contracts.Proto.Disco.Destination.Remote
        };
        var bodyBytes = request.ToByteArray();
        var headers = new Dictionary<string, string> { { "key", "value" } };
        var message = new Message(headers, request);

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
        var underTest = new ProtobufMessageSerializer<DiscoDataRequest>();
        var request = new DiscoDataRequest
        {
            RequestId = "321",
            Destination = Contracts.Proto.Disco.Destination.Local
        };
        var bodyBytes = request.ToByteArray();
        var headers = new Dictionary<string, string> { { "key", "value" } };
        var transportMessage = new TransportMessage(headers, bodyBytes);

        // Act
        var result = await underTest.Deserialize(transportMessage);

        // Assert
        var actualRequest = result.Body as DiscoDataRequest;
        actualRequest.Should().NotBeNull();
        actualRequest?.RequestId.Should().Be(request.RequestId);
        actualRequest?.Destination.Should().Be(request.Destination);
        result.Headers.Count.Should().Be(headers.Count);
        result.Headers.ContainsKey(headers.First().Key).Should().BeTrue();
        result.Headers[headers.First().Key].Should().Be(headers.First().Value);
    }
}
