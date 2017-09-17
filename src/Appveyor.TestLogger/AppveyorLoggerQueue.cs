namespace Microsoft.VisualStudio.TestPlatform.Extensions.Appveyor.TestLogger
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class AppveyorLoggerQueue
    {
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// it is localhost with a random port, e.g. http://localhost:9023/
        /// </summary>
        private readonly string appveyorApiUrl;

        private readonly AsyncProducerConsumerCollection<string> queue = new AsyncProducerConsumerCollection<string>();
        private readonly Task consumeTask;
        private readonly CancellationTokenSource consumeTaskCancellationSource = new CancellationTokenSource();

        private int totalEnqueued = 0;
        private int totalSent = 0;

        public AppveyorLoggerQueue(string appveyorApiUrl)
        {
            this.appveyorApiUrl = appveyorApiUrl;
            this.consumeTask = ConsumeItemsAsync(consumeTaskCancellationSource.Token);
        }

        public void Enqueue(string json)
        {
            queue.Add(json);
            totalEnqueued++;
        }

        public void Flush()
        {
            // Cancel any idle consumers and let them return
            queue.Cancel();

            try
            {
                // any active consumer will circle back around and batch post the remaining queue.
                consumeTask.Wait(TimeSpan.FromSeconds(60));

                // Cancel any active HTTP requests if still hasn't finished flushing
                consumeTaskCancellationSource.Cancel();
                if (!consumeTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("cancellation didn't happen quickly");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

#if DEBUG
            Console.WriteLine("Appveyor.TestLogger: {0} test results reported ({1} enqueued).", totalSent, totalEnqueued);
#endif
        }

        private async Task ConsumeItemsAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                string[] nextItems = await this.queue.TakeAsync();
                if (nextItems == null || nextItems.Length == 0) return;      // Queue is cancelling and and empty.

                if (nextItems.Length == 1) await PostItemAsync(nextItems[0], cancellationToken);
                else if (nextItems.Length > 1) await PostBatchAsync(nextItems, cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;
            }
        }

        private async Task PostItemAsync(string json, CancellationToken cancellationToken)
        {
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync(appveyorApiUrl + "api/tests", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                totalSent += 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task PostBatchAsync(ICollection<string> jsonEntities, CancellationToken cancellationToken)
        {
            var jsonArray = "[" + string.Join(",", jsonEntities) + "]";
            HttpContent content = new StringContent(jsonArray, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync(appveyorApiUrl + "api/tests/batch", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                totalSent += jsonEntities.Count;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}