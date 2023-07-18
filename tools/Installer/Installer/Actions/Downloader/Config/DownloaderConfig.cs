using System.Text.Json.Serialization;
using Contracts.Helpers.Attributes;

namespace Abs.CommonCore.Installer.Actions.Downloader.Config
{
    [JsonSchema("{\r\n  \"$schema\": \"https://json-schema.org/draft/2019-09/schema\",\r\n  \"type\": \"object\",\r\n  \"default\": {},\r\n  \"title\": \"JSON schema for component downloader\",\r\n  \"required\": [\r\n    \"components\"\r\n  ],\r\n  \"properties\": {\r\n    \"components\": {\r\n      \"type\": \"array\",\r\n      \"default\": [],\r\n      \"title\": \"The components to download\",\r\n      \"items\": {\r\n        \"type\": \"string\"\r\n      }\r\n    }\r\n  }\r\n}")]
    public class DownloaderConfig
    {
        [JsonPropertyName("components")]
        public string[] Components { get; init; } = Array.Empty<string>();
    }
}
