using Abs.CommonCore.Contracts.Json.Installer;

namespace Abs.CommonCore.Installer.Actions
{
    public abstract class ActionBase
    {
        protected Component[] DetermineComponents(InstallerComponentRegistryConfig registryConfig, string[]? specificComponents, ICollection<string>? configComponents)
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

        protected void ReadMissingParameters(Dictionary<string, string> parameters)
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
    }
}
