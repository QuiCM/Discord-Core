namespace Discord
{
    /// <summary>
    /// 
    /// </summary>
    public static class Discord
    {
        /// <summary>
        /// Creates a <see cref="Gateway.Gateway"/> object using the provided <see cref="Credentials.Credentials"/> object.
        /// This gateway can be used to create a connection with <see cref="Gateway.Gateway.ConnectAsync"/>.
        /// </summary>
        /// <param name="credentials"><see cref="Credentials.Credentials"/> object containing authorization information for the gateway API</param>
        /// <returns></returns>
        public static Gateway.Gateway CreateGateway(Credentials.Credentials credentials, Configuration config)
        {
            return new Gateway.Gateway(credentials, config);
        }
    }
}
