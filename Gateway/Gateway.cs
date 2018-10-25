using Discord.Descriptors;
using Discord.Descriptors.Guilds;
using Discord.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Gateway
{
    /// <summary>
    /// Provides a connection to the Discord Gateway API
    /// </summary>
    public class Gateway : IDisposable
    {
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
        public IEnumerable<GuildDescriptor> Guilds => Connector.Guilds;
        public IEnumerable<UserDescriptor> Users => Connector.Users;

        /// <summary>
        /// Constructs a new Gateway instance using the provided <seealso cref="Credentials.Credentials"/>
        /// </summary>
        /// <param name="credentials"></param>
        public Gateway(Credentials.Credentials credentials)
        {
            Credentials = credentials;
            GatewayTokenSource = new CancellationTokenSource();

            Connector = new Connector(this, Credentials);
        }

        public virtual async Task ConnectAsync(CancellationToken token)
        {
            //if the user hasn't provided an un-cancelled token, abuse them
            token.ThrowIfCancellationRequested();

            //To allow reconnecting to the same gateway instance, we recreate our internal token source if it has previously been cancelled
            if (GatewayTokenSource.IsCancellationRequested)
            {
                GatewayTokenSource = new CancellationTokenSource();
            }

            await Connector.HeartbeatAsync(token);
        }

        public virtual void Disconnect()
        {
            Connector.Disconnect();
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
