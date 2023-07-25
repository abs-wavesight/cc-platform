namespace Abs.CommonCore.Platform.Extensions
{
    public static class ForEachAsyncExtensions
    {
        public static async Task ForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
        {
            collection = collection ?? throw new ArgumentNullException(nameof(collection));
            asyncItemAction = asyncItemAction ?? throw new ArgumentNullException(nameof(asyncItemAction));

            foreach (var item in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await asyncItemAction(item);
            }
        }

        public static async Task ForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
        {
            collection = collection ?? throw new ArgumentNullException(nameof(collection));
            asyncItemAction = asyncItemAction ?? throw new ArgumentNullException(nameof(asyncItemAction));

            var index = 0;
            foreach (var item in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await asyncItemAction(item, index);

                index++;
            }
        }
    }
}
