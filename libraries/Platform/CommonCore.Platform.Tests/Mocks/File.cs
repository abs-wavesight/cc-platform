using System.Diagnostics;

namespace Abs.CommonCore.Platform.Tests.Mocks
{
    public static class File
    {
        public static StreamWriter CreateText(string path)
        {
            return new StreamWriter();
        }

        public static bool Exists(string path)
        {
            return true;
        }

        public static void Delete(string path)
        { }

        public static void WriteAllText(string filePath, string text) =>
            Trace.WriteLine($"Fake File WriteAllText FilePath: {filePath} Text: {text}");
    }
}
