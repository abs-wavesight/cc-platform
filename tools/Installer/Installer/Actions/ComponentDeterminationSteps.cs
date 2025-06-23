using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Actions.Models;
using Abs.CommonCore.Installer.Services;
using Abs.CommonCore.Platform.Extensions;

namespace Abs.CommonCore.Installer.Actions;
internal class ComponentDeterminationSteps(ICommandExecutionService commandExecutionService,
    ILogger logger,
    IServiceManager serviceManager,
    InstallerComponentRegistryConfig registryConfig,
    InstallerComponentInstallerConfig? installerConfig,
    string imageStorage)
{
    internal async Task<Component[]> DetermineComponents(string[]? specificComponents, string[][] installingVesrion_Component, bool dockerRun)
    {
        try
        {
            var installLocation = new DirectoryInfo(registryConfig.Location);

            if (specificComponents?.Length > 0)
            {
                return specificComponents
                    .Select(x => registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
            }

            if (installerConfig?.Components.Count > 0)
            {
                var defaultComponentsToInstall = installerConfig.Components
                    .Select(x => registryConfig.Components.First(y => string.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .ToArray();
                var command = DockerPath.GetDockerPath();
                var currentContainers = GetCurrentContainers(commandExecutionService, logger, dockerRun, command);

                logger.LogInformation("Determining components to install");

                if (!dockerRun
                    || (defaultComponentsToInstall.Any(c => c.Name == "RabbitMqNano") && NeedUpdateComponent("RabbitMqNano", installingVesrion_Component, currentContainers))
                    || (defaultComponentsToInstall.Any(c => c.Name == "RabbitMq") && NeedUpdateComponent("RabbitMq", installingVesrion_Component, currentContainers)))
                {
                    logger.LogInformation("Full installation is required");
                    if (dockerRun)
                    {
                        logger.LogInformation("Stopping docker");
                        await commandExecutionService.ExecuteCommandAsync(command, "network prune -f", "");

                        await serviceManager.StopServiceAsync("dockerd");
                        await serviceManager.StopServiceAsync("docker");
                    }

                    return defaultComponentsToInstall;
                }
                else
                {
                    logger.LogInformation("Selecting components to update.");
                    return SelectUpdatedComponents(installingVesrion_Component, defaultComponentsToInstall, currentContainers).ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to determine components to use", ex);
        }

        throw new Exception("No components found to download");
    }

    internal List<DockerContainerInfoModel> GetCurrentContainers(ICommandExecutionService commandExecutionService, ILogger logger, bool dockerRun, string command)
    {
        var currentContainers = new List<DockerContainerInfoModel>();
        if (dockerRun)
        {
            var rawDockerPsResponse = commandExecutionService.ExecuteCommandWithResult(command, "ps", "");
            logger.LogInformation("Parsing raw ps command response:");
            foreach (var line in rawDockerPsResponse)
            {
                logger.LogInformation("    {line}", line);
            }

            currentContainers = DockerService.ParceDockerPsCommand(rawDockerPsResponse);
        }

        return currentContainers;
    }

    private List<Component> SelectUpdatedComponents(string[][] installingVersionComponent, Component[] defaultComponentsToInstall, List<DockerContainerInfoModel> currentContainers)
    {
        var componentsToInstall = new List<Component>();
        foreach (var component in defaultComponentsToInstall)
        {
            switch (component.Name)
            {
                case "Docker":
                case "RabbitMq":
                case "RabbitMqNano":
                case "Certificates-Central":
                    break;
                case "Installer":
                    componentsToInstall.Add(component);
                    break;
                default:
                    if (NeedUpdateComponent(component.Name, installingVersionComponent, currentContainers))
                    {
                        componentsToInstall.Add(component);
                    }

                    break;
            }
        }

        logger.LogInformation("Components to install: {components}", componentsToInstall.Select(x => x.Name).StringJoin(Environment.NewLine));
        return componentsToInstall;
    }

    private bool NeedUpdateComponent(string componentName, string[][] installingVersionComponent, List<DockerContainerInfoModel> currentContainers)
    {
        var bringingComponentTag = installingVersionComponent.FirstOrDefault(x => x[1].Contains(componentName.ToLower()))?[0];
        var currentContainer = currentContainers.FirstOrDefault(x => x.ImageName == imageStorage + componentName.ToLower());
        if (currentContainer == null)
        {
            return true;
        }

        var currentComponentOs = currentContainer.ImageTag.Substring(0, 13);
        var currentComponentVersion = currentContainer.ImageTag.Substring(13);
        var containerToKeep = currentContainer.ImageName + ":" + currentComponentOs + bringingComponentTag;

        if (bringingComponentTag == currentComponentVersion)
        {
            return false;
        }

        return true;
    }
}
