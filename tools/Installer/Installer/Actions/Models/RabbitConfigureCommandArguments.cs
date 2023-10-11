namespace Abs.CommonCore.Installer.Actions.Models;

public class RabbitConfigureCommandArguments
{
    public Uri? Rabbit { get; init; }
    public string? RabbitUsername { get; init; }
    public string? RabbitPassword { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UpdatePermissions { get; init; }
    public FileInfo? DrexSiteConfig { get; init; }
    public AccountType AccountType { get; init; }
    public FileInfo? CredentialsFile { get; init; }
}
