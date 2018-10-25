using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Http.Retry
{
    /// <summary>
    /// A retry method that retries an HTTP request after a delay
    /// </summary>
    public class DelayedRetry : RetryMethod
    {
        /// <summary>
        /// Base number of milliseconds to delay
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;
        /// <summary>
        /// Number of times to attempt retrying
        /// </summary>
        public override int RetryAttempts { get; set; } = 3;

        /// <summary>
        /// Determines how long to delay based on the number of times already retried.
        /// Defaults to <code><paramref name="retryCount"/>*<see cref="RetryDelayMs"/></code>
        /// </summary>
        /// <param name="retryCount">Number of times already retried</param>
        /// <returns></returns>
        public virtual int DelayFactor(int retryCount)
        {
            return retryCount * RetryDelayMs;
        }

        /// <summary>
        /// Attempts to retry a GET request
        /// </summary>
        /// <param name="http"></param>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override async Task<HttpResponseMessage> RetryGet(HttpClient http, string url, CancellationToken token)
        {
            HttpResponseMessage response;
            int retries = 1;
            do
            {
                await Task.Delay(DelayFactor(retries), token);
                response = await http.GetAsync(url, token);
                token.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                retries++;
            }
            while (retries <= RetryAttempts);

            return response;
        }

        /// <summary>
        /// Attempts to retry a POST, PUT, or PATCH request
        /// </summary>
        /// <param name="http"></param>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override async Task<HttpResponseMessage> RetryPost(HttpClient http, string url, HttpContent content, CancellationToken token)
        {
            HttpResponseMessage response;
            int retries = 1;
            do
            {
                await Task.Delay(DelayFactor(retries), token);
                response = await http.PostAsync(url, content, token);
                token.ThrowIfCancellationRequested();

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                retries++;
            }
            while (retries <= RetryAttempts);

            return response;
        }
    }
}
