using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Rebus
{
    /// <summary>
    /// Exception that is thrown when an incoming message can be handled by multiple saga handlers.
    /// This is an error because it would require that multiple saga data instances could be updated
    /// atomically by all saga persisters.
    /// </summary>
    [Serializable]
    public class MultipleSagaHandlersFoundException : ApplicationException
    {
        readonly object messageThatCouldBeHandledByMultipleSagaHandlers;
        readonly Type[] sagaHandlerTypes;

        /// <summary>
        /// Mandatory exception ctor
        /// </summary>
        protected MultipleSagaHandlersFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Constructs the exception with a reference to the message that could be handled by multiple saga handlers
        /// </summary>
        public MultipleSagaHandlersFoundException(object messageThatCouldBeHandledByMultipleSagaHandlers, Type[] sagaHandlerTypes)
            : base(string.Format("The message type {0} could be handled by multiple saga handlers: {1}. This is an error because it would require that multiple saga instances could be updated atomically, which is not possible with all saga persisters",
            messageThatCouldBeHandledByMultipleSagaHandlers.GetType(), string.Join(", ", sagaHandlerTypes.Select(t => t.ToString()))))
        {
            this.messageThatCouldBeHandledByMultipleSagaHandlers = messageThatCouldBeHandledByMultipleSagaHandlers;
            this.sagaHandlerTypes = sagaHandlerTypes;
        }

        /// <summary>
        /// Gets the types of saga handlers that were found
        /// </summary>
        public Type[] SagaHandlerTypes
        {
            get { return sagaHandlerTypes; }
        }

        /// <summary>
        /// Gets the message that resulted in multiple saga handlers being resolved
        /// </summary>
        public object MessageThatCouldBeHandledByMultipleSagaHandlers
        {
            get { return messageThatCouldBeHandledByMultipleSagaHandlers; }
        }
    }
}