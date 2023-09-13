namespace Abs.CommonCore.LocalDevUtility.Commands.TestDrex;

public class TestDrexOptions
{
    public string Name { get; init; }
    public Role? Role { get; init; }
    public Origin? Origin { get; init; }
    public bool? Loop { get; init; }
    public bool? File { get; init; }
    public FileInfo? Config { get; init; }
}
