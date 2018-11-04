using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Utility
{
    /// <summary>
    /// Thrown when session limit count is reached upon attempting to connect to the Gateway
    /// </summary>
    public class SessionLimitException : Exception
    {
        /// <summary>
        /// Number of milliseconds until session limit resets
        /// </summary>
        public long ResetAfter { get; }

        public SessionLimitException(string message, long resetAfter) : base(message)
        {
            ResetAfter = resetAfter;
        }
    }
}
