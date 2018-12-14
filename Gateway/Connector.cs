using Discord.Descriptors;
using Discord.Descriptors.Commands;
using Discord.Descriptors.Payloads;
using Discord.Http.Gateway;
using Discord.Json.Objects;
using Discord.StatusCodes;
using System;
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
        /// Token provided by an end-user for them to manage cancellation with
        /// </summary>
        private CancellationToken _externalToken;

        /// <summary>
        /// Information returned from /api/gateway or /api/gateway/bot endpoints.
        /// Cast this object based on the value of <see cref="Credentials.Credentials.IsBotToken"/>
        /// </summary>
        private GetGatewayResponseObject _connectionInfo;

        private bool _heartbeatAck = false;
        private int? _sequence = null;
        private string _session = null;

        /// <summary>
        /// Gateway instance hosting this connector
        /// </summary>
        protected Gateway Gateway { get; }
        /// <summary>
        /// Credentials used to authenticate with the Gateway
        /// </summary>
        protected Credentials.Credentials Credentials { get; }
        /// <summary>
        /// Interval at which heartbeat commands are sent over the Gateway
        /// </summary>
        protected int HeartbeatInterval { get; set; }
        /// <summary>
        /// <see cref="GatewayEvents"/> instance managing gateway event handlers
        /// </summary>
        public GatewayEvents Events { get; set; } = new GatewayEvents();

        /// <summary>
        /// WebSocket used to connect to the Gateway
        /// </summary>
        internal WebSocket Socket { get; set; }
        /// <summary>
        /// TokenSource created by linking the Gateway's CancellationToken and an end-user provided token
        /// </summary>
        internal CancellationTokenSource linkedTokenSource;
        /// <summary>
        /// Event invoked when a message is received and <see cref="GatewayRoutes.Encoding"/> is set to <see cref="WebSocketMessageEncoding.Json"/>.
        /// This event exposes the raw message sent through the websocket
        /// </summary>
        public event EventHandler<string> OnRawTextMessage;
        /// <summary>
        /// Event invoked when a message is received and <see cref="GatewayRoutes.Encoding"/> is set to <see cref="WebSocketMessageEncoding.Binary"/>.
        /// This event exposes the raw message sent through the websocket
        /// </summary>
        public event EventHandler<byte[]> OnRawBinaryMessage;

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
        /// Disconnect with <see cref="DisconnectAsync"/> or by cancelling the provided <see cref="CancellationToken"/>.
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

            return await InternalConnectAsync();
        }

        /// <summary>
        /// Connects to the Gateway API using the provided <see cref="GetGatewayResponseObject"/>
        /// Disconnect with <see cref="DisconnectAsync"/> or by cancelling the provided <see cref="CancellationToken"/>.
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

            return await InternalConnectAsync();
        }

        protected virtual async Task<Task> InternalConnectAsync()
        {
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

            Events.RegisterInternalHandles(this);

            //GatewayEvents.OnGatewayHello += GatewayEvents_OnGatewayHello;
            //We send the linked TokenSource token through so that cancellation will propagate to/from the WebSocket.
            //The returned task can be awaited to block
            await Socket.ConnectAsync(linkedTokenSource.Token);
            Task blockable = await Socket.ReadWriteAsync(linkedTokenSource.Token);

            //Return the blockable task so that clients can block
            return blockable;
        }

        /// <summary>
        /// Reconnects to a disconnected gateway using the provided gateway session.
        /// Returns a Task that may be awaited to block
        /// </summary>
        /// <param name="seq"></param>
        /// <param name="session"></param>
        /// <param name="externalToken"></param>
        /// <returns></returns>
        public virtual async Task<Task> ReconnectAsync()
        {
            return await InternalConnectAsync();
        }

        /// <summary>
        /// Disconnects the gateway
        /// </summary>
        public virtual async Task<DisconnectStatus> DisconnectAsync()
        {
            if (_externalToken == null)
            {
                //No connection has been made to this gateway
                return DisconnectStatus.NotConnected;
            }

            if (linkedTokenSource.IsCancellationRequested)
            {
                //The user-provided token or our internal token have already been cancelled
                return DisconnectStatus.AlreadyPending;
            }

            //Cancelling our token should cause everything to stop
            await Socket.DisconnectAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "", linkedTokenSource.Token);
            linkedTokenSource.Cancel();

            return DisconnectStatus.Pending;
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
            Queue(new GatewayEvent<int?>(GatewayOpCode.Heartbeat).WithPayload(_sequence));

            token.ThrowIfCancellationRequested();
            await Task.Delay(HeartbeatInterval, token);

            if (!_heartbeatAck)
            {
                await Socket.DisconnectAsync(System.Net.WebSockets.WebSocketCloseStatus.ProtocolError, "No ACK received", token);
                linkedTokenSource.Cancel();
                //need to restart once everything dies - or leave this to end user?
                return;
            }

            //Fire and forget - we don't need to await this
#pragma warning disable CS4014
            HeartbeatAsync(token);
#pragma warning restore CS4014
        }

        /// <summary>
        /// Queues an event for sending over the Websocket
        /// </summary>
        /// <param name="message"></param>
        public virtual void Queue<TPayload>(GatewayEvent<TPayload> gatewayEvent,
            Newtonsoft.Json.JsonSerializerSettings settings = null)
        {
            string json = gatewayEvent.Serialize(settings);
            Socket.Send(gatewayEvent.Serialize(settings));
        }

        internal void GatewayEvents_OnGatewayHello(string rawJson, GatewayEvent<HelloPayload> ev)
        {
            HeartbeatInterval = ev.Payload.HeartbeatInterval;

            if (_session != null && _sequence != null)
            {
                Queue(new GatewayEvent<ResumePayload>(GatewayOpCode.Resume)
                    .WithPayload(new ResumePayload
                    {
                        Sequence = _sequence.Value,
                        Session = _session,
                        Token = Credentials.AuthToken
                    })
                );
            }
            else
            {
                Queue(new GatewayEvent<IdentifyPayload>(GatewayOpCode.Identify)
                    .WithPayload(new IdentifyPayload
                    {
                        Token = Credentials.AuthToken,
                        Compress = false,
                        LargeThreshold = 250,
                        Properties = new ConnectionProperties { browser = "DotNET Core2.1", device = "DAsyc", os = "Win10" }
                })
               );
            }

#pragma warning disable CS4014
            //There's no need to await this. Warning disabled for sanity's sake
            HeartbeatAsync(linkedTokenSource.Token);
#pragma warning restore CS4014
        }

        internal void GatewayEvents_AllEventsCallback(string rawJson, GatewayEvent<object> ev)
        {
            Console.WriteLine($"[RECV] [{ev.OpCode}] {(ev.OpCode == GatewayOpCode.Dispatch ? ((DispatchGatewayEvent<object>)ev).Type.ToString() : "")}");

            if (ev is DispatchGatewayEvent<object> e && e.Sequence.HasValue)
            {
                _sequence = e.Sequence;
            }
        }

        internal void GatewayEvents_OnReady(string json, DispatchGatewayEvent<GatewayReady> ready)
        {
            _session = ready.Payload.Session;
        }

        internal void GatewayEvents_OnHeartbeatReq(string json, GatewayEvent<object> ev)
        {
            Queue(new GatewayEvent<object>(GatewayOpCode.HeartbeatAck),
                new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
        }

        internal void GatewayEvents_OnHeartbeatAck(string json, GatewayEvent<object> ev)
        {
            _heartbeatAck = true;
        }

        private void Socket_OnTextMessage(object sender, StringMessageEventArgs e)
        {
            OnRawTextMessage?.Invoke(this, e.Data);
            Events.Invoke(e.Data);
        }

        private void Socket_OnBinaryMessage(object sender, BinaryMessageEventArgs e)
        {
            OnRawBinaryMessage?.Invoke(this, e.Data);
            //Invoke
        }
    }
}
