using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Timeouts
{
    public interface ITimeoutManager
    {
        void Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body);

        DueMessagesResult GetDueMessage();
    }

    public class DueMessage
    {
        public Dictionary<string,string> Headers { get; set; }
        public byte[] Body { get; set; }
    }

    public abstract class DueMessagesResult : IEnumerable<DueMessage>, IDisposable
    {
        readonly List<DueMessage> _dueMessages;

        protected DueMessagesResult(IEnumerable<DueMessage> dueMessages)
        {
            _dueMessages = dueMessages.ToList();
        }

        public void Complete()
        {
            Commit();
        }

        public void Dispose()
        {
            Cleanup();
        }

        protected abstract void Commit();

        protected abstract void Cleanup();

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