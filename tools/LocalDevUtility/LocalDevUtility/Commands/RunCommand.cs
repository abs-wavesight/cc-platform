using System.Text;
using Abs.CommonCore.LocalDevUtility.Extensions;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Abs.CommonCore.LocalDevUtility.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using TextCopy;

namespace Abs.CommonCore.LocalDevUtility.Commands;

public static class RunCommand
{
    public static async Task<int> Run(RunOptions runOptions, ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var appConfig = (await ConfigureCommand.ReadConfig())!;
        ConfigureCommand.ValidateConfigAndThrow(appConfig);

        if (runOptions.Reset == true)
        {
            DockerHelper.ResetDocker(powerShellAdapter);
        }

        await CreateEnvFile(appConfig, runOptions);

        using (CliStep.Start("Copying nuget.config files to component directories"))
        {
            var nugetConfigCopyTargets = new List<string>
            {
                Path.Combine(appConfig.CommonCoreDrexRepositoryPath!, "service"),
                Path.Combine(appConfig.CommonCoreDrexRepositoryPath!, "demo"),
                Path.Combine(appConfig.CommonCoreDrexRepositoryPath!, "client")
            };
            foreach (var nugetConfigCopyTarget in nugetConfigCopyTargets)
            {
                try
                {
                    File.Copy(
                        Path.Combine(appConfig.CommonCorePlatformRepositoryPath!, Constants.NugetConfigFileName),
                        Path.Combine(nugetConfigCopyTarget, Constants.NugetConfigFileName),
                        true);
                }
                catch (DirectoryNotFoundException ex)
                {
                    // Just warn here, it's not necessarily a breaking issue (e.g. in CI, or if we're not building containers from source)
                    logger.LogWarning($"Could not find nuget.config file to copy to {nugetConfigCopyTarget}: {ex.Message}");
                }
            }
        }

        using (CliStep.Start("Creating container logs directory"))
        {
            Directory.CreateDirectory(Path.Combine(appConfig.CommonCorePlatformRepositoryPath!, Constants.LogsDirectoryName));
        }

        var executionRootPath = Path.Combine(appConfig.CommonCorePlatformRepositoryPath!, Constants.DockerComposeExecutionRootPath);

        var composeCommandBuilder = new StringBuilder();
        composeCommandBuilder.Append($"cd \"{executionRootPath}\"; docker-compose -f docker-compose.root.yml");

        AddAllAliasesTargets(runOptions);
        AddAllDependencies(runOptions);

        using (CliStep.Start("Pulling images", true))
        {
            foreach (var component in RunOptions.NonAliasComponentPropertyNames)
            {
                PullImageIfNeeded(powerShellAdapter, runOptions, appConfig, component);
                AddComponentFilesIfPresent(composeCommandBuilder, runOptions, component);
            }
        }

        var configCommand = $"{composeCommandBuilder} config";
        Console.WriteLine("\nFinal compose configuration:");
        AnsiConsole.Write(new Rule());
        powerShellAdapter.RunPowerShellCommand(configCommand);
        AnsiConsole.Write(new Rule());
        Console.WriteLine();

        composeCommandBuilder.Append(" up --build");

        if (runOptions.Background == true)
        {
            composeCommandBuilder.Append(" --detach");
        }

        if (runOptions.AbortOnContainerExit == true)
        {
            composeCommandBuilder.Append(" --abort-on-container-exit");
        }

        if (runOptions.Mode == RunMode.c)
        {
            Console.WriteLine("\nCONFIRM: Press enter to run this Docker Compose command.");
            Console.WriteLine(composeCommandBuilder.ToString());
            Console.ReadLine();
        }

        if (runOptions.Mode is RunMode.c or RunMode.r)
        {
            Console.WriteLine("\nNow running the following Docker Compose command:");
            Console.WriteLine(composeCommandBuilder.ToString());
            powerShellAdapter.RunPowerShellCommand(composeCommandBuilder.ToString());
        }

        if (runOptions.Mode is RunMode.o)
        {
            await ClipboardService.SetTextAsync(composeCommandBuilder.ToString());
            Console.WriteLine("Docker Compose command below has been copied to your clipboard:");
            Console.WriteLine(composeCommandBuilder.ToString());
        }

        return 0;
    }

    private static void PullImageIfNeeded(IPowerShellAdapter powerShellAdapter, RunOptions runOptions, AppConfig appConfig, string componentPropertyName)
    {
        var componentIsNotIncludedOrIsSetToRunFromSource = runOptions.GetType().GetProperty(componentPropertyName)!.GetValue(runOptions, null) is not RunComponentMode propertyValue
                                                           || propertyValue == RunComponentMode.s;
        if (componentIsNotIncludedOrIsSetToRunFromSource)
        {
            return;
        }

        var runComponent = runOptions.GetType().GetRunComponent(componentPropertyName)!;
        Console.WriteLine($"Pulling {runComponent.ImageName} image...");
        powerShellAdapter.RunPowerShellCommand($"docker pull {Constants.ContainerRepository}/{runComponent.ImageName}:windows-{appConfig.ContainerWindowsVersion}");
    }

    /// <summary>
    /// Transform all alias components into their targets
    /// </summary>
    /// <param name="runOptions"></param>
    private static void AddAllAliasesTargets(RunOptions runOptions)
    {
        foreach (var component in RunOptions.AliasComponentPropertyNames)
        {
            AddAliasIfNeeded(runOptions, component);
        }
    }

    private static void AddAliasIfNeeded(RunOptions runOptions, string componentPropertyName)
    {
        if (runOptions.GetType().GetProperty(componentPropertyName)!.GetValue(runOptions, null) is not RunComponentMode propertyValue)
        {
            return;
        }

        var runComponent = runOptions.GetType().GetRunComponent(componentPropertyName)!;
        if (!runComponent.IsAlias)
        {
            return;
        }

        foreach (var aliasPropertyName in runComponent.AliasPropertyNames)
        {
            runOptions.GetType().GetProperty(aliasPropertyName)!.SetValue(runOptions, propertyValue);
        }

    }

    /// <summary>
    /// Iterate all components until all dependencies are added
    /// </summary>
    /// <param name="runOptions"></param>
    private static void AddAllDependencies(RunOptions runOptions)
    {
        bool dependencyFound;
        do
        {
            dependencyFound = false;
            foreach (var component in RunOptions.ComponentPropertyNames)
            {
                if (AddDependenciesIfNeeded(runOptions, component))
                {
                    dependencyFound = true;
                }
            }
        } while (dependencyFound);
    }

    /// <summary>
    /// Returns true if a dependency was added
    /// </summary>
    /// <param name="runOptions"></param>
    /// <param name="componentPropertyName"></param>
    /// <returns></returns>
    private static bool AddDependenciesIfNeeded(RunOptions runOptions, string componentPropertyName)
    {
        if (runOptions.GetType().GetProperty(componentPropertyName)!.GetValue(runOptions, null) is not RunComponentMode)
        {
            return false;
        }

        var dependencyFound = false;
        var runComponent = runOptions.GetType().GetRunComponent(componentPropertyName)!;
        foreach (var dependencyPropertyName in runComponent.DependencyPropertyNames)
        {
            var dependencyPropertyInfo = runOptions.GetType().GetProperty(dependencyPropertyName)!;
            var dependencyIsAlreadyPresent = dependencyPropertyInfo.GetValue(runOptions, null) is RunComponentMode;
            if (dependencyIsAlreadyPresent)
            {
                continue;
            }

            // Set dependency component to run from image
            dependencyPropertyInfo.SetValue(runOptions, RunComponentMode.i);
            dependencyFound = true;
        }

        return dependencyFound;
    }

    private static void AddComponentFilesIfPresent(
        StringBuilder builder,
        RunOptions runOptions,
        string componentPropertyName)
    {
        if (runOptions.GetType().GetProperty(componentPropertyName)!.GetValue(runOptions, null) is not RunComponentMode propertyValue) return;

        var runComponent = runOptions.GetType().GetRunComponent(componentPropertyName)!;
        builder.Append($" -f ./{runComponent.ComposePath}/docker-compose.base.yml");
        builder.Append($" -f ./{runComponent.ComposePath}/docker-compose.{GetComposeFileSuffix(propertyValue)}.yml");

        if (string.IsNullOrWhiteSpace(runComponent.Profile)) return;

        AddProfile(builder, runComponent.Profile);
    }

    private static void AddProfile(StringBuilder builder, string profile)
    {
        builder.Append($" --profile {profile}");
    }

    private static string GetComposeFileSuffix(RunComponentMode runComponentMode)
    {
        switch (runComponentMode)
        {
            case RunComponentMode.s:
                return "source";
            case RunComponentMode.i:
                return "image";
            default:
                throw new ArgumentOutOfRangeException(nameof(runComponentMode), runComponentMode, null);
        }
    }

    private static async Task CreateEnvFile(AppConfig appConfig, RunOptions runOptions)
    {
        var envValues = new Dictionary<string, string>
        {
            {Constants.ComposeEnvKeys.WindowsVersion, appConfig.ContainerWindowsVersion!},
            {Constants.ComposeEnvKeys.PathToCommonCorePlatformRepository, appConfig.CommonCorePlatformRepositoryPath!},
            {Constants.ComposeEnvKeys.PathToCommonCoreDrexRepository, appConfig.CommonCoreDrexRepositoryPath!},
            {Constants.ComposeEnvKeys.DrexSiteConfigFileNameOverride, runOptions.DrexSiteConfigFileNameOverride ?? Constants.DefaultDrexSiteConfigFileName}
        };
        var envFileText = string.Join("\n", envValues.Select(_ => $"{_.Key}={_.Value}"));
        var envFileName = Path.Combine(appConfig.CommonCorePlatformRepositoryPath!, Constants.EnvFileRelativePath);
        await File.WriteAllTextAsync(envFileName, envFileText);
        Console.WriteLine($"\nDocker Compose .env file created ({envFileName}):\n{envFileText}");
    }
}
