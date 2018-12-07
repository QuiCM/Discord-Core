using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.StatusCodes
{
    public enum DisconnectStatus
    {
        /// <summary>
        /// Disconnect was requested, but the Gateway does not appear to be connected
        /// </summary>
        NotConnected,
        /// <summary>
        /// Disconnect was requested, but a previous disconnect request is still pending
        /// </summary>
        AlreadyPending,
        /// <summary>
        /// A disconnect has been requested
        /// </summary>
        Pending
    }
}
