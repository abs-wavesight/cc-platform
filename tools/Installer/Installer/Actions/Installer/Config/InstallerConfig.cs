using System.Text.Json.Serialization;
using Contracts.Helpers.Attributes;

namespace Abs.CommonCore.Installer.Actions.Installer.Config
{
    [JsonSchema("{\r\n  \"$schema\": \"https://json-schema.org/draft/2019-09/schema\",\r\n  \"type\": \"object\",\r\n  \"default\": {},\r\n  \"title\": \"JSON schema for component installer\",\r\n  \"required\": [\r\n    \"location\",\r\n    \"components\"\r\n  ],\r\n  \"properties\": {\r\n    \"location\": {\r\n      \"type\": \"string\",\r\n      \"default\": \"\",\r\n      \"title\": \"Location to read components from\"\r\n    },\r\n    \"components\": {\r\n      \"type\": \"array\",\r\n      \"default\": [],\r\n      \"title\": \"The components to process\",\r\n      \"items\": {\r\n        \"type\": \"object\",\r\n        \"title\": \"Individual components to process\",\r\n        \"required\": [\r\n          \"name\",\r\n          \"actions\"\r\n        ],\r\n        \"properties\": {\r\n          \"name\": {\r\n            \"type\": \"string\",\r\n            \"default\": \"\",\r\n            \"title\": \"Name of the component\"\r\n          },\r\n          \"actions\": {\r\n            \"type\": \"array\",\r\n            \"default\": [],\r\n            \"title\": \"Actions for the component\",\r\n            \"items\": {\r\n              \"type\": \"object\",\r\n              \"default\": {},\r\n              \"title\": \"Action information\",\r\n              \"required\": [\r\n                \"source\",\r\n                \"action\"\r\n              ],\r\n              \"properties\": {\r\n                \"source\": {\r\n                  \"type\": \"string\",\r\n                  \"default\": \"\",\r\n                  \"title\": \"Source for the action\"\r\n                },\r\n                \"destination\": {\r\n                  \"type\": \"string\",\r\n                  \"default\": \"\",\r\n                  \"title\": \"Destination for the action\"\r\n                },\r\n                \"action\": {\r\n                  \"type\": \"string\",\r\n                  \"default\": \"none\",\r\n                  \"title\": \"Action for the source\",\r\n                  \"enum\": [ \"none\", \"copy\", \"install\", \"execute\", \"updatePath\" ]\r\n                },\r\n                \"isImmediate\": {\r\n                  \"type\": \"boolean\",\r\n                  \"default\": \"false\",\r\n                  \"title\": \"Indicates whether the action has higher priority\"\r\n                }\r\n              }\r\n            }\r\n          }\r\n        }\r\n      }\r\n    }\r\n  }\r\n}")]
    public class InstallerConfig
    {
        [JsonPropertyName("location")]
        public string Location { get; init; } = "";

        [JsonPropertyName("components")]
        public Component[] Components { get; init; } = Array.Empty<Component>();
    }
}
