using Discord.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Gateway
{
    /// <summary>
    /// Provides a connection to the Discord Gateway API
    /// </summary>
    public class Gateway : IDisposable
    {
        protected System.Net.IWebProxy _proxy { get; set; }
        /// <summary>
        /// Credentials used to authenticate with Discord's APIs
        /// </summary>
        protected Credentials.Credentials Credentials { get; set; }
        /// <summary>
        /// CancellationTokenSource used forward cancellation to components
        /// </summary>
        public CancellationTokenSource GatewayTokenSource { get; private set; }
        /// <summary>
        /// Connector used to connect to the Discord Gateway API
        /// </summary>
        public Connector Connector { get; set; }
        /// <summary>
        /// REST API helper
        /// </summary>
        public Rest Rest { get; set; }
        /// <summary>
        /// Proxy used for connections
        /// </summary>
        public System.Net.IWebProxy Proxy => _proxy;
        /// <summary>
        /// <see cref="GatewayEvents"/> instance managing gateway event handlers
        /// </summary>
        public GatewayEvents Events => Connector.Events;

        /// <summary>
        /// Constructs a new Gateway instance using the provided <see cref="Credentials.Credentials"/>
        /// </summary>
        /// <param name="credentials"></param>
        public Gateway(Credentials.Credentials credentials, Configuration config)
        {
            Credentials = credentials;
            GatewayTokenSource = new CancellationTokenSource();

            Http.Gateway.GatewayRoutes.Encoding = config.Encoding;
            Rest = new Rest(Credentials, config.UserAgentUrl, config.Version);
            Connector = new Connector(this, Credentials);

            InitializeProxy(config);

            if (Http.Gateway.GatewayRoutes.Encoding == WebSocketting.WebSocketMessageEncoding.Json)
            {
                Connector.OnRawTextMessage += Connector_OnTextMessage;
            }
            else
            {
                Connector.OnRawBinaryMessage += Connector_OnBinaryMessage;
            }
        }

        /// <summary>
        /// Retrieves connection information for Discord's Gateway API.
        /// </summary>
        /// <param name="token">CancellationToken used to handle cancellation of the connection</param>
        /// <returns>a <see cref="GetGatewayResponseObject"/> containing information to connect to the Gateway</returns>
        public virtual async Task<Json.Objects.GetGatewayResponseObject> AuthenticateAsync(CancellationToken token)
        {
            return await Connector.AuthenticateAsync(token);
        }

        /// <summary>
        /// Asynchronously connects to the Gateway.
        /// Returns a <see cref="Task"/> which may be awaited to block the calling thread
        /// </summary>
        /// <param name="token"></param>
        /// <returns>a <see cref="Task"/> that may be awaited to block the calling thread</returns>
        public virtual async Task<Task> ConnectAsync(CancellationToken token)
        {
            //if the user has provided a cancelled token, abuse them
            token.ThrowIfCancellationRequested();

            //To allow reconnecting to the same gateway instance, we recreate our internal token source if it has previously been cancelled
            if (GatewayTokenSource.IsCancellationRequested)
            {
                GatewayTokenSource = new CancellationTokenSource();
            }
            Task blockable = await Connector.ConnectAsync(token);

            return blockable;
        }

        /// <summary>
        /// Asynchronously connects to the Gateway with the given connection info.
        /// Returns a <see cref="Task"/> which may be awaited to block the calling thread.
        /// Throws a <see cref="Utility.SessionLimitException"/> if the maximum number of sessions have been used up.
        /// </summary>
        /// <param name="token"></param>
        /// <returns>a <see cref="Task"/> that may be awaited to block the calling thread</returns>
        /// <throws><see cref="Utility.SessionLimitException"/> if maximum session count has been reached.</throws>
        public virtual async Task<Task> ConnectAsync(CancellationToken token, Json.Objects.GetGatewayResponseObject connectionInfo)
        {
            //if the user has provided a cancelled token, abuse them
            token.ThrowIfCancellationRequested();

            if (Credentials.IsBotToken)
            {
                Json.Objects.GetGatewayBotResponseObject botConnectionInfo = connectionInfo as Json.Objects.GetGatewayBotResponseObject;
                if (botConnectionInfo.session_start_limit.remaining < 1)
                {
                    throw new Utility.SessionLimitException("Session limit", botConnectionInfo.session_start_limit.reset_after);
                }
            }

            //To allow reconnecting to the same gateway instance, we recreate our internal token source if it has previously been cancelled
            if (GatewayTokenSource.IsCancellationRequested)
            {
                GatewayTokenSource = new CancellationTokenSource();
            }

            Task blockable = await Connector.ConnectAsync(token, connectionInfo);

            return blockable;
        }

        public virtual void Disconnect()
        {
            Connector.DisconnectAsync();
        }

        private void Connector_OnBinaryMessage(object sender, byte[] e)
        {
        }

        private void Connector_OnTextMessage(object sender, string e)
        {
        }

        private void InitializeProxy(Configuration config)
        {
            if (config.ProxyConfiguration.UseProxy)
            {
                if (config.ProxyConfiguration.BypassAddresses != null)
                {
                    _proxy = new System.Net.WebProxy(
                        config.ProxyConfiguration.Address,
                        config.ProxyConfiguration.BypassLocalAddresses,
                        config.ProxyConfiguration.BypassAddresses
                    )
                    {
                        UseDefaultCredentials = config.ProxyConfiguration.Credentials?.UseDefault ?? false
                    };
                }
                else
                {
                    _proxy = new System.Net.WebProxy(
                        config.ProxyConfiguration.Address
                    )
                    {
                        UseDefaultCredentials = config.ProxyConfiguration.Credentials?.UseDefault ?? false
                    };
                }

                if (config.ProxyConfiguration.Credentials != null)
                {
                    _proxy.Credentials = new System.Net.NetworkCredential
                    {
                        Domain = config.ProxyConfiguration.Credentials.Domain,
                        Password = config.ProxyConfiguration.Credentials.Password,
                        UserName = config.ProxyConfiguration.Credentials.Username
                    };
                }
                Rest.HttpHandler.Proxy = _proxy;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    GatewayTokenSource.Dispose();
                }

                Credentials = null;
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GatewayConnection() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        public void Dispose()
        {
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
