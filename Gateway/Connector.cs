using Discord.Descriptors;
using Discord.Descriptors.Guilds;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebSocketting;

namespace Discord.Gateway
{
    /// <summary>
    /// Responsible for connecting to and maintaining the connection to the Discord Gateway API
    /// </summary>
    public class Connector
    {
        /// <summary>
        /// Gateway instance hosting this connector
        /// </summary>
        protected Gateway Gateway { get; }
        /// <summary>
        /// Credentials used to authenticate with the Gateway
        /// </summary>
        protected Credentials.Credentials Credentials { get; }
        /// <summary>
        /// WebSocket used to connect to the Gateway
        /// </summary>
        protected WebSocket Socket { get; }

        public List<GuildDescriptor> Guilds { get; protected set; }
        public List<UserDescriptor> Users { get; protected set; }

        private CancellationToken _externalToken;

        /// <summary>
        /// Constructs a new gateway connector for the given gateway.
        /// The connector connects with the provided <see cref="Credentials.Credentials"/>
        /// </summary>
        /// <param name="gateway">Gateway instance hosting this connector</param>
        /// <param name="credentials">Credentials with which to connect to the Discord Gateway API</param>
        public Connector(Gateway gateway, Credentials.Credentials credentials)
        {
            Gateway = gateway;
            Credentials = credentials;
        }

        /// <summary>
        /// Connects to the Gateway.
        /// Disconnect with <see cref="Disconnect"/> or by cancelling the provided <see cref="CancellationToken"/>
        /// </summary>
        /// <param name="externalToken">CancellationToken used for managing cancellation of the connection</param>
        /// <returns></returns>
        public virtual async Task ConnectAsync(CancellationToken externalToken)
        {
            _externalToken = externalToken;

            try
            {
                using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    externalToken,
                    Gateway.GatewayTokenSource.Token))
                {
                    if (!await AuthenticateAsync(linkedCts.Token))
                    {
                        Gateway.GatewayTokenSource.Cancel();
                        linkedCts.Token.ThrowIfCancellationRequested();
                    }

                    Task.WaitAny(new[] {
                            HeartbeatAsync(linkedCts.Token),
                            //Connect, ready, message pump, etc
                        },
                        linkedCts.Token
                    );
                }
            }
            catch (OperationCanceledException)
            {
                if (externalToken.IsCancellationRequested)
                {
                    externalToken.ThrowIfCancellationRequested();
                }
                else
                {
                    //internal cancellation
                }
            }
        }

        /// <summary>
        /// Disconnects the gateway
        /// </summary>
        public virtual void Disconnect()
        {
            if (_externalToken == null)
            {
                throw new InvalidOperationException("Gateway must be connected in order to disconnect.");
            }

            if (_externalToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("Disconnection already pending.");
            }

            if (!Gateway.GatewayTokenSource.IsCancellationRequested)
            {
                Gateway.GatewayTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Sends the payloads required to authenticate with Discord's Gateway API
        /// </summary>
        /// <param name="token">CancellationToken used to handle cancellation of the connection</param>
        /// <returns>awaitable boolean. True if auth succeeded, else false</returns>
        public virtual async Task<bool> AuthenticateAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            Json.Objects.GetGatewayBotResponseObject response =
                await Gateway.Rest.GetGatewayAsync(token);

            return true;
        }

        /// <summary>
        /// Sends a heartbeat payload every interval to maintain the Gateway connection
        /// </summary>
        /// <param name="token">CancellationToken used to manage cancellation of the connection</param>
        /// <returns></returns>
        public virtual async Task HeartbeatAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(1000, token);
        }
    }
}
