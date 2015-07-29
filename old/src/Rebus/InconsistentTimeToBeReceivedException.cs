using System;
using System.Runtime.Serialization;
using Rebus.Shared;

namespace Rebus
{
    /// <summary>
    /// Exception that is thrown when a time-to-be-received header has been added to two or more logical messages in
    /// a message and the configured time is not the same
    /// </summary>
    [Serializable]
    public class InconsistentTimeToBeReceivedException : ApplicationException
    {
        /// <summary>
        /// Mandatory exception ctor
        /// </summary>
        protected InconsistentTimeToBeReceivedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal InconsistentTimeToBeReceivedException(string message, params object[] objs)
            : base(string.Format(@"When specifying the {0} header, it must be consistent across messages within one batch!

Otherwise, messages might either get deleted before they actually expire, or not expire in time.

{1}", Headers.TimeToBeReceived, string.Format(message, objs)))
        {

        }
    }
}