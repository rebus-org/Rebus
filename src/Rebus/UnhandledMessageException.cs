using System;

namespace Rebus
{
    [Serializable]
    public class UnhandledMessageException : ApplicationException
    {
        readonly object unhandledMessage;

        public UnhandledMessageException(object unhandledMessage)
            : base(string.Format("Could not find any handlers to execute message of type {0}", unhandledMessage.GetType()))
        {
            this.unhandledMessage = unhandledMessage;
        }

        public object UnhandledMessage
        {
            get { return unhandledMessage; }
        }
    }
}