using Discord.Http.Retry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
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
        public Channels.ChannelRoutes Channels { get; private set; }
        public Uri BaseAddress { get; } = new Uri("https://discordapp.com/api/");

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

            Channels = new Channels.ChannelRoutes(this);
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
                string queryString = string.Join("&", queryParams.Select(param => $"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}"));
                url = _urlRegex.IsMatch(url) ? $"{url}&{queryString}" : $"{url}?{queryString}";
            }

            Uri uri = new Uri(url);
            
            HttpResponseMessage response = await Http.GetAsync(uri, ct);

            if (!response.IsSuccessStatusCode)
            {
                response = await RetryMethod.RetryGet(Http, uri, ct);
            }

            //if the response is still failed after retrying, throw an exception
            response.EnsureSuccessStatusCode();
            
            string json = await response.Content.ReadAsStringAsync();
            Trace.WriteLine($"HTTP GET '{uri}' returned:\n{json}");

            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Performs an HTTP POST request on an internet resource
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri"></param>
        /// <param name="content"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostAsync(
            string uri,
            HttpContent content,
            CancellationToken ct)
        {
            Uri requestAddress = new Uri(BaseAddress, uri);
            HttpResponseMessage response = await Http.PostAsync(requestAddress, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                response = await RetryMethod.RetryPost(Http, requestAddress, content, ct);
            }

            //if the response is still failed after retrying, throw an exception
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}
