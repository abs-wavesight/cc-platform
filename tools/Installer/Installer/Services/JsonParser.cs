using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Abs.CommonCore.Platform.Exceptions;
using Abs.CommonCore.Platform.Extensions;
using Contracts.Helpers.Extensions;
using Json.Schema;

namespace Abs.CommonCore.Installer.Services;

public class JsonParser
{
    public static JsonParser Instance { get; } = new();

    private JsonParser()
    { }

    public T Load<T>(string filePath, Func<T, string, string>? preprocess = null)
        where T : class, new()
    {
        GuardAgainstMissingFile<T>(filePath);
        GuardAgainstInvalidJsonSchema<T>(filePath);

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var t = JsonSerializer.Deserialize<T>(fs)!;

        if (preprocess is null)
        {
            return t;
        }

        // Transform the config as needed
        var configText = File.ReadAllText(filePath);
        var transformed = preprocess(t, configText);

        // Parse transformed JSON
        return JsonSerializer.Deserialize<T>(new MemoryStream(Encoding.ASCII.GetBytes(transformed)))!;
    }

    private static void GuardAgainstMissingFile<T>(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            throw new ConfigException(typeof(T), configFilePath, "Config file not found");
        }
    }

    private static void GuardAgainstInvalidJsonSchema<T>(string configFilePath)
    {
        var configRawFileContents = File.ReadAllText(configFilePath);
        var configJsonObject = JsonNode.Parse(configRawFileContents);

        var schemaObject = (T)Activator.CreateInstance(typeof(T))!;
        var schemaText = schemaObject.ToJsonSchema();
        var jsonSchema = JsonSchema.FromText(schemaText);
        var schemaEvaluationResult = jsonSchema.Evaluate(
            configJsonObject,
            new EvaluationOptions { OutputFormat = OutputFormat.List });

        if (schemaEvaluationResult.IsValid)
        {
            return;
        }

        var errors = schemaEvaluationResult.Details
            .Where(_ => _.HasErrors)
            .SelectMany(detail => detail.Errors!.Select(error => $"{detail.InstanceLocation} > {detail.EvaluationPath}: {error.Value}"))
            .ToList();

        throw new ConfigException(typeof(T), configFilePath, errors.StringJoin(";"));
    }
}
