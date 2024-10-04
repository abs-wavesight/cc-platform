using Abs.CommonCore.Contracts.Json.Installer;

#pragma warning disable CA1416

namespace Abs.CommonCore.Installer.Actions;

public abstract class ActionBase
{
    protected static Component[] DetermineComponents(InstallerComponentRegistryConfig registryConfig, string[]? specificComponents, ICollection<string>? configComponents)
    {
        try
        {
            if (specificComponents?.Length > 0)
            {
                return specificComponents
                    .Select(x => registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
            }

            if (configComponents?.Count > 0)
            {
                return configComponents
                    .Select(x => registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to determine components to use", ex);
        }

        throw new Exception("No components found to use");
    }

    protected static void ReadMissingParameters(Dictionary<string, string> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Value))
            {
                Console.Write($"Enter value for parameter '{parameter.Key}': ");
                parameters[parameter.Key] = Console.ReadLine() ?? "";
            }
        }
    }

    protected void DeleteRecursiveDirectory(ILogger logger, string path)
    {
        logger.LogInformation($"Deleting directory '{path}' recursively");
        foreach (var directory in Directory.GetDirectories(path))
        {
            DeleteRecursiveDirectory(logger, directory);
        }

        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException)
        {
            Directory.Delete(path, true);
        }
        catch (UnauthorizedAccessException)
        {
            Directory.Delete(path, true);
        }
    }
}
