using Discord.Http.Retry;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Http
{
    /// <summary>
    /// Provides access to Discord's REST API
    /// </summary>
    public partial class Rest
    {
        /// <summary>
        /// Base URL to Discord's REST API
        /// </summary>
        public const string RestBaseUrl = "https://discordapp.com/api/";

        /// <summary>
        /// HTTP PATCH method
        /// </summary>
        public HttpMethod Patch = new HttpMethod("PATCH");
        /// <summary>
        /// RetryMethod used when a HTTP request does not complete with a success code
        /// </summary>
        public RetryMethod RetryMethod { get; set; } = new DelayedRetry();

        private Regex _urlRegex = new Regex(".+\\?\\w+=.+(?>&\\w+=.+)*?");
        private Credentials.Credentials _credentials;
        private string _userAgent;
        private string _userAgentVersion;

        /// <summary>
        /// HttpClient used to perform HTTP requests
        /// </summary>
        protected HttpClient Http { get; set; }

        /// <summary>
        /// HttpClientHandler used to configure the HttpClient that will make requests
        /// </summary>
        public HttpClientHandler HttpHandler { get; } = new HttpClientHandler();

        /// <summary>
        /// Constructs a new Rest instance that will use the given credentials to authenticate calls
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="userAgentUrl"></param>
        /// <param name="userAgentVersion"></param>
        public Rest(Credentials.Credentials credentials, string userAgentUrl, string userAgentVersion)
        {
            _credentials = credentials;
            _userAgent = userAgentUrl;
            _userAgentVersion = userAgentVersion;

            Http = new HttpClient(HttpHandler);
            Http.DefaultRequestHeaders.Add("User-Agent", $"DiscordBot ({_userAgent}, {_userAgentVersion})");
            Http.DefaultRequestHeaders.Add("Authorization", _credentials.AuthToken);
        }

        /// <summary>
        /// Performs an HTTP GET request on an internet resource
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="queryParams"></param>
        /// <param name="ct"></param>
        /// <exception cref="HttpRequestException">If the request is unsuccessful</exception>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(
            string url,
            IEnumerable<KeyValuePair<string, string>> queryParams,
            CancellationToken ct)
        {
            if (queryParams != null)
            {
                string queryString = string.Join("&", queryParams.Select(param => $"{param.Key}={param.Value}"));
                url = _urlRegex.IsMatch(url) ? $"{url}&{queryString}" : $"{url}?{queryString}";
            }
            
            HttpResponseMessage response = await Http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                response = await RetryMethod.RetryGet(Http, url, ct);
            }

            //if the response is still failed after retrying, throw an exception
            response.EnsureSuccessStatusCode();
            
            string json = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"HTTP GET '{url}' returned:\n{json}");

            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Performs an HTTP POST request on an internet resource
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<T> PostAsync<T>(
            string url,
            HttpContent content,
            CancellationToken ct)
        {
            HttpResponseMessage response = await Http.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                response = await RetryMethod.RetryPost(Http, url, content, ct);
            }

            //if the response is still failed after retrying, throw an exception
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"HTTP POST '{url}'; content '{content}' returned:\n{json}");
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
