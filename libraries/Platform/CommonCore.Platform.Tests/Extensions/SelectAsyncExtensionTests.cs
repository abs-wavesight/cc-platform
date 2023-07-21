using Xunit;

namespace Abs.CommonCore.Platform.Tests.Extensions
{
    public class SelectAsyncExtensionTests
    {
        [Fact]
        public async Task TestSelectManyAsync()
        {
            var items = Enumerable.Range(0, 100)
                .Select(i => DateTime.Now.Ticks % 1000)
                .ToArray();

            var results = await items
                .SelectManyAsync<long, long>(async x =>
                {
                    await Task.Yield();

                    return new[] { x - 1, x, x + 1 };
                });

            var asyncSum = results.Sum();
            var expectedSum = items
                .SelectMany(x => new[] { x - 1, x, x + 1 })
                .Sum();

            Assert.Equal(expectedSum, asyncSum);
        }

        [Fact]
        public async Task TestSelectAsync()
        {
            var items = Enumerable.Range(0, 100)
                .Select(i => DateTime.Now.Ticks % 1000)
                .ToArray();

            var results = await items
                .SelectAsync<long, long>(async x =>
                {
                    await Task.Yield();

                    return x * x;
                });

            var asyncSum = results.Sum();
            var expectedSum = items
                .Select(x => x * x)
                .Sum();

            Assert.Equal(expectedSum, asyncSum);
        }
    }
}
