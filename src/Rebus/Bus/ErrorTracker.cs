using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rebus.Bus
{
    /// <summary>
    /// Class used by <see cref="RebusBus"/> to track errors between retries.
    /// </summary>
    public class ErrorTracker
    {
        readonly ConcurrentDictionary<string, TrackedMessage> trackedMessages = new ConcurrentDictionary<string, TrackedMessage>();

        public void Track(string id, Exception exception)
        {
            var trackedMessage = GetOrAdd(id);
            trackedMessage.AddError(exception);
        }

        public void Forget(string id)
        {
            TrackedMessage temp;
            trackedMessages.TryRemove(id, out temp);
        }

        public string GetErrorText(string id)
        {
            var trackedMessage = GetOrAdd(id);
            return trackedMessage.GetErrorMessages();
        }

        public bool MessageHasFailedMaximumNumberOfTimes(string id)
        {
            var trackedMessage = GetOrAdd(id);
            return trackedMessage.Errors >= 5;
        }

        TrackedMessage GetOrAdd(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(string.Format("Id of message to track is null! Cannot track message errors with a null id"));
            }
            return trackedMessages.GetOrAdd(id, i => new TrackedMessage());
        }

        class TrackedMessage
        {
            readonly List<Exception> exceptions = new List<Exception>();

            public int Errors
            {
                get { return exceptions.Count; }
            }

            public void AddError(Exception exception)
            {
                exceptions.Add(exception);
            }

            public string GetErrorMessages()
            {
                return string.Join(Environment.NewLine, exceptions.Select(e => e.ToString()));
            }
        }
    }
}