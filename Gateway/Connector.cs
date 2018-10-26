using Discord.Descriptors;
using Discord.Descriptors.Guilds;
using Discord.Http.Gateway;
using Discord.Json.Objects;
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

        /// <summary>
        /// TokenSource created by linking the Gateway's CancellationToken and an end-user provided token
        /// </summary>
        internal CancellationTokenSource linkedTokenSource;

        public List<GuildDescriptor> Guilds { get; protected set; }
        public List<UserDescriptor> Users { get; protected set; }

        /// <summary>
        /// Token provided by an end-user for them to manage cancellation with
        /// </summary>
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
        public virtual async Task<Task> ConnectAsync(CancellationToken externalToken)
        {
            if (externalToken == null) 
            {
                throw new ArgumentNullException("externalToken");
            }

            _externalToken = externalToken;
            //Create a TokenSource from the provided client CancellationToken and our own internal token source's token
            //If the client ever cancels their token then this TokenSource will propagate cancellation
            //Likewise, if we ever cancel our token then this TokenSource will propagate cancellation
            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken, Gateway.GatewayTokenSource.Token);

            //Retrieve information required to connect to the Gateway.
            //This will throw an exception if it fails.
            //Should we handle the exception and cancel, or let it bubble?
            GetGatewayBotResponseObject connectionProperties = await AuthenticateAsync(linkedTokenSource.Token);

            //Use the retrieved information to create a new WebSocket instance, then connect to it
            Socket = new WebSocket(connectionProperties.url, GatewayRoutes.Encoding);
            //We send the linked TokenSource token through so that cancellation will propagate to/from the WebSocket.
            //The returned task can be awaited to block
            Task blockable = await Socket.ConnectAsync(linkedTokenSource.Token);
            //When blockable completes we can assume that we have disconnected from the gateway 
            //and so should cancel the Gateway's token
            blockable = blockable.ContinueWith(task => Gateway.GatewayTokenSource.Cancel()); 

            //heartbeat logic

            //Return the blockable task so that clients can block
            return blockable;
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
                //Cancelling our token should cause the WebSocket and everything else to disconnect
                Gateway.GatewayTokenSource.Cancel();
            }
            else 
            {
                throw new InvalidOperationException("Disconnection already pending.");
            }
        }

        /// <summary>
        /// Sends the payloads required to authenticate with Discord's Gateway API
        /// </summary>
        /// <param name="token">CancellationToken used to handle cancellation of the connection</param>
        /// <returns>awaitable boolean. True if auth succeeded, else false</returns>
        public virtual async Task<GetGatewayBotResponseObject> AuthenticateAsync(CancellationToken token)
        {
            //Doesn't this require authentication?

            token.ThrowIfCancellationRequested();

            GetGatewayBotResponseObject response =
                (GetGatewayBotResponseObject)await GatewayRoutes.GetGatewayAsync(Gateway.Rest, token);

            return response;
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
