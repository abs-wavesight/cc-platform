using Abs.CommonCore.Installer.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Installer.Tests
{
    public class CommandExecutionTests
    {
        [Fact]
        public async Task CommandExecuted()
        {
            var commandExecution = new CommandExecutionService(NullLoggerFactory.Instance);
            var tempFile = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });

            // del doesn't work directly
            await commandExecution.ExecuteCommandAsync("cmd.exe", $"cmd /c del {tempFile}", "");
            Assert.True(File.Exists(tempFile) == false);
        }

        [Fact]
        public async Task VerifyOnly_CommandNotExecuted()
        {
            var commandExecution = new CommandExecutionService(NullLoggerFactory.Instance, true);

            var exception = await Record.ExceptionAsync(() =>
                commandExecution.ExecuteCommandAsync("Not a valid command", "Not a valid argument", ""));
            Assert.Null(exception);
        }
    }
}