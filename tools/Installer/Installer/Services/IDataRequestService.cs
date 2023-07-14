namespace Abs.CommonCore.Installer.Services
{
    public interface IDataRequestService
    {
        public Task<byte[]> RequestByteArrayAsync(string source);
    }
}
