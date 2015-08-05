using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Timeouts
{
    /// <summary>
    /// Represents the result of calling <see cref="ITimeoutManager.GetDueMessages"/> - contains zero or mode <see cref="DueMessage"/> objects
    /// where each due message can be individually marked as successfully delivered 
    /// </summary>
    public class DueMessagesResult : IEnumerable<DueMessage>, IDisposable
    {
        readonly Action _cleanupAction;
        readonly List<DueMessage> _dueMessages;

        /// <summary>
        /// Constructs the result, wrapping the given list of due messages, performing the given action when the instance is disposed
        /// </summary>
        public DueMessagesResult(IEnumerable<DueMessage> dueMessages, Action cleanupAction = null)
        {
            _cleanupAction = cleanupAction;
            _dueMessages = dueMessages.ToList();
        }

        /// <summary>
        /// Invokes the cleanup action
        /// </summary>
        public void Dispose()
        {
            if (_cleanupAction == null) return;

            _cleanupAction();
        }


        /// <summary>
        /// Returns all due messages from this result
        /// </summary>
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