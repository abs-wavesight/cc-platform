using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Services
{
    [ExcludeFromCodeCoverage]
    public class CommandExecutionService : ICommandExecutionService
    {
        private readonly bool _verifyOnly;
        private readonly ILogger _logger;

        public CommandExecutionService(ILoggerFactory loggerFactory, bool verifyOnly = false)
        {
            _logger = loggerFactory.CreateLogger<CommandExecutionService>();
            _verifyOnly = verifyOnly;
        }

        public async Task ExecuteCommandAsync(string command, string arguments, string workingDirectory)
        {
            _logger.LogInformation($"Executing: {command} {arguments}");

            if (_verifyOnly)
            {
                return;
            }

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
                // Intentional not logged to error
                if (string.IsNullOrWhiteSpace(args.Data) == false) _logger.LogInformation(args.Data);
            };
            process.OutputDataReceived += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data) == false) _logger.LogInformation(args.Data);
            };
            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await process.WaitForExitAsync();
        }
    }
}
