using Octokit;

namespace Abs.CommonCore.Installer.Actions;

public class ReleaseVersionProvider : ActionBase
{
    private readonly string? _nugetEnvironmentVariable = Environment.GetEnvironmentVariable(Constants.NugetEnvironmentVariableName);

    public async Task<string?> GetVersionAsync(string releaseName, string owner, string repoName)
    {
        var client = new GitHubClient(new ProductHeaderValue(Constants.AbsHeaderValue));
        client.Credentials = new Credentials(_nugetEnvironmentVariable);

        var releaseVersion = (await client.Repository.Release.GetAll(owner, repoName))
            .OrderByDescending(r => r.PublishedAt)
            .Select(r => r.Name)
            .FirstOrDefault(n => n.StartsWith(releaseName))
            ?[releaseName.Length..]
            ?.Trim();

        return releaseVersion;
    }
}
