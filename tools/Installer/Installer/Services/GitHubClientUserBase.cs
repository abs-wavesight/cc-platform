using Octokit;

namespace Abs.CommonCore.Installer.Services;

public abstract class GitHubClientUserBase
{
    private readonly string? _nugetEnvironmentVariable =
        Environment.GetEnvironmentVariable(Constants.NugetEnvironmentVariableName);

    protected GitHubClient GetClient()
    {
        return new GitHubClient(new ProductHeaderValue(Constants.AbsHeaderValue))
        {
            Credentials = new Credentials(_nugetEnvironmentVariable)
        };
    }
}
