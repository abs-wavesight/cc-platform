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

        public CommandExecutionService(ILogger logger, bool verifyOnly = false)
        {
            _logger = logger;
            _verifyOnly = verifyOnly;
        }

        public async Task ExecuteCommandAsync(string command, string arguments)
        {
            _logger.LogInformation($"Executing: {command} {arguments}");

            if (_verifyOnly)
            {
                return;
            }

            var process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.ErrorDataReceived += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data) == false) _logger.LogError(args.Data);
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
