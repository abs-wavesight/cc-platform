using System.Diagnostics.CodeAnalysis;

namespace Abs.CommonCore.SftpService;

[ExcludeFromCodeCoverage]
public class SftpUser
{
    public string Name { get; init; }
    public string Password { get; init; }
    public string Root { get; init; }
}
