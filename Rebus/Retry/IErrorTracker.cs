using System;
using System.Collections.Generic;

namespace Rebus.Retry
{
    /// <summary>
    /// Service that is responsible for tracking errors across message deliveries.
    /// </summary>
    public interface IErrorTracker
    {
        /// <summary>
        /// This method is called on each experienced failed delivery. The <paramref name="final"/> flag
        /// can be set to true if this error is to be considered the final delivery attempt, meaning
        /// that the error tracker should immediately max out its internal counter (or whatever it is
        /// doing), resulting in <see cref="HasFailedTooManyTimes"/> yielding true from now on
        /// </summary>
        void RegisterError(string messageId, Exception exception, bool final = false);
        
        /// <summary>
        /// This method is called when there's no need to track the error anymore
        /// </summary>
        void CleanUp(string messageId);

        /// <summary>
        /// Gets whether the given message ID has had too many error registered for it
        /// </summary>
        bool HasFailedTooManyTimes(string messageId);
        
        /// <summary>
        /// Should get a short error description for the message ID (i.e. something like "5 failed deliveries")
        /// </summary>
        string GetShortErrorDescription(string messageId);
        
        /// <summary>
        /// Should get a full, detailed error description for the message ID (i.e. could be timestamps and full stack traces for all failed deliveries)
        /// </summary>
        string GetFullErrorDescription(string messageId);

        /// <summary>
        /// Gets all caught exceptions for the message ID
        /// </summary>
        IEnumerable<Exception> GetExceptions(string messageId);
    }
}