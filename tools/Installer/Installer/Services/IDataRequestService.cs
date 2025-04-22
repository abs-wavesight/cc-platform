namespace Abs.CommonCore.Installer.Services;

public interface IDataRequestService
{
    public Task<Stream> RequestByteArrayAsync(string source);
}
