namespace Abs.CommonCore.Platform.Extensions
{
    public static class SelectAsyncExtensions
    {
        public static async Task<IReadOnlyList<TResult>> SelectManyAsync<TItem, TResult>(this IEnumerable<TItem> items,
            Func<TItem, Task<IEnumerable<TResult>>> itemAction,
            int maxDegreesOfParallelism = 0,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            items = items ?? throw new ArgumentNullException(nameof(items));
            itemAction = itemAction ?? throw new ArgumentNullException(nameof(itemAction));

            var data = new List<TResult>();

            await items
                .ForAllAsync(async x =>
                {
                    var newItems = await itemAction(x);

                    // More efficient from multiple threads than ConcurrentBag
                    lock (data)
                    {
                        data.AddRange(newItems);
                    }
                }, maxDegreesOfParallelism, cancellationToken: cancellationToken);

            return data;
        }

        public static async Task<IReadOnlyList<TResult>> SelectAsync<TItem, TResult>(this IEnumerable<TItem> items,
            Func<TItem, Task<TResult>> itemAction,
            int maxDegreesOfParallelism = 0)
        {
            items = items ?? throw new ArgumentNullException(nameof(items));
            itemAction = itemAction ?? throw new ArgumentNullException(nameof(itemAction));

            var data = new List<TResult>();

            await items
                .ForAllAsync(async x =>
                {
                    var newItem = await itemAction(x);

                    lock (data)
                    {
                        data.Add(newItem);
                    }
                }, maxDegreesOfParallelism);

            return data;
        }
    }
}
