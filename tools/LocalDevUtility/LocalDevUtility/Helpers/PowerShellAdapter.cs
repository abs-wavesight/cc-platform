using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;

namespace Abs.CommonCore.LocalDevUtility.Helpers;

public class PowerShellAdapter : IPowerShellAdapter
{
    private readonly ConcurrentDictionary<string, string> _colorsByContainerName = new();

    public List<string> RunPowerShellCommand(string command, TimeSpan? timeout)
    {
        return RunPowerShellCommand(command, NullLogger.Instance, timeout);
    }

    public List<string> RunPowerShellCommand(string command, ILogger logger, TimeSpan? timeout)
    {
        logger.LogInformation($"Executing command: {command}");

        var output = new List<string>();

        var process = new Process();
        process.StartInfo.FileName = "powershell.exe"; // Use cmd for more extensibility
        process.StartInfo.Arguments = $"-Command \"\"{command}\"\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;

        process.ErrorDataReceived += (sender, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Data) == false) ProcessCommandOutput(output, logger, sender, args?.Data?.Trim() ?? "");
        };
        process.OutputDataReceived += (sender, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Data) == false) ProcessCommandOutput(output, logger, sender, args?.Data?.Trim() ?? "");
        };
        process.Start();

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        var task = process.WaitForExitAsync();
        if (timeout is not null)
        {
            task = task
                .WaitAsync(timeout.Value);
        }

        task
            .GetAwaiter()
            .GetResult();

        return output;
    }

    private void ProcessCommandOutput(List<string> rawOutput, ILogger logger, object? sender, string data)
    {
        OutputWithColorForDockerCompose(data!);
        rawOutput.Add(data!);

        logger.LogInformation(data);
    }

    /// <summary>
    /// Hack to get back colors-by-container in docker compose output
    /// </summary>
    /// <param name="rawOutput"></param>
    private void OutputWithColorForDockerCompose(string rawOutput)
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
