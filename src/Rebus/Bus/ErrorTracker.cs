using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Logging;

namespace Rebus.Bus
{
    /// <summary>
    /// Class used by <see cref="RebusBus"/> to track errors between retries.
    /// </summary>
    public class ErrorTracker
    {
        static ILog log;

        static ErrorTracker()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<string, TrackedMessage> trackedMessages = new ConcurrentDictionary<string, TrackedMessage>();

        public void TrackDeliveryFail(string id, Exception exception)
        {
            var trackedMessage = GetOrAdd(id);
            trackedMessage.AddError(exception);
        }

        public void StopTracking(string id)
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
            return trackedMessage.FailCount >= 5;
        }

        TrackedMessage GetOrAdd(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(string.Format("Id of message to track is null! Cannot track message errors with a null id"));
            }
            return trackedMessages.GetOrAdd(id, i => new TrackedMessage(id));
        }

        class TrackedMessage
        {
            readonly List<Exception> exceptions = new List<Exception>();

            public TrackedMessage(string id)
            {
                Id = id;
            }

            public string Id { get; private set; }

            public int FailCount
            {
                get { return exceptions.Count; }
            }

            public void AddError(Exception exception)
            {
                exceptions.Add(exception);

                log.Debug("Message {0} has failed {1} time(s)", Id, FailCount);
            }

            public string GetErrorMessages()
            {
                return string.Join(Environment.NewLine, exceptions.Select(e => e.ToString()));
            }
        }
    }
}