using Contracts.Helpers.Attributes;

namespace CommonCore.Platform.Tests.Models
{
    [JsonSchema("{\r\n\"type\": \"object\",\r\n  \"properties\": {\r\n    \"IntValue\": {\r\n      \"type\": \"integer\"\r\n    },\r\n    \"StringValue\": {\r\n      \"type\": \"string\"\r\n    }\r\n  },\r\n  \"required\": [\r\n    \"IntValue\",\r\n    \"StringValue\"\r\n  ]\r\n}")]
    public class TestConfig
    {
        public int IntValue { get; init; }
        public string? StringValue { get; init; }
    }
}
