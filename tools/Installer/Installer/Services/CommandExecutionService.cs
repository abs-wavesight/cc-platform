using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Abs.CommonCore.Installer.Services;

[ExcludeFromCodeCoverage]
public partial class CommandExecutionService : ICommandExecutionService
{
    [GeneratedRegex(@"(\u001b)(8|7|H|>|\]0;|\[(\?\d+(h|l)|[0-2]?(K|J)|\d*(A|B|C|D\D|E|F|G|H|I|M|N|S|T|U|X)|1000D\d+|\d*;\d*(f|H|r|m)|\d+;\d+;\d+m))", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex AnsiEscapeCodesRegex();

    private readonly bool _verifyOnly;
    private readonly ILogger _logger;

    public CommandExecutionService(ILoggerFactory loggerFactory, bool verifyOnly = false)
    {
        _logger = loggerFactory.CreateLogger<CommandExecutionService>();
        _verifyOnly = verifyOnly;
    }

    public async Task ExecuteCommandAsync(string command, string arguments, string workingDirectory, bool throwOnError = true)
    {
        _logger.LogInformation("Executing: {command} {arguments}", command, arguments);

        if (_verifyOnly)
        {
            return;
        }

        var isError = false;

        var process = new Process();
        process.StartInfo.FileName = "cmd"; // Use cmd for more extensibility
        process.StartInfo.Arguments = $"/C {command} {arguments}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = workingDirectory;

        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                _logger.LogError(args.Data?.Trim());
                isError = true;
            }
        };
        process.OutputDataReceived += (sender, args) =>
        {
            // ansi commands duplicate text data.
            // If remove only ANSI codes and log the rest of the string, text output will be logged multiple times.
            var isAnsiCommand = args.Data is not null
                && AnsiEscapeCodesRegex().IsMatch(args.Data);
            if (!isAnsiCommand && !string.IsNullOrWhiteSpace(args.Data))
            {
                var message = args.Data.Trim();
                if (message.Contains("error", StringComparison.CurrentCultureIgnoreCase)
                    || message.Contains("failed", StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogWarning(message);
                }
                else
                {
                    _logger.LogInformation(message);
                }
            }
        };
        process.Start();

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        await process.WaitForExitAsync();

        if (isError && throwOnError)
        {
            throw new Exception($"Error while executing command '{command} {arguments}'");
        }
    }
}
