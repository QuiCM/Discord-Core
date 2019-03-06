using Newtonsoft.Json;
using System.IO;
using System.Net;
using WebSocketting;

namespace Discord
{
    /// <summary>
    /// Maps to a <see cref="WebProxy"/>
    /// </summary>
    public class ProxyConfiguration
    {
        /// <summary>
        /// Maps to a <see cref="NetworkCredential"/>
        /// </summary>
        public class ProxyCredentials
        {
            /// <summary>
            /// See <see cref="NetworkCredential.UserName"/>
            /// </summary>
            public string Username { get; set; }
            /// <summary>
            /// See <see cref="NetworkCredential.Password"/>
            /// </summary>
            public string Password { get; set; }
            /// <summary>
            /// See <see cref="NetworkCredential.Domain"/>
            /// </summary>
            public string Domain { get; set; }
            /// <summary>
            /// See <see cref="WebProxy.UseDefaultCredentials"/>
            /// </summary>
            public bool UseDefault { get; set; }
        }
        /// <summary>
        /// Whether to use the proxy or not
        /// </summary>
        public bool UseProxy { get; set; }
        /// <summary>
        /// See <see cref="WebProxy.Address"/>
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// See <see cref="WebProxy.BypassList"/>
        /// </summary>
        public string[] BypassAddresses { get; set; }
        /// <summary>
        /// See <see cref="WebProxy.BypassProxyOnLocal"/>
        /// </summary>
        public bool BypassLocalAddresses { get; set; }
        /// <summary>
        /// See <see cref="WebProxy.Credentials"/>
        /// </summary>
        public ProxyCredentials Credentials { get; set; }
    }

    /// <summary>
    /// Configuration items for connecting to Discord's APIs
    /// </summary>
    public class Configuration
    {
        [JsonIgnore]
        private string _path;

        /// <summary>
        /// Proxy configuration used to configure the websocket and REST proxies
        /// </summary>
        public ProxyConfiguration ProxyConfiguration { get; set; } = new ProxyConfiguration();
        /// <summary>
        /// Authentication token used to authenticate against Discord services
        /// </summary>
        public string AuthToken { get; set; }
        /// <summary>
        /// Last known session ID, used for reconnecting
        /// </summary>
        public string LastSession { get; set; }
        /// <summary>
        /// Last know sequence received, used for reconnecting
        /// </summary>
        public int? LastSequence { get; set; }
        /// <summary>
        /// User agent passed to the Gateway API
        /// </summary>
        public string UserAgentUrl { get; set; } = "www";
        /// <summary>
        /// Application version passed to the Gateway API
        /// </summary>
        public string Version { get; set; } = "0.1a";
        /// <summary>
        /// Encoding method to use for Websocket communications
        /// </summary>
        public WebSocketMessageEncoding Encoding { get; set; } = WebSocketMessageEncoding.Json;


        [JsonConstructor]
        private Configuration() { }

        private Configuration(string path)
        {
            _path = path;
        }

        private Configuration SetPath(string path)
        {
            _path = path;
            return this;
        }

        /// <summary>
        /// Writes the configuration to disk
        /// </summary>
        public Configuration Save()
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.Indented));
            return this;
        }

        /// <summary>
        /// Reads a configuration file from the given path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Configuration Load(string path)
        {
            return File.Exists(path)
                ? JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(path)).SetPath(path)
                : new Configuration(path).Save();
        }
    }
}
