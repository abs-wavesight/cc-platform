namespace Abs.CommonCore.Platform.ConfigFolderWatcher
{
    public interface IConfigFolderWatcher : IDisposable
    {
        event EventHandler<string>? Changed;
        event EventHandler<string>? Added;
        event EventHandler<string>? Deleted;
        event EventHandler<Exception>? Failed;
    }
}
