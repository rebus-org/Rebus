using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using Rebus.Messages;
using log4net;
using System.Linq;

namespace Rebus.Timeout
{
    public class TimeoutService : IHandleMessages<RequestTimeoutMessage>
    {
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        readonly IBus bus;
        readonly object listLock = new object();
        readonly List<Timeout> timeouts = new List<Timeout>();
        readonly Timer timer = new Timer();

        public TimeoutService(IBus bus)
        {
            this.bus = bus;

            timer.Interval = 300;
            timer.Elapsed += CheckCallbacks;
        }

        void CheckCallbacks(object sender, ElapsedEventArgs e)
        {
            lock(listLock)
            {
                var dueTimeouts = timeouts.ToList().Where(t => t.TimeToReturn >= DateTime.UtcNow);

                foreach (var timeout in dueTimeouts)
                {
                    bus.Send(new TimeoutExpiredMessage
                                 {
                                     CorrelationId = timeout.CorrelationId,
                                     DueTime = timeout.TimeToReturn
                                 });

                    timeouts.Remove(timeout);
                }
            }
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        public void Handle(RequestTimeoutMessage message)
        {
            var currentMessageContext = MessageContext.GetCurrent();

            lock (listLock)
            {
                timeouts.Add(new Timeout
                                 {
                                     CorrelationId = message.CorrelationId,
                                     ReplyTo = currentMessageContext.ReturnAddress,
                                     TimeToReturn = DateTime.UtcNow + message.Timeout,
                                 });
            }
        }
    }

    public class Timeout
    {
        public string ReplyTo { get; set; }
        public string CorrelationId { get; set; }
        public DateTime TimeToReturn { get; set; }
    }
}