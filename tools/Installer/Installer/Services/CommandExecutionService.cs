using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Services
{
    public class CommandExecutionService : ICommandExecutionService
    {
        private readonly ILogger _logger;

        public CommandExecutionService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task ExecuteCommandAsync(string command, string arguments)
        {
            _logger.LogInformation($"Executing: {command} {arguments}");

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
