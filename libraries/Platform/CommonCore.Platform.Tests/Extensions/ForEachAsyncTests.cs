using Xunit;

namespace Abs.CommonCore.Platform.Tests.Extensions
{
    public class ForEachAsyncExtensionTests
    {
        [Fact]
        public async Task TestForEachAsync()
        {
            var items = Enumerable.Range(0, 100)
                .Select(i => DateTime.Now.Ticks % 1000)
                .ToArray();

            var concurrentSum = 0L;
            await items
                .ForEachAsync(async x =>
                {
                    await Task.Yield(); // Force concurrency
                    concurrentSum += x;
                });

            var expectedSum = items.Sum();

            Assert.Equal(expectedSum, concurrentSum);
        }

        [Fact]
        public async Task TestForEachAsync_Index()
        {
            var items = Enumerable.Range(0, 100)
                .Select(i => DateTime.Now.Ticks % 1000)
                .ToArray();

            var concurrentSum = 0L;
            await items
                .ForEachAsync(async (x, index) =>
                {
                    await Task.Yield(); // Force concurrency
                    concurrentSum += (x + index);
                });

            var expectedSum = items
                .Select((x, i) => x + i)
                .Sum();

            Assert.Equal(expectedSum, concurrentSum);
        }
    }
}
