using Abs.CommonCore.Installer.Services;
using PasswordGenerator;

namespace Abs.CommonCore.Installer.Actions;
public class AddSftpUser
{
    private readonly ICommandExecutionService _commandExecutionService;

    public AddSftpUser(ICommandExecutionService commandExecutionService)
    {
        _commandExecutionService = commandExecutionService;
    }

    public async Task AddSftpUserAsync(string name, bool isDrex)
    {
        var pwd = new Password()
            .IncludeLowercase()
            .IncludeUppercase()
            .IncludeNumeric()
            .LengthRequired(32);
        var password = pwd.Next();

        const string containerName = "cc.sftp-service";
        var dockerCommand = $"exec -it {containerName} cmd.exe /C ";
        dockerCommand += $"dotnet Abs.CommonCore.SftpService.dll -u {name}";

        if (isDrex)
        {
            dockerCommand += $" -p \"{password}\" -d";
        }

        await _commandExecutionService.ExecuteCommandAsync("docker", dockerCommand, string.Empty);

        Console.WriteLine($"Restarting {containerName} container...");
        const string restartCommand = $"restart {containerName}";
        await _commandExecutionService.ExecuteCommandAsync("docker", restartCommand, string.Empty);
    }
}
