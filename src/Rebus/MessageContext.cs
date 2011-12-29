using System;
using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// Holds information about the message currently being handled on this particular thread.
    /// </summary>
    public class MessageContext : IDisposable, IMessageContext
    {
        public static event Action<IMessageContext> Established = delegate { };
        
        public event Action<IMessageContext> Disposed = delegate { };

        [ThreadStatic] static MessageContext current;

#if DEBUG
        public string StackTrace { get; set; }
#endif

        internal static MessageContext Enter(string returnAddress)
        {
            if (current != null)
            {
#if DEBUG
                throw new InvalidOperationException(
                    string.Format(
                        @"Cannot establish new message context when one is already present!

Stacktrace of when the current message context was created:
{0}",
                        current.StackTrace));
#else
                throw new InvalidOperationException(
                    string.Format("Cannot establish new message context when one is already present"));
#endif

            }
            current = new MessageContext
                          {
                              ReturnAddress = returnAddress
                          };

            Established(current);

            return current;
        }

        MessageContext()
        {
            DispatchMessageToHandlers = true;
            Items = new Dictionary<string, object>();

#if DEBUG
            StackTrace = Environment.StackTrace;
#endif
        }

        public IDictionary<string, object> Items { get; private set; }

        public string ReturnAddress { get; set; }

        public static IMessageContext GetCurrent()
        {
            if (current == null)
            {
                throw new InvalidOperationException("No message context available - the MessageContext instance will"
                                                    + " only be set during the handling of messages, and it"
                                                    + " is available only on the worker thread.");
            }

            return current;
        }

        public static bool HasCurrent
        {
            get { return current != null; }
        }

        internal bool DispatchMessageToHandlers { get; set; }

        public void Abort()
        {
            DispatchMessageToHandlers = false;
        }

        public void Dispose()
        {
            current = null;
            
            Disposed(this);
        }
    }
}