using System;
//Intellisense comments use these
using Discord.Descriptors;
using Discord.Descriptors.Guilds;
//-------------------------------

namespace Discord.Http
{
    /// <summary>
    /// Provides methods for constructing links to various CDN endpoints.
    /// </summary>
    public static class Cdn
    {
        public enum ImageSize
        {
            size16 = 16,
            size32 = 32,
            size64 = 64,
            size128 = 128,
            size256 = 256,
            size512 = 512,
            size1024 = 1024,
            size2048 = 2048
        }

        public const string CdnBaseUrl = "https://cdn.discordapp.com/";
        public const string EmojiEndpoint = "emojis/";
        public const string GuildIconEndpoint = "icons/";
        public const string GuildSplashEndpoint = "splashes/";
        public const string DefaultUserAvatarEndpoint = "embed/avatars/";
        public const string UserAvatarEndpoint = "avatars/";
        public const string ApplicationIconEndpoint = "app-icons/";

        /// <summary>
        /// Appends the 'desired size' parameter to the given endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string SetDesiredSize(string endpoint, ImageSize size)
        {
            return $"{endpoint}?size={(int)size}";
        }

        /// <summary>
        /// Returns the endpoint for a specific emoji. Format may be gif or PNG.
        /// https://cdn.discordapp/emojis/emoji_id.format
        /// </summary>
        /// <param name="emojiId">Unique ID of the emoji. Obtain from <see cref="EmojiDescriptor.Id"/></param>
        /// <param name="format">Format of the emoji. May be gif or PNG</param>
        /// <returns></returns>
        public static string GetEmojiEndpoint(ulong emojiId, CdnFormat format)
        {
            if (format != CdnFormat.gif && format != CdnFormat.png)
            {
                throw new ArgumentException("Format must be gif or PNG", nameof(format));
            }

            return $"{CdnBaseUrl}{EmojiEndpoint}{emojiId}.{format}";
        }


        /// <summary>
        /// Returns the endpoint for a specific guild's icon. Format may be PNG, JPEG, or WebP.
        /// https://cdn.discordapp/icons/guild_id/guild_icon.format
        /// </summary>
        /// <param name="guildId">Unique ID of the guild. Obtain from <see cref="GuildDescriptor.Id"/></param>
        /// <param name="iconHash">Guild's hashed icon. Obtain from <see cref="GuildDescriptor.IconHash"/></param>
        /// <param name="format">CdnFormat PNG, JPEG, or WebP</param>
        /// <returns></returns>
        public static string GetGuildIconEndpoint(ulong guildId, string iconHash, CdnFormat format)
        {
            if (format == CdnFormat.gif)
            {
                throw new ArgumentException("Format must be PNG, JPEG, or WebP", nameof(format));
            }

            return $"{CdnBaseUrl}{GuildIconEndpoint}{guildId}/{iconHash}.{format}";
        }

        /// <summary>
        /// Returns the endpoint for a specific guild's splash screen. Format may be PNG, JPEG, or WebP.
        /// https://cdn.discordapp/splashes/guild_id/guild_splash.format
        /// </summary>
        /// <param name="guildId">Unique ID of the guild. Obtain from <see cref="GuildDescriptor.Id"/></param>
        /// <param name="splashHash">Guild's hashed splash. Obtain from <see cref="GuildDescriptor.SplashImageHash"/></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetGuildSplashEndpoint(ulong guildId, string splashHash, CdnFormat format)
        {
            if (format == CdnFormat.gif)
            {
                throw new ArgumentException("Format must be PNG, JPEG, or WebP", nameof(format));
            }

            return $"{CdnBaseUrl}{GuildSplashEndpoint}{guildId}/{splashHash}.{format}";
        }

        /// <summary>
        ///  Returns the endpoint for a default avatar for the given discriminator. Format is always PNG.
        /// https://cdn.discordapp/embed/avatars/avatar_hash.png
        /// </summary>
        /// <param name="discriminator">4 digit discriminator number. Obtain from <see cref="UserDescriptor.Discriminator"/></param>
        /// <param name="avatarHash">User's hashed avatar. Obtain from <see cref="UserDescriptor.AvatarHash"/></param>
        /// <returns></returns>
        public static string GetDefaultAvatarEndpoint(int discriminator, string avatarHash)
        {
            return $"{CdnBaseUrl}{DefaultUserAvatarEndpoint}{discriminator % 5}/{avatarHash}.png";
        }

        /// <summary>
        /// Returns the endpoint for a user's avatar. Format may be PNG, JPEG, GIF, or WebP.
        /// https://cdn.discordapp/avatars/user_id/avatar_hash.format
        /// </summary>
        /// <param name="userId">Unique ID of the user. Obtain from <see cref="UserDescriptor.Id"/></param>
        /// <param name="avatarHash">User's hashed avatar. Obtain from <see cref="UserDescriptor.AvatarHash"/></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetAvatarEndpoint(ulong userId, string avatarHash, CdnFormat format)
        {
            return $"{CdnBaseUrl}{UserAvatarEndpoint}{userId}/{avatarHash}.{format}";
        }

        /// <summary>
        /// Returns the endpoint for an application's icon. Format may be PNG, JPEG, or WebP.
        /// https://cdn.discordapp/app-icons/application_id/application_hash.format
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appHash"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetApplicationIconEndpoint(ulong appId, string appHash, CdnFormat format)
        {
            if (format == CdnFormat.gif)
            {
                throw new ArgumentException("Format must be PNG, JPEG, or WebP", nameof(format));
            }

            return $"{CdnBaseUrl}{ApplicationIconEndpoint}{appId}/{appHash}.{format}";
        }
    }
}
