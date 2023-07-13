using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Abs.CommonCore.LocalDevUtility.Models;
using Figgle;
using Spectre.Console;

namespace Abs.CommonCore.LocalDevUtility;

public static class Program
{
    public static async Task<int> Main(string[]? args)
    {
        Console.WriteLine(FiggleFonts.Doom.Render("ABS-WS | Common Core"));
        AnsiConsole.Write(new Rule("Local Dev Utility"));

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

        command.Handler = CommandHandler.Create(RunCommand.Run);

        return command;
    }

    private static Command BuildStopCommand()
    {
        var command = new Command("stop");
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
