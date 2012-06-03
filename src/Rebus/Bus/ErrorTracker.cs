using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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

        /// <summary>
        /// Default constructor which sets the timeoutSpan to 1 day
        /// </summary>
        public ErrorTracker()
        {
            StartTimeoutTracker(TimeSpan.FromDays(1), TimeSpan.FromMinutes(5));
        }

        private void StartTimeoutTracker(TimeSpan timeoutSpan, TimeSpan timeoutCheckInterval)
        {
            this.timeoutSpan = timeoutSpan;
            new Timer(TimeoutTracker, null, TimeSpan.Zero, timeoutCheckInterval);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeoutSpan">How long messages will be supervised by the ErrorTracker</param>
        public ErrorTracker(TimeSpan timeoutSpan, TimeSpan timeoutCheckInterval)
        {
            StartTimeoutTracker(timeoutSpan, timeoutCheckInterval);
        }

        readonly ConcurrentDictionary<string, TrackedMessage> trackedMessages = new ConcurrentDictionary<string, TrackedMessage>();
        readonly ConcurrentQueue<Timed<string>> timedoutMessages = new ConcurrentQueue<Timed<string>>();
        TimeSpan timeoutSpan;


        private void TimeoutTracker(object state)
        {
            CheckForMessageTimeout();
        }

        internal void CheckForMessageTimeout()
        {
            Timed<string> id;
            bool couldRetrieve = timedoutMessages.TryPeek(out id);

            while (couldRetrieve && id.Time <= Time.Now())
            {
                if (timedoutMessages.TryDequeue(out id))
                {
                    TrackedMessage trackedMessage;
                    if (trackedMessages.TryRemove(id.Value, out trackedMessage))
                        log.Error("Handling message {0} has failed due to timeout at {1}", id.Value, Time.Now());
                }

                couldRetrieve = timedoutMessages.TryPeek(out id);
            }
        }

        /// <summary>
        /// Increments the fail count for this particular message, and starts tracking
        /// the message if it is not already being tracked.
        /// </summary>
        /// <param name="id">ID of the message to track</param>
        /// <param name="exception">The exception that was caught, thus resulting in wanting to track this message</param>
        public void TrackDeliveryFail(string id, Exception exception)
        {
            var trackedMessage = GetOrAdd(id);
            trackedMessage.AddError(exception);
        }

        /// <summary>
        /// Stops tracking the message with the specified ID. If the message is not
        /// being tracked, nothing happens.
        /// </summary>
        /// <param name="id">ID of message to stop tracking</param>
        public void StopTracking(string id)
        {
            TrackedMessage temp;
            trackedMessages.TryRemove(id, out temp);
        }

        /// <summary>
        /// Gets the error messages tracked so far for the message with the specified ID.
        /// </summary>
        /// <param name="id">ID of message whose error messages to get</param>
        /// <returns>Concatenated string of the tracked error messages</returns>
        public string GetErrorText(string id)
        {
            var trackedMessage = GetOrAdd(id);
            return trackedMessage.GetErrorMessages();
        }

        /// <summary>
        /// Determines whether the message with the specified ID has failed
        /// "enough time"
        /// </summary>
        /// <param name="id">ID of message to check</param>
        /// <returns>Whether the message has failed too many times</returns>
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

            if (!trackedMessages.ContainsKey(id))
                timedoutMessages.Enqueue(id.At(Time.Now().Add(timeoutSpan)));

            return trackedMessages.GetOrAdd(id, i => new TrackedMessage(id));
        }

        class TrackedMessage
        {
            readonly List<Timed<Exception>> exceptions = new List<Timed<Exception>>();

            public TrackedMessage(string id)
            {
                Id = id;
            }

            string Id { get; set; }

            public int FailCount
            {
                get { return exceptions.Count; }
            }

            public void AddError(Exception exception)
            {
                exceptions.Add(exception.AtThisInstant());

                log.Debug("Message {0} has failed {1} time(s)", Id, FailCount);
            }

            public string GetErrorMessages()
            {
                return string.Join(Environment.NewLine + Environment.NewLine, exceptions.Select(FormatTimedException));
            }

            static string FormatTimedException(Timed<Exception> e)
            {
                return string.Format(@"{0}:
{1}", e.Time, e.Value);
            }
        }
    }
}