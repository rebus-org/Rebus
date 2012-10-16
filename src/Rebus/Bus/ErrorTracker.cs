using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Rebus.Configuration;
using Rebus.Extensions;
using Rebus.Logging;

namespace Rebus.Bus
{
    public class ErrorTracker : IErrorTracker, IDisposable
    {
        static ILog log;

        static ErrorTracker()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<string, TrackedMessage> trackedMessages = new ConcurrentDictionary<string, TrackedMessage>();
        readonly string errorQueueAddress;

        TimeSpan timeoutSpan;
        Timer timer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeoutSpan">How long messages will be supervised by the ErrorTracker</param>
        /// <param name="timeoutCheckInterval">This is the interval that will last between checking whether delivery attempts have been tracked for too long</param>
        /// <param name="errorQueueAddress">This is the address of the error queue to which messages should be forwarded whenever they are deemed poisonous</param>
        public ErrorTracker(TimeSpan timeoutSpan, TimeSpan timeoutCheckInterval, string errorQueueAddress)
        {
            this.errorQueueAddress = errorQueueAddress;
            StartTimeoutTracker(timeoutSpan, timeoutCheckInterval);

            MaxRetries = Math.Max(0, RebusConfigurationSection
                                         .GetConfigurationValueOrDefault(s => s.MaxRetries, 5)
                                         .GetValueOrDefault(5));
        }

        /// <summary>
        /// Default constructor which sets the timeoutSpan to 1 day
        /// </summary>
        public ErrorTracker(string errorQueueAddress)
            : this(TimeSpan.FromDays(1), TimeSpan.FromMinutes(5), errorQueueAddress)
        {
        }

        void StartTimeoutTracker(TimeSpan timeoutSpanToUse, TimeSpan timeoutCheckInterval)
        {
            timeoutSpan = timeoutSpanToUse;
            timer = new Timer(TimeoutTracker, null, TimeSpan.Zero, timeoutCheckInterval);
        }

        void TimeoutTracker(object state)
        {
            CheckForMessageTimeout();
        }

        internal void CheckForMessageTimeout()
        {
            var keysOfExpiredMessages = trackedMessages
                .Where(m => m.Value.Expired(timeoutSpan))
                .Select(m => m.Key)
                .ToList();
            
            keysOfExpiredMessages.ForEach(key =>
                {
                    TrackedMessage temp;
                    if (trackedMessages.TryRemove(key, out temp))
                    {
                        log.Warn(
                            "Timeout expired for delivery tracking of message with ID {0}. This probably means that the " +
                            "message was deleted from the queue before the max number of retries could be carried out, " +
                            "thus the delivery tracking for this message could not be fully completed. The error text for" +
                            "the message deliveries is as follows: {1}", temp.Id, temp.GetErrorMessages());
                    }
                });
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

        public string ErrorQueueAddress
        {
            get { return errorQueueAddress; }
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
        /// Retrieves information about caught exceptions for the message with the
        /// given id.
        /// </summary>
        /// <param name="id">ID of message whose poison message information to get</param>
        /// <returns>Information about the poison message</returns>
        public PoisonMessageInfo GetPoisonMessageInfo(string id)
        {
            var trackedMessage = GetOrAdd(id);

            return trackedMessage.GetPoisonMessageInfo();
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
            return trackedMessage.FailCount >= MaxRetries;
        }

        public int MaxRetries { get; private set; }

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
            readonly List<Timed<Exception>> exceptions = new List<Timed<Exception>>();

            public TrackedMessage(string id)
            {
                Id = id;
                TimeAdded = RebusTimeMachine.Now();
            }

            public string Id { get; private set; }
            
            public DateTime TimeAdded { get; private set; }

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

            public PoisonMessageInfo GetPoisonMessageInfo()
            {
                return new PoisonMessageInfo(Id, exceptions.Select(e => new TimedException(e.Time, e.Value)));
            } 

            static string FormatTimedException(Timed<Exception> e)
            {
                return string.Format(@"{0}:
{1}", e.Time, e.Value);
            }

            public bool Expired(TimeSpan timeout)
            {
                return TimeAdded.ElapsedUntilNow() >= timeout;
            }
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }
}