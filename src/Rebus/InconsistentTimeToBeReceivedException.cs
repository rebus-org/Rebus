using System;
using Rebus.Shared;

namespace Rebus
{
    [Serializable]
    public class InconsistentTimeToBeReceivedException : ApplicationException
    {
        public InconsistentTimeToBeReceivedException(string message, params object[] objs)
            : base(string.Format(@"When specifying the {0} header, it must be consistent across messages within one batch!

Otherwise, messages might either get deleted before they actually expire, or not expire in time.

{1}", Headers.TimeToBeReceived, string.Format(message, objs)))
        {
            
        }
    }
}