using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Timeouts
{
    public interface ITimeoutManager
    {
        Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, Stream body);

        Task<DueMessagesResult> GetDueMessages();
    }

    public class DueMessage
    {
        public DueMessage(Dictionary<string, string> headers, Stream body)
        {
            Headers = headers;
            Body = body;
        }

        public Dictionary<string,string> Headers { get; private set; }
        public Stream Body { get; private set; }

        public TransportMessage ToTransportMessage()
        {
            return new TransportMessage(Headers, Body);
        }
    }

    public class DueMessagesResult : IEnumerable<DueMessage>, IDisposable
    {
        readonly List<DueMessage> _dueMessages;

        public DueMessagesResult(IEnumerable<DueMessage> dueMessages)
        {
            _dueMessages = dueMessages.ToList();
        }

        public void Complete()
        {
        }

        public void Dispose()
        {
        }


        public IEnumerator<DueMessage> GetEnumerator()
        {
            return _dueMessages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}