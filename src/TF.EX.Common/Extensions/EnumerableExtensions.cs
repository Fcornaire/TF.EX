namespace TF.EX.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static async Task ForEachAsync<TSource>(
            this IEnumerable<TSource> source,
            int degreeOfParallelism,
            Func<TSource, Task> asyncAction)
        {
            using var semaphore = new SemaphoreSlim(degreeOfParallelism);
            var tasks = source.Select(async item =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    await asyncAction(item).ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
