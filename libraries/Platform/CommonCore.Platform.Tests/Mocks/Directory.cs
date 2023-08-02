namespace Abs.CommonCore.Platform.Tests.Mocks
{
    public static class Directory
    {
        public static DirectoryInfo CreateDirectory(string path)
        {
            return new DirectoryInfo(path);
        }

        public static bool Exists(string path)
        {
            return false;
        }

        public static void Delete(string path, bool recursive)
        { }
    }
}
