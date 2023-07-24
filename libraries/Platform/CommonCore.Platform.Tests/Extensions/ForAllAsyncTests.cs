using Abs.CommonCore.Platform.Extensions;
using Xunit;

namespace Abs.CommonCore.Platform.Tests.Extensions
{
    public class ForAllAsyncExtensionTests
    {
        [Fact]
        public async Task TestForAllAsync()
        {
            var items = Enumerable.Range(0, 100)
                .Select(i => DateTime.Now.Ticks % 1000)
                .ToArray();

            var concurrentSum = 0L;
            await items
                .ForAllAsync(async x =>
                {
                    await Task.Yield(); // Force concurrency
                    Interlocked.Add(ref concurrentSum, x);
                });

            var expectedSum = items.Sum();

            Assert.Equal(expectedSum, concurrentSum);
        }
    }
}
