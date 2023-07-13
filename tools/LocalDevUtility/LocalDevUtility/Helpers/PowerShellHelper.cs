using System.Collections.Concurrent;
using System.Management.Automation;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Abs.CommonCore.LocalDevUtility.Helpers;

public static class PowerShellHelper
{
    private static readonly ConcurrentDictionary<string,string> _colorsByContainerName = new ();

    public static void RunPowerShellCommand(string command)
    {
        using var ps = PowerShell.Create();
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

    private static void ProcessCommandStandardOutput(object? sender, DataAddedEventArgs eventArgs)
    {
        if (sender is not PSDataCollection<string> collection) return;
        var outputItem = collection[eventArgs.Index];
        OutputWithColorForDockerCompose(outputItem);
    }

    private static void ProcessCommandErrorOutput(object? sender, DataAddedEventArgs eventArgs)
    {
        if (sender is not PSDataCollection<string> collection) return;
        var outputItem = collection[eventArgs.Index];
        Console.Error.WriteLine(outputItem);
    }

    /// <summary>
    /// Hack to get back colors-by-container in docker compose output
    /// </summary>
    /// <param name="rawOutput"></param>
    private static void OutputWithColorForDockerCompose(string rawOutput)
    {
        var colorStack = new Stack<string>();
        colorStack.Push("fuchsia");
        colorStack.Push("blue");
        colorStack.Push("aqua");
        colorStack.Push("lime");
        colorStack.Push("yellow");
        colorStack.Push("teal");
        colorStack.Push("purple");
        colorStack.Push("navy");
        colorStack.Push("olive");
        colorStack.Push("green");
        colorStack.Push("maroon");

        // Regex to extract the starting string including the container name from the following: "my-container-name_1  | the log message goes here"
        var containerNameRegex = new Regex(@"[\w-]+?\s+\|\s+");
        var match = containerNameRegex.Match(rawOutput);
        if (match.Success)
        {
            var containerName = match.Value.Replace("|", "").Trim();
            string? color = null;
            if (_colorsByContainerName.TryGetValue(containerName, out var value))
            {
                color = value;
            }
            else if (colorStack.Any())
            {
                do
                {
                    color = colorStack.Pop();
                } while (_colorsByContainerName.Any(_ => _.Value == color));
                _colorsByContainerName.TryAdd(containerName, color);
            }

            var coloredOutputPart = $"[{color}]{match.Value}[/]";
            var uncoloredOutputPart = rawOutput.Substring(match.Value.Length).EscapeMarkup();
            AnsiConsole.MarkupLine($"{coloredOutputPart}{uncoloredOutputPart}");
            return;
        }

        Console.WriteLine(rawOutput);
    }
}
