using System.Text;
using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Commands.Run;
using Abs.CommonCore.LocalDevUtility.Commands.Shared;
using Abs.CommonCore.LocalDevUtility.Extensions;

namespace Abs.CommonCore.LocalDevUtility.Helpers;

public static class DockerHelper
{
    public static void ResetDocker(IPowerShellAdapter powerShellAdapter)
    {
        using (CliStep.Start("Resetting Docker", true))
        {
            powerShellAdapter.RunPowerShellCommand("docker ps -aq | ForEach-Object { Write-Output \"Stopping $(docker stop $_) & removing $(docker rm $_)\" };");
            powerShellAdapter.RunPowerShellCommand("Write-Output \"Pruning system: $(docker system prune -f)\" ");
            powerShellAdapter.RunPowerShellCommand("Write-Output \"Pruning volume: $(docker volume prune -f)\" ");
            powerShellAdapter.RunPowerShellCommand("Write-Output \"Pruning network: $(docker network prune -f)\" ");
        }
    }

    public static StringBuilder BuildComposeCommand(AppConfig appConfig, ComposeOptions composeOptions)
    {
        var executionRootPath = Path.GetFullPath(
            Path.Combine(appConfig.CommonCorePlatformRepositoryPath!, Constants.DockerComposeExecutionRootPath));

        var composeCommandBuilder = new StringBuilder();
        composeCommandBuilder.Append($"cd \"{executionRootPath}\"; docker-compose -f docker-compose.root.yml");

        AddAllAliasesTargets(composeOptions);
        AddAllDependencies(composeOptions);

        using (CliStep.Start("Constructing compose command", true))
        {
            foreach (var component in ComposeOptions.NonAliasComponentPropertyNames)
            {
                AddComponentFilesIfPresent(composeCommandBuilder, composeOptions, component);
            }
        }

        return composeCommandBuilder;
    }

    public static async Task CreateEnvFile(AppConfig appConfig, Dictionary<string, string>? additionalOptions = null)
    {
        var envValues = new Dictionary<string, string>
        {
            [Constants.ComposeEnvKeys.WindowsVersion] = appConfig.ContainerWindowsVersion!,
            [Constants.ComposeEnvKeys.PathToCommonCorePlatformRepository] = appConfig.CommonCorePlatformRepositoryPath!,
            [Constants.ComposeEnvKeys.PathToCommonCoreDrexRepository] = appConfig.CommonCoreDrexRepositoryPath!,
            [Constants.ComposeEnvKeys.PathToCommonCoreDiscoRepository] = appConfig.CommonCoreDiscoRepositoryPath!,
            [Constants.ComposeEnvKeys.PathToCommonCoreSiemensAdapterRepository] = appConfig.CommonCoreSiemensAdapterRepositoryPath!,
            [Constants.ComposeEnvKeys.PathToCommonCoreKdiAdapterRepository] = appConfig.CommonCoreKdiAdapterRepositoryPath!,
            [Constants.ComposeEnvKeys.PathToCommonCoreVoyageManagerAdapterRepository] = appConfig.VoyageManagerRepositoryPath!,
            [Constants.ComposeEnvKeys.PathToCommonCoreSchedulerRepository] = appConfig.CommonCoreSchedulerRepositoryPath!,
            [Constants.ComposeEnvKeys.PathToCommonCoreDrexNotificationAdapterRepository] = appConfig.CommonCoreDrexNotificationAdapterRepositoryPath!,
            [Constants.ComposeEnvKeys.PathToCertificates] = appConfig.CertificatePath!,
            [Constants.ComposeEnvKeys.PathToSshKeys] = appConfig.SshKeysPath!,
            [Constants.ComposeEnvKeys.SftpRootPath] = appConfig.SftpRootPath!,
            [Constants.ComposeEnvKeys.FdzRootPath] = appConfig.FdzRootPath!,
            [Constants.ComposeEnvKeys.AbsCcClientsLogsPath] = appConfig.CommonCoreCliensLogs!
        };

        if (additionalOptions != null)
        {
            foreach (var (key, value) in additionalOptions)
            {
                envValues.Add(key, value);
            }
        }

        var envFileText = string.Join("\n", envValues.Select(p => $"{p.Key}={p.Value}"));
        var envFileName = Path.Combine(appConfig.CommonCorePlatformRepositoryPath!, Constants.EnvFileRelativePath);
        await File.WriteAllTextAsync(envFileName, envFileText);
        Console.WriteLine($"\nDocker Compose .env file created ({envFileName}):\n{envFileText}");
    }

    /// <summary>
    /// Transform all alias components into their targets
    /// </summary>
    /// <param name="composeOptions"></param>
    private static void AddAllAliasesTargets(ComposeOptions composeOptions)
    {
        foreach (var component in ComposeOptions.AliasComponentPropertyNames)
        {
            AddAliasIfNeeded(composeOptions, component);
        }
    }

    private static void AddAliasIfNeeded(ComposeOptions composeOptions, string componentPropertyName)
    {
        if (composeOptions.GetType().GetProperty(componentPropertyName)!.GetValue(composeOptions, null) is not ComposeComponentMode propertyValue)
        {
            return;
        }

        var runComponentAliases = composeOptions.GetType().GetRunComponentAliases(componentPropertyName).ToList();
        if (!runComponentAliases.Any())
        {
            return;
        }

        foreach (var runComponentAlias in runComponentAliases)
        {
            composeOptions.GetType().GetProperty(runComponentAlias.AliasPropertyName)!.SetValue(composeOptions, propertyValue);
        }
    }

    /// <summary>
    /// Iterate all components until all dependencies are added
    /// </summary>
    /// <param name="composeOptions"></param>
    private static void AddAllDependencies(ComposeOptions composeOptions)
    {
        bool dependencyFound;
        do
        {
            dependencyFound = false;
            foreach (var component in ComposeOptions.ComponentPropertyNames)
            {
                if (AddDependenciesIfNeeded(composeOptions, component))
                {
                    dependencyFound = true;
                }
            }
        } while (dependencyFound);
    }

    /// <summary>
    /// Returns true if a dependency was added
    /// </summary>
    /// <param name="composeOptions"></param>
    /// <param name="componentPropertyName"></param>
    /// <returns></returns>
    private static bool AddDependenciesIfNeeded(ComposeOptions composeOptions, string componentPropertyName)
    {
        if (composeOptions.GetType().GetProperty(componentPropertyName)!.GetValue(composeOptions, null) is not ComposeComponentMode)
        {
            return false;
        }

        var dependencyFound = false;
        var runComponentDependencies = composeOptions.GetType().GetRunComponentDependencies(componentPropertyName);
        foreach (var runComponentDependency in runComponentDependencies)
        {
            var dependencyPropertyInfo = composeOptions.GetType().GetProperty(runComponentDependency.DependencyPropertyName)!;
            var dependencyIsAlreadyPresent = dependencyPropertyInfo.GetValue(composeOptions, null) is ComposeComponentMode;
            if (dependencyIsAlreadyPresent)
            {
                continue;
            }

            // Set dependency component to run from image
            dependencyPropertyInfo.SetValue(composeOptions, ComposeComponentMode.i);
            dependencyFound = true;
        }

        return dependencyFound;
    }

    private static void AddComponentFilesIfPresent(
        StringBuilder builder,
        ComposeOptions composeOptions,
        string componentPropertyName)
    {
        if (composeOptions.GetType().GetProperty(componentPropertyName)!.GetValue(composeOptions, null) is not ComposeComponentMode propertyValue)
        {
            return;
        }

        var runComponent = composeOptions.GetType().GetRunComponent(componentPropertyName)!;
        builder.Append($" -f ./{runComponent.ComposePath}/docker-compose.base.yml");
        builder.Append($" -f ./{runComponent.ComposePath}/docker-compose.{GetComposeFileSuffix(propertyValue)}.yml");

        var variantToAdd = GetVariantToAddIfNeeded(composeOptions, componentPropertyName);
        if (!string.IsNullOrWhiteSpace(variantToAdd))
        {
            builder.Append($" -f ./{runComponent.ComposePath}/docker-compose.variant.{variantToAdd}.yml");
        }

        if (string.IsNullOrWhiteSpace(runComponent.Profile))
        {
            return;
        }

        AddProfile(builder, runComponent.Profile);
    }

    private static string? GetVariantToAddIfNeeded(ComposeOptions composeOptions, string componentPropertyName)
    {
        var runComponent = composeOptions.GetType().GetRunComponent(componentPropertyName)!;
        var variantToAdd = runComponent.DefaultVariant;
        foreach (var potentialDependingComponentPropertyName in ComposeOptions.ComponentPropertyNames)
        {
            var dependencyRunComponents = composeOptions.GetType().GetRunComponentDependencies(potentialDependingComponentPropertyName);
            var matchingRunComponentDependency = dependencyRunComponents.SingleOrDefault(_ => _.DependencyPropertyName == componentPropertyName);
            if (matchingRunComponentDependency == null)
            {
                continue;
            }

            var dependentRunComponentIsNotPresent = composeOptions.GetType().GetProperty(potentialDependingComponentPropertyName)!.GetValue(composeOptions, null) is not ComposeComponentMode;
            if (dependentRunComponentIsNotPresent)
            {
                continue;
            }

            // Only overwrite variant if it's present
            if (!string.IsNullOrWhiteSpace(matchingRunComponentDependency.Variant))
            {
                variantToAdd = matchingRunComponentDependency.Variant;
            }
        }

        return variantToAdd;
    }

    private static void AddProfile(StringBuilder builder, string profile)
    {
        builder.Append($" --profile {profile}");
    }

    private static string GetComposeFileSuffix(ComposeComponentMode composeComponentMode)
    {
        return composeComponentMode switch
        {
            ComposeComponentMode.s => "source",
            ComposeComponentMode.i => "image",
            _ => throw new ArgumentOutOfRangeException(nameof(composeComponentMode), composeComponentMode, null),
        };
    }
}
