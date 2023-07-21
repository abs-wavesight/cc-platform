namespace Abs.CommonCore.Platform.Extensions
{
    public static class TaskExtensions
    {
        #region Public Methods

        public static Task WhenAllAsync(this IEnumerable<Task> tasks)
        {
            return Task.WhenAll(tasks);
        }

        public static Task<TResult[]> WhenAllAsync<TResult>(this IEnumerable<Task<TResult>> tasks)
        {
            return Task.WhenAll(tasks);
        }

        public static async Task WhenAnyAsync(this IEnumerable<Task> tasks)
        {
            await await Task.WhenAny(tasks);
        }

        public static async Task<TResult> WhenAnyAsync<TResult>(this IEnumerable<Task<TResult>> tasks)
        {
            return await await Task.WhenAny(tasks);
        }

        #endregion Public Methods
    }
}
