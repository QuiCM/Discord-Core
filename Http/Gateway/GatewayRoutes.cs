using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketting;

namespace Discord.Http.Gateway
{
    public static class GatewayRoutes
    {
        public const string GatewayEndpoint = "gateway";
        public const string BotGatewayEndpoint = "gateway/bot";
        /// <summary>
        /// Version of Discord's Gateway API to retrieve a connection to
        /// </summary>
        /// <value></value>
        public static int ApiVersion { get; set; } = 6;
        /// <summary>
        /// Encoding type to be used when connecting
        /// </summary>
        /// <value></value>
        public static WebSocketMessageEncoding Encoding { get; set; } = WebSocketMessageEncoding.Json;

        /// <summary>
        /// Returns an object containing information needed to connect to the Discord Gateway API
        /// </summary>
        /// <param name="rest"><see cref="Rest"/> controller used to query Discord's REST API</param>
        /// <param name="ct">CancellationToken used to cancel the request</param>
        /// <exception cref="HttpRequestException">If the request is unsuccessful</exception>
        /// <returns>Returns a <see cref="Json.Objects.GetGatewayResponseObject"/> containing Gateway connection information</returns>
        public static async Task<Json.Objects.GetGatewayResponseObject> GetGatewayAsync(Rest rest, CancellationToken ct)
        {
            string endpoint = $"{Rest.RestBaseUrl}{GatewayEndpoint}?v={ApiVersion}&encoding={Encoding.ToString().ToLowerInvariant()}";

            Json.Objects.GetGatewayResponseObject response = await rest.GetAsync<Json.Objects.GetGatewayResponseObject>(
                endpoint,
                null,
                ct
            );

            return response;
        }

        /// <summary>
        /// Returns an object containing information needed to connect to the Discord Gateway API via a Bot token
        /// </summary>
        /// <param name="rest"><see cref="Rest"/> controller used to query Discord's REST API</param>
        /// <param name="ct">CancellationToken used to cancel the request</param>
        /// <exception cref="HttpRequestException">If the request is unsuccessful</exception>
        /// <returns>Returns a <see cref="Json.Objects.GetGatewayBotResponseObject"/> containing Gateway connection information</returns>
        public static async Task<Json.Objects.GetGatewayBotResponseObject> GetBotGatewayAsync(Rest rest, CancellationToken ct)
        {
            string endpoint = $"{Rest.RestBaseUrl}{BotGatewayEndpoint}?v={ApiVersion}&encoding={Encoding.ToString().ToLowerInvariant()}";

            Json.Objects.GetGatewayBotResponseObject response = await rest.GetAsync<Json.Objects.GetGatewayBotResponseObject>(
                endpoint,
                null,
                ct
            );

            return response;
        }
    }
}