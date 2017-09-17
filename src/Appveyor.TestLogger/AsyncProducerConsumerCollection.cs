namespace Microsoft.VisualStudio.TestPlatform.Extensions.Appveyor.TestLogger
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <remarks>
    /// Adopted from https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/consuming-the-task-based-asynchronous-pattern
    /// </remarks>
    internal class AsyncProducerConsumerCollection<T>
    {
        private readonly Queue<T> collection = new Queue<T>();
        private readonly Queue<TaskCompletionSource<T[]>> waiting = new Queue<TaskCompletionSource<T[]>>();
        private bool canceled = false;

        public void Cancel()
        {
            TaskCompletionSource<T[]>[] allWaiting;
            lock (collection)
            {
                canceled = true;
                allWaiting = waiting.ToArray();
                waiting.Clear();
            }

            foreach (var tcs in allWaiting)
            {
                tcs.TrySetResult(new T[] { });
            }
        }

        public void Add(T item)
        {
            TaskCompletionSource<T[]> tcs = null;
            lock (collection)
            {
                if (waiting.Count > 0) tcs = waiting.Dequeue();
                else collection.Enqueue(item);
            }

            tcs?.TrySetResult(new [] {item});
        }

        /// <summary>
        /// Queue producer for consumers to <c>await</c> on.
        /// </summary>
        /// <returns>Array of all available items.  Empty array if queue is being canceled.</returns>
        public Task<T[]> TakeAsync()
        {
            lock (collection)
            {
                if (collection.Count > 0)
                {
                    var result = Task.FromResult(collection.ToArray());
                    collection.Clear();
                    return result;
                }
                else if (canceled == false)
                {
                    var tcs = new TaskCompletionSource<T[]>();
                    waiting.Enqueue(tcs);
                    return tcs.Task;
                }
                else // canceled == true
                {
                    return Task.FromResult(new T[] { });
                }
            }
        }
    }
}