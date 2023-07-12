using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Management.Automation;
using Abs.CommonCore.LocalDevUtility.Models;

namespace Abs.CommonCore.LocalDevUtility;

public static class Program
{
    public static async Task<int> Main(string[]? args)
    {
        var root = BuildRootCommand();
        root.AddCommand(BuildConfigureCommand());
        root.AddCommand(BuildRunCommand());
        root.AddCommand(BuildStopCommand());
        return await root.InvokeAsync(args ?? Array.Empty<string>());
    }

    private static RootCommand BuildRootCommand()
    {
        var root = new RootCommand("Utility to aid local development and testing");
        root.SetHandler(() =>
        {
            Console.WriteLine("You must use a sub-command to invoke this utility: \"configure\", \"run\", or \"stop\".");
        });
        return root;
    }

    private static Command BuildConfigureCommand()
    {
        var command = new Command("configure");

        var printOption = new Option<bool?>("--print-only")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        printOption.AddAlias("-p");
        command.AddOption(printOption);

        command.Handler = CommandHandler.Create(ConfigureCommand.Configure);

        return command;
    }

    private static Command BuildRunCommand()
    {
        var command = new Command("run");

        command.AddOption(GetRunComponentOption("drex-service"));
        command.AddOption(GetRunComponentOption("drex-test-client"));
        command.AddOption(GetRunComponentOption("rabbitmq"));
        command.AddOption(GetRunComponentOption("vector"));
        command.AddOption(GetRunComponentOption("grafana"));
        command.AddOption(GetRunComponentOption("loki"));

        command.AddOption(GetFlagOption("deps", "d"));
        command.AddOption(GetFlagOption("log-viz", "l"));
        command.AddOption(GetFlagOption("reset", "r"));
        command.AddOption(GetFlagOption("background", "b"));
        command.AddOption(GetFlagOption("abort-on-container-exit", "a"));

        command.Handler = CommandHandler.Create(Run);

        return command;
    }

    private static Command BuildStopCommand()
    {
        var command = new Command("stop");
        return command;
    }

    private static async Task<int> Run(RunOptions runOptions)
    {
        var appConfig = (await ConfigureCommand.ReadConfig())!;
        // ConfigureCommand.ValidateConfigAndThrow(appConfig); // TODO RH: Uncomment

        // await CreateEnvFile(appConfig); // TODO RH: Uncomment

        // TODO RH: Add all "-f" commands
        var composeCommand = "docker-compose --help";

        RunCommand(composeCommand);

        // Run command in powershell



        return 0;
    }

    // private static (string output, string error) RunCommand(string command)
    // {
    //     using var process = new Process();
    //     process.StartInfo = new ProcessStartInfo("powershell.exe", command)
    //     {
    //         RedirectStandardOutput = true,
    //         RedirectStandardError = true
    //     };
    //     process.Start();
    //     var output = process.StandardOutput.ReadToEnd();
    //     var error = process.StandardError.ReadToEnd();
    //     return (output, error);
    // }

    private static void RunCommand(string command)
    {
        using (var ps = PowerShell.Create())
        {
            ps.AddScript(command);
            ps.AddCommand("Out-String").AddParameter("Stream", true);

            var output = new PSDataCollection<string>();
            output.DataAdded += ProcessCommandStandardOutput;
            ps.Streams.Error.DataAdded += ProcessCommandErrorOutput;

            var asyncToken = ps.BeginInvoke<object, string>(null, output);

            if (asyncToken.AsyncWaitHandle.WaitOne())
            {
                ps.EndInvoke(asyncToken);
            }

            ps.InvokeAsync();
        }
    }

    private static void ProcessCommandStandardOutput(object? sender, DataAddedEventArgs eventArgs)
    {
        if (sender is not PSDataCollection<string> collection) return;
        var outputItem = collection[eventArgs.Index];
        Console.WriteLine(outputItem);
    }

    private static void ProcessCommandErrorOutput(object? sender, DataAddedEventArgs eventArgs)
    {
        if (sender is not PSDataCollection<string> collection) return;
        var outputItem = collection[eventArgs.Index];
        Console.Error.WriteLine(outputItem);
    }

    private static async Task CreateEnvFile(AppConfig appConfig)
    {
        var envValues = new Dictionary<string, string>
        {
            {"WINDOWS_VERSION", appConfig.ContainerWindowsVersion},
            {"PATH_TO_CC_PLATFORM_REPO", appConfig.CommonCorePlatformRepositoryPath},
            {"PATH_TO_CC_DREX_REPO", appConfig.CommonCoreDrexRepositoryPath}
        };
        var envFileText = string.Join("\n", envValues.Select(_ => $"{_.Key}={_.Value}"));
        var envFileName = Path.Combine(appConfig.CommonCorePlatformRepositoryPath, Constants.EnvFileRelativePath);
        await File.WriteAllTextAsync(envFileName, envFileText);
    }

    private static Option<RunComponentMode?> GetRunComponentOption(string name)
    {
        return new Option<RunComponentMode?>($"--{name}")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private static Option<bool?> GetFlagOption(string name, string? alias = null)
    {
        var option = new Option<bool?>($"--{name}");
        if (!string.IsNullOrEmpty(alias))
        {
            option.AddAlias($"-{alias}");
        }
        return option;
    }
}
