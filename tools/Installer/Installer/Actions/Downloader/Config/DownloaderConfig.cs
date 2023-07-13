using System.Text.Json.Serialization;
using Contracts.Helpers.Attributes;

namespace Abs.CommonCore.Installer.Actions.Downloader.Config
{
    [JsonSchema("{\r\n  \"$schema\": \"https://json-schema.org/draft/2019-09/schema\",\r\n  \"type\": \"object\",\r\n  \"default\": {},\r\n  \"title\": \"JSON schema for component downloader\",\r\n  \"required\": [\r\n    \"outputLocation\",\r\n    \"components\"\r\n  ],\r\n  \"properties\": {\r\n    \"outputLocation\": {\r\n      \"type\": \"string\",\r\n      \"default\": \"\",\r\n      \"title\": \"Location to save components to\"\r\n    },\r\n    \"components\": {\r\n      \"type\": \"array\",\r\n      \"default\": [],\r\n      \"title\": \"The components to process\",\r\n      \"items\": {\r\n        \"type\": \"object\",\r\n        \"title\": \"Individual components to process\",\r\n        \"required\": [\r\n          \"name\",\r\n          \"type\",\r\n          \"files\"\r\n        ],\r\n        \"properties\": {\r\n          \"name\": {\r\n            \"type\": \"string\",\r\n            \"default\": \"\",\r\n            \"title\": \"Name of the components\"\r\n          },\r\n          \"type\": {\r\n            \"type\": \"string\",\r\n            \"default\": \"none\",\r\n            \"title\": \"Type of the component\",\r\n            \"enum\": [ \"none\", \"docker\" ]\r\n          },\r\n          \"files\": {\r\n            \"type\": \"array\",\r\n            \"default\": [],\r\n            \"title\": \"Files for the component\",\r\n            \"items\": {\r\n              \"type\": \"object\",\r\n              \"default\": {},\r\n              \"title\": \"File information\",\r\n              \"required\": [\r\n                \"source\",\r\n                \"destination\"\r\n              ],\r\n              \"properties\": {\r\n                \"source\": {\r\n                  \"type\": \"string\",\r\n                  \"default\": \"\",\r\n                  \"title\": \"Source for the file\"\r\n                },\r\n                \"destination\": {\r\n                  \"type\": \"string\",\r\n                  \"default\": \"\",\r\n                  \"title\": \"Destination for the file\"\r\n                }\r\n              }\r\n            }\r\n          }\r\n        }\r\n      }\r\n    }\r\n  }\r\n}")]
    public class DownloaderConfig
    {
        [JsonPropertyName("outputLocation")]
        public string OutputLocation { get; init; }

        [JsonPropertyName("components")]
        public Component[] Components { get; init; }
    }
}
