namespace Discord.Credentials
{
    /// <summary>
    /// Used to authenticate a connection to a Discord API
    /// </summary>
    public class Credentials
    {
        public string AuthToken => Token;
        public bool IsBotToken => Token.StartsWith("Bot ");
        public bool IsBearerToken => Token.StartsWith("Bearer ");

        protected string Token { get; set; }

        public Credentials(string token)
        {
            Token = token;
        }
    }
}
