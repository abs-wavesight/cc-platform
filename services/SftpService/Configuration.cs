using System.Diagnostics.CodeAnalysis;

namespace Abs.CommonCore.SftpService;

[ExcludeFromCodeCoverage]
public class Configuration
{
    public List<string> Clients { get; init; }
    public List<SiteUser> Sites { get; init; }
    public string DefaultPassword { get; init; }
}

public class SiteUser
{
    public string Username { get; init; }
    public string Password { get; init; }
}
