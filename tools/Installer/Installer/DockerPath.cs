namespace Abs.CommonCore.Installer;
public static class DockerPath
{
    public const string DockerFilePath = @"c:\\docker\\docker.exe";
    private const string _dockerExe = "docker";

    private const string _dockerComposePath = @"c:\\docker\\docker-compose.exe";
    private const string _dockerComposeExe = "docker-compose";

    public static string GetDockerPath()
    {
        var exists = File.Exists(DockerFilePath);
        return exists
            ? DockerFilePath
            : _dockerExe;
    }

    public static string GetDockerComposePath()
    {
        var exists = File.Exists(_dockerComposePath);
        return exists
            ? _dockerComposePath
            : _dockerComposeExe;
    }
}
