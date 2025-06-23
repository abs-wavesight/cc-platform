using Abs.CommonCore.Contracts.Json.Installer;
using Abs.CommonCore.Installer.Services;

namespace Abs.CommonCore.Installer.Actions;

internal class PreInstallActions(ICommandExecutionService commandExecutionService, ILogger logger, ComponentDeterminationSteps componentDeterminationSteps)
{
    internal async Task<bool> CheckRequiredContainersRun(Component component, string rootLocation, ComponentAction action, bool dockerRun)
    {
        if (dockerRun == false)
        {
            logger.LogError("Docker is not running, but required.");
            throw new Exception("Docker is not running, but required for this action.");
        }

        logger.LogInformation($"Checking containers from '{action.Source}'");

        var rawLines = await File.ReadAllLinesAsync(action.Source);
        var requiredComponents = rawLines.Select(c => c.Trim().Split("_")).ToArray();
        var command = DockerPath.GetDockerPath();
        var currentContainers = componentDeterminationSteps.GetCurrentContainers(commandExecutionService, logger, true, command);

        foreach (var requiredComponent in requiredComponents)
        {
            var containerExist = false;
            foreach (var container in currentContainers)
            {
                if (container.ImageName.Contains(requiredComponent[0]))
                {
                    containerExist = true;
                    var splitTag = container.ImageTag.Split("-");
                    var version = splitTag[2].Split(".");
                    var minimumVersion = requiredComponent[1].Split(".");
                    var i = 0;
                    while (i < version.Length && i < minimumVersion.Length)
                    {
                        if (int.TryParse(version[i], out var currentVersion) && int.TryParse(minimumVersion[i], out var requiredVersion))
                        {
                            if (currentVersion < requiredVersion)
                            {
                                logger.LogError($"Container '{requiredComponent[0]}' version {container.ImageTag} is lower than required {requiredComponent[1]}.");
                                throw new Exception($"Container '{requiredComponent[0]}' version {container.ImageTag} is lower than required {requiredComponent[1]}.");
                            }
                        }

                        i++;
                    }

                    break;
                }
            }

            if (!containerExist)
            {
                logger.LogError($"Required container '{requiredComponent[0]}' is not running.");
                return false;
            }
        }

        return true;
    }
}
