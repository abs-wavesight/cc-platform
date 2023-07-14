using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Installer.Services
{
    public class DataRequestService : IDataRequestService
    {
        private readonly ILogger _logger;
        private const string _nugetEnvironmentVariableName = "ABS_NUGET_PASSWORD";
        private readonly string? _nugetEnvironmentVariable;

        private readonly HttpClient _httpClient = new HttpClient();

        public DataRequestService(ILogger logger)
        {
            _logger = logger;
            _nugetEnvironmentVariable = Environment.GetEnvironmentVariable(_nugetEnvironmentVariableName);
            if (string.IsNullOrWhiteSpace(_nugetEnvironmentVariable))
            {
                throw new Exception("Nuget environment variable not found");
            }
        }

        public async Task<byte[]> RequestByteArrayAsync(string source)
        {
            _logger.LogInformation($"Loading data from '{source}'");
            using (var request = new HttpRequestMessage(HttpMethod.Get, source))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _nugetEnvironmentVariable);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.raw"));

                using (var response = await _httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
        }
    }
}
