using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rebus.Bus
{
    public class ErrorTracker
    {
        readonly ConcurrentDictionary<string, TrackedMessage> trackedMessages = new ConcurrentDictionary<string, TrackedMessage>();

        public bool MessageHasFailedMaximumNumberOfTimes(string id)
        {
            var trackedMessage = trackedMessages.GetOrAdd(id, i => new TrackedMessage());
            return trackedMessage.Errors >= 5;
        }

        public string GetErrorText(string id)
        {
            var trackedMessage = trackedMessages.GetOrAdd(id, i => new TrackedMessage());
            return trackedMessage.GetErrorMessages();
        }

        public void SignOff(string id)
        {
            TrackedMessage temp;
            trackedMessages.TryRemove(id, out temp);
        }

        public void TrackError(string id, Exception exception)
        {
            var trackedMessage = trackedMessages.GetOrAdd(id, i => new TrackedMessage());
            trackedMessage.AddError(exception);
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