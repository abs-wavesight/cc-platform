using Abs.CommonCore.LocalDevUtility.Commands.Configure;
using Abs.CommonCore.LocalDevUtility.Extensions;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using TextCopy;

namespace Abs.CommonCore.LocalDevUtility.Commands.Run;

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

        var additionalEnvValues = new Dictionary<string, string>
        {
            { Constants.ComposeEnvKeys.DrexSiteConfigFileNameOverride, runOptions.DrexSiteConfigFileNameOverride ?? Constants.DefaultDrexSiteConfigFileName }
        };
        await DockerHelper.CreateEnvFile(appConfig, additionalEnvValues);

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

        var composeCommandBuilder = DockerHelper.BuildComposeCommand(appConfig, runOptions);

        var configCommand = $"{composeCommandBuilder} config";
        Console.WriteLine("\nFinal compose configuration:");
        AnsiConsole.Write(new Rule());
        powerShellAdapter.RunPowerShellCommand(configCommand);
        AnsiConsole.Write(new Rule());
        Console.WriteLine();

        using (CliStep.Start("Pulling images", true))
        {
            foreach (var component in RunOptions.NonAliasComponentPropertyNames)
            {
                PullImageIfNeeded(powerShellAdapter, runOptions, appConfig, component);
            }
        }

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
}
