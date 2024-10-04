using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Abs.CommonCore.Installer.Extensions;
using Octokit;

namespace Abs.CommonCore.Installer.Services;

[ExcludeFromCodeCoverage]
public class DataRequestService : IDataRequestService
{
    private readonly ILogger _logger;
    private readonly string? _nugetEnvironmentVariable;
    private readonly bool _verifyOnly;

    private readonly HttpClient _httpClient = new();

    public DataRequestService(ILoggerFactory loggerFactory, bool verifyOnly = false)
    {
        _logger = loggerFactory.CreateLogger<DataRequestService>();
        _nugetEnvironmentVariable = Environment.GetEnvironmentVariable(Constants.NugetEnvironmentVariableName);
        _verifyOnly = verifyOnly;

        if (verifyOnly == false && string.IsNullOrWhiteSpace(_nugetEnvironmentVariable))
        {
            throw new Exception("Nuget environment variable not found");
        }
    }

    public async Task<byte[]> RequestByteArrayAsync(string source)
    {
        _logger.LogInformation($"Loading data from '{source}'");

        return _verifyOnly
            ? Array.Empty<byte>()
            : source.Contains("github.com", StringComparison.OrdinalIgnoreCase) && source.Contains("/releases/", StringComparison.OrdinalIgnoreCase)
            ? await DownloadGithubReleaseFileAsync(source)
            : source.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
            ? await DownloadGithubRawFileAsync(source)
            : await DownloadDirectFileAsync(source);
    }

    private async Task<byte[]> DownloadGithubReleaseFileAsync(string source)
    {
        var client = new GitHubClient(new Octokit.ProductHeaderValue(Constants.AbsHeaderValue))
        {
            Credentials = new Credentials(_nugetEnvironmentVariable)
        };

        var segments = source.GetGitHubPathSegments();
        var release = await client.Repository.Release.Get(segments.Owner, segments.Repo, segments.Tag);
        var asset = release.Assets
            .First(x => string.Equals(x.Name, segments.File));

        var uri = new Uri(asset.Url, UriKind.Absolute);
        var response = await client.Connection.Get<byte[]>(uri, new Dictionary<string, string>(), "application/octet-stream");
        return response.Body;
    }

    private Task<byte[]> DownloadGithubRawFileAsync(string source)
    {
        return DownloadFileAsync(source, true);
    }

    private Task<byte[]> DownloadDirectFileAsync(string source)
    {
        return DownloadFileAsync(source, false);
    }

    private async Task<byte[]> DownloadFileAsync(string source, bool isGithub)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, source);
        if (isGithub)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nugetEnvironmentVariable);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.raw"));
        }

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
