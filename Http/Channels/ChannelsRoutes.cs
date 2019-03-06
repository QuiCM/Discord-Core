using Discord.Descriptors.Channels;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Http.Channels
{
    public class ChannelRoutes
    {
        private Rest _rest;
        const string BaseUri = "channels";
        const string MessageUri = "messages";

        public ChannelRoutes(Rest rest)
        {
            _rest = rest;
        }

        public async Task<HttpStatusCode> PostMessageAsync(ChannelDescriptor channel, MessageDescriptor message, CancellationToken ct)
        {
            if (channel is ChannelCategoryDescriptor || channel is VoiceChannelDescriptor)
            {
                throw new ArgumentException("Invalid channel type", nameof(channel));
            }

            if (message == null || (message.Content == null && message.embeds == null))
            {
                throw new ArgumentException("Message may not be null", nameof(message));
            }

            string msgJson = JsonConvert.SerializeObject(
                new { content = message.Content, embed = message.embeds?[0] ?? null },
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
            );

            HttpContent content = new StringContent(msgJson, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _rest.PostAsync($"{BaseUri}/{channel.Id}/{MessageUri}", content, ct);


            return response.StatusCode;
        }

        public async Task<HttpStatusCode> UploadFileAsync(ChannelDescriptor channel, MessageDescriptor message, string filePath, CancellationToken ct)
        {
            if (channel is ChannelCategoryDescriptor || channel is VoiceChannelDescriptor)
            {
                throw new ArgumentException("Invalid channel type", nameof(channel));
            }

            MultipartFormDataContent content = new MultipartFormDataContent("--dcore--");

            if (message?.Content != null || message?.embeds != null)
            {
                object msg = new { content = message.Content ?? "", embed = message.embeds[0] ?? null };
                content.Add(
                    new StringContent(
                        JsonConvert.SerializeObject(msg, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                    ),
                    "payload_json"
               );
            }

            content.Add(new StreamContent(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)), "file", Path.GetFileName(filePath));

            HttpResponseMessage response = await _rest.PostAsync($"{BaseUri}/{channel.Id}/{MessageUri}", content, ct);

            return response.StatusCode;
        }
    }
}
