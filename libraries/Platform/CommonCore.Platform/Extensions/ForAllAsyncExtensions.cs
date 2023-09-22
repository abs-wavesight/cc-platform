namespace Abs.CommonCore.Platform.Extensions;

public static class ForAllAsyncExtensions
{
    public static Task ForAllAsync<T>(
        this IEnumerable<T> collection,
        Func<T, Task> asyncItemAction,
        int maxDegreesOfParallelism = 0,
        CancellationToken cancellationToken = default)
    {
        collection = collection ?? throw new ArgumentNullException(nameof(collection));
        asyncItemAction = asyncItemAction ?? throw new ArgumentNullException(nameof(asyncItemAction));

        if (maxDegreesOfParallelism <= 0)
        {
            return Parallel.ForEachAsync(collection, cancellationToken, (i, c) => ProcessItemAsync(i, c, asyncItemAction));
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreesOfParallelism,
            CancellationToken = cancellationToken
        };

        return Parallel.ForEachAsync(collection, options, (i, c) => ProcessItemAsync(i, c, asyncItemAction));
    }

    private static ValueTask ProcessItemAsync<T>(T item, CancellationToken cancellation, Func<T, Task> asyncItemAction)
    {
        cancellation.ThrowIfCancellationRequested();

        var task = asyncItemAction(item);
        return new ValueTask(task);
    }
}
