using Abs.CommonCore.Installer.Config;

namespace Abs.CommonCore.Installer.Actions
{
    public abstract class ActionBase
    {
        protected void MergeParameters(Dictionary<string, string> source, Dictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                source[parameter.Key] = parameter.Value;
            }
        }

        protected string ReplaceConfigParameters(string text, Dictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                text = text.Replace(parameter.Key, parameter.Value, StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }

        protected Component[] DetermineComponents(ComponentRegistryConfig registryConfig, string[]? specificComponents, string[]? configComponents)
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

                if (configComponents?.Length > 0)
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
    }
}
