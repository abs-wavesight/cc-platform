namespace CommonCore.Platform.Extensions
{
    public static class ListExtensions
    {
        public static void AddIfNotNullOrEmpty(this List<string> list, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            list.Add(value);
        }

        public static string StringJoin<T>(this IEnumerable<T> list, string delimiter)
        {
            return string.Join(delimiter, list);
        }

        public static string StringJoin<T>(this IEnumerable<T> list, char delimiter)
        {
            return string.Join(delimiter, list);
        }
    }
}
