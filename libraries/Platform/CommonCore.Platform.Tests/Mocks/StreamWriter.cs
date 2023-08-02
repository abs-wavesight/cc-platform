using System.Diagnostics;

namespace Abs.CommonCore.Platform.Tests.Mocks
{
    public class StreamWriter : IDisposable
    {
        public void Dispose() => Trace.WriteLine("Fake StreamWriter Dispose");

        public void WriteLine(string text) => Trace.WriteLine($"Fake StreamWriter WriteLine {text}");
    }
}
