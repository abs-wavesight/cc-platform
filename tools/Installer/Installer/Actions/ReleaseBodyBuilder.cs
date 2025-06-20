﻿using System.Globalization;
using System.Text;
using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Extensions;
using Abs.CommonCore.Platform.Config;

namespace Abs.CommonCore.Installer.Actions;

public class ReleaseBodyBuilder : ActionBase
{
    private const string EnglishCultureName = "en-us";
    private readonly ILogger _logger;

    public ReleaseBodyBuilder(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ReleaseBodyBuilder>();
    }

    public static async Task BuildReleaseBodyAsync(FileInfo? configFile, Dictionary<string, string>? parameters, FileInfo output)
    {
        var config = configFile != null
            ? ConfigParser.LoadConfig<InstallerComponentDownloaderConfig>(configFile.FullName)
            : null;

        var mergedParameters = config?.Parameters ?? new Dictionary<string, string>();
        mergedParameters = mergedParameters.MergeParameters(parameters);

        await CreateBodyAsync(mergedParameters, output);
    }

    private static async Task CreateBodyAsync(Dictionary<string, string> parameters, FileInfo output)
    {
        var body = new StringBuilder();

        foreach (var parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Value))
            {
                continue;
            }

            body.AppendLine($"{FormatKey(parameter.Key)}: {parameter.Value}");
        }

        await File.WriteAllTextAsync(output.FullName, body.ToString());
    }

    private static string FormatKey(string key)
    {
        key = key
            .Replace("$", "")
            .Replace("_", " ")
            .ToLower();

        var culture = new CultureInfo(EnglishCultureName);
        return culture.TextInfo.ToTitleCase(key);
    }
}
