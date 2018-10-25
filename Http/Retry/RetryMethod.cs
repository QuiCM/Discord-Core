using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Http.Retry
{
    /// <summary>
    /// Used to retry HTTP requests if they fail
    /// </summary>
    public abstract class RetryMethod
    {
        /// <summary>
        /// The number of times to retry
        /// </summary>
        public abstract int RetryAttempts { get; set; }

        /// <summary>
        /// Attempts to retry a GET request
        /// </summary>
        /// <param name="http"></param>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public abstract Task<HttpResponseMessage> RetryGet(
            HttpClient http,
            string url,
            System.Threading.CancellationToken token
        );

        /// <summary>
        /// Attempts to retry a POST, PUT, or PATCH request
        /// </summary>
        /// <param name="http"></param>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public abstract Task<HttpResponseMessage> RetryPost(
            HttpClient http, 
            string url, 
            HttpContent content, 
            System.Threading.CancellationToken token
        );
    }
}
