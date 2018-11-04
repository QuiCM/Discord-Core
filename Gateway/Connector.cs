using Discord.Descriptors;
using Discord.Descriptors.Guilds;
using Discord.Http.Gateway;
using Discord.Json.Objects;
using Discord.Utility;
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
        internal WebSocket Socket { get; set; }

        /// <summary>
        /// TokenSource created by linking the Gateway's CancellationToken and an end-user provided token
        /// </summary>
        internal CancellationTokenSource linkedTokenSource;

        public List<GuildDescriptor> Guilds { get; protected set; }
        public List<UserDescriptor> Users { get; protected set; }
        /// <summary>
        /// Event invoked when a message is received and <see cref="GatewayRoutes.Encoding"/> is set to <see cref="WebSocketMessageEncoding.Json"/>
        /// </summary>
        public event EventHandler<string> OnTextMessage;
        /// <summary>
        /// Event invoked when a message is received and <see cref="GatewayRoutes.Encoding"/> is set to <see cref="WebSocketMessageEncoding.Binary"/>
        /// </summary>
        public event EventHandler<byte[]> OnBinaryMessage;

        /// <summary>
        /// Token provided by an end-user for them to manage cancellation with
        /// </summary>
        private CancellationToken _externalToken;
        /// <summary>
        /// Information returned from /api/gateway or /api/gateway/bot endpoints.
        /// Cast this object based on the value of <see cref="Credentials.Credentials.IsBotToken"/>
        /// </summary>
        private GetGatewayResponseObject _connectionInfo;

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
        /// Tests authentication then connects to the Gateway API.
        /// Disconnect with <see cref="Disconnect"/> or by cancelling the provided <see cref="CancellationToken"/>.
        /// Returns a <see cref="Task"/> which may be awaited to block the calling thread
        /// </summary>
        /// <param name="externalToken">CancellationToken used for managing cancellation of the connection</param>
        /// <returns>a <see cref="Task"/> which may be awaited to block the calling thread</returns>
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
            _connectionInfo = await AuthenticateAsync(linkedTokenSource.Token);

            //Use the retrieved information to create a new WebSocket instance, then connect to it
            Socket = new WebSocket(_connectionInfo.url, GatewayRoutes.Encoding, Gateway.Proxy);
            if (GatewayRoutes.Encoding == WebSocketMessageEncoding.Binary)
            {
                Socket.OnBinaryMessage += Socket_OnBinaryMessage;
            }
            else
            {
                Socket.OnTextMessage += Socket_OnTextMessage;
            }
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
        /// Connects to the Gateway API using the provided <see cref="GetGatewayResponseObject"/>
        /// Disconnect with <see cref="Disconnect"/> or by cancelling the provided <see cref="CancellationToken"/>.
        /// Returns a <see cref="Task"/> which may be awaited to block the calling thread
        /// </summary>
        /// <param name="externalToken">CancellationToken used for managing cancellation of the connection</param>
        /// <returns>a <see cref="Task"/> which may be awaited to block the calling thread</returns>
        public virtual async Task<Task> ConnectAsync(CancellationToken externalToken, GetGatewayResponseObject connectionInfo)
        {
            if (externalToken == null)
            {
                throw new ArgumentNullException("externalToken");
            }

            _externalToken = externalToken;
            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken, Gateway.GatewayTokenSource.Token);
            
            _connectionInfo = connectionInfo;
            
            Socket = new WebSocket(_connectionInfo.url, GatewayRoutes.Encoding, Gateway.Proxy);
            Task blockable = await Socket.ConnectAsync(linkedTokenSource.Token);
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
        /// Retrieves connection information for Discord's Gateway API.
        /// </summary>
        /// <param name="token">CancellationToken used to handle cancellation of the connection</param>
        /// <returns>a <see cref="GetGatewayResponseObject"/> containing information to connect to the Gateway</returns>
        public virtual async Task<GetGatewayResponseObject> AuthenticateAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            GetGatewayResponseObject response = Credentials.IsBearerToken
                ? await GatewayRoutes.GetGatewayAsync(Gateway.Rest, token)
                : await GatewayRoutes.GetBotGatewayAsync(Gateway.Rest, token);

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



        private void Socket_OnTextMessage(object sender, StringMessageEventArgs e)
        {
            OnTextMessage?.Invoke(this, e.Data);
        }

        private void Socket_OnBinaryMessage(object sender, BinaryMessageEventArgs e)
        {
            OnBinaryMessage?.Invoke(this, e.Data);
        }
    }
}
