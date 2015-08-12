using System;
using System.Runtime.Serialization;
using Rebus.Shared;

namespace Rebus
{
    /// <summary>
    /// Exception that is thrown when a return address header has been added to two or more logical messages in
    /// a message and the return address is not the same
    /// </summary>
    [Serializable]
    public class InconsistentReturnAddressException : ApplicationException
    {
        /// <summary>
        /// Mandatory exception ctor
        /// </summary>
        protected InconsistentReturnAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal InconsistentReturnAddressException(string message, params object[] objs)
            : base(string.Format(@"When specifying the {0} header, it must be consistent across messages within one batch!

That means that if you specify the return address for one message in a batch, you should either

    a) refrain from specifying the return address on other messages, or
    b) specify the same return address (implies some kind of consistency)

{1}", Headers.ReturnAddress, string.Format(message, objs)))
        {

        }
    }
}