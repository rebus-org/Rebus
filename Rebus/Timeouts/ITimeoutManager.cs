using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Timeouts
{
    public interface ITimeoutManager
    {
        Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body);

        Task<DueMessagesResult> GetDueMessages();
    }

    public class DueMessage
    {
        readonly Action _completeAction;

        public DueMessage(Dictionary<string, string> headers, byte[] body, Action completeAction = null)
        {
            _completeAction = completeAction;
            Headers = headers;
            Body = body;
        }

        public Dictionary<string, string> Headers { get; private set; }

        public byte[] Body { get; private set; }

        public void MarkAsCompleted()
        {
            if (_completeAction == null) return;

            _completeAction();
        }

        public TransportMessage ToTransportMessage()
        {
            return new TransportMessage(Headers, Body);
        }
    }

    public class DueMessagesResult : IEnumerable<DueMessage>, IDisposable
    {
        readonly Action _cleanupAction;
        readonly List<DueMessage> _dueMessages;

        public DueMessagesResult(IEnumerable<DueMessage> dueMessages, Action cleanupAction = null)
        {
            _cleanupAction = cleanupAction;
            _dueMessages = dueMessages.ToList();
        }

        public void Dispose()
        {
            if (_cleanupAction == null) return;

            _cleanupAction();
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