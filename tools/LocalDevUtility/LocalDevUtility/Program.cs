using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Abs.CommonCore.LocalDevUtility.Commands;
using Abs.CommonCore.LocalDevUtility.Helpers;
using Abs.CommonCore.LocalDevUtility.Models;
using Figgle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Abs.CommonCore.LocalDevUtility;

public class Program
{
    public static async Task<int> Main(string[]? args = null)
    {
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss.fff ";
                });
            });
        services.AddSingleton<IPowerShellAdapter, PowerShellAdapter>();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var powerShellAdapter = serviceProvider.GetRequiredService<IPowerShellAdapter>();

        return await Main(args, logger, powerShellAdapter);
    }

    public static async Task<int> Main(string[]? args, ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        Console.WriteLine(FiggleFonts.Doom.Render("ABS-WS | Common Core"));
        AnsiConsole.Write(new Rule("Local Dev Utility"));

        var root = BuildRootCommand();
        root.AddCommand(BuildConfigureCommand(logger, powerShellAdapter));
        root.AddCommand(BuildRunCommand(logger, powerShellAdapter));
        root.AddCommand(BuildStopCommand(logger, powerShellAdapter));
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

    private static Command BuildConfigureCommand(ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var command = new Command("configure", "Configure the utility via interactive prompts. Must be run at least once before using \"run\".");

        var printOption = new Option<bool?>("--print-only", "Print the current configuration without prompting for new values")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        printOption.AddAlias("-p");
        command.AddOption(printOption);

        command.Handler = CommandHandler.Create(async (ConfigureOptions configureOptions) =>
        {
            await ConfigureCommand.Configure(configureOptions, logger);
        });

        return command;
    }

    private static Command BuildRunCommand(ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var command = new Command("run", "Run Common Core components via Docker");

        var modeOption = new Option<RunMode?>("--mode", "r = run immediately, c = confirm before running, o = output and copy to clipboard without running")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        modeOption.AddAlias("-m");
        command.AddOption(modeOption);

        command.AddOption(GetRunComponentOption("drex-service"));
        command.AddOption(GetRunComponentOption("rabbitmq"));
        command.AddOption(GetRunComponentOption("vector"));
        command.AddOption(GetRunComponentOption("grafana"));
        command.AddOption(GetRunComponentOption("loki"));

        command.AddOption(GetFlagOption("deps", "d", "Run dependencies: RabbitMQ (local and remote), Vector"));
        command.AddOption(GetFlagOption("log-viz", "l", "Run log visualization components: Grafana, Loki"));
        command.AddOption(GetFlagOption("reset", "r", "Reset Docker"));
        command.AddOption(GetFlagOption("background", "b", "Run in background, a.k.a. detached (cannot be used with --abort-on-container-exit)"));
        command.AddOption(GetFlagOption("abort-on-container-exit", "a", "Abort if any container exits (cannot be used with --background)"));

        var siteConfigOverrideOption = new Option<string?>("--drex-site-config-file-name-override")
        {
            Arity = ArgumentArity.ZeroOrOne,
        };
        siteConfigOverrideOption.AddAlias("-s");
        command.AddOption(siteConfigOverrideOption);

        command.Handler = CommandHandler.Create(async (RunOptions runOptions) =>
        {
            await RunCommand.Run(runOptions, logger, powerShellAdapter);
        });

        return command;
    }

    private static Command BuildStopCommand(ILogger logger, IPowerShellAdapter powerShellAdapter)
    {
        var command = new Command("stop", "Stop compose components that may have been started in the background");

        command.AddOption(GetFlagOption("reset", "r", "Reset Docker"));

        command.Handler = CommandHandler.Create(async (StopOptions stopOptions) =>
        {
            await StopCommand.Stop(stopOptions, logger, powerShellAdapter);
        });

        return command;
    }

    private static Option<RunComponentMode?> GetRunComponentOption(string name)
    {
        return new Option<RunComponentMode?>($"--{name}", "Component: i = from image, s = from source")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
    }

    private static Option<bool?> GetFlagOption(string name, string alias, string description)
    {
        var option = new Option<bool?>($"--{name}", description)
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        option.AddAlias($"-{alias}");
        return option;
    }
}
