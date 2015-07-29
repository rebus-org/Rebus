using System;
using System.Collections.Generic;

namespace Rebus
{
    /// <summary>
    /// This is 
    /// </summary>
    public interface IMessageContext : IDisposable
    {
        /// <summary>
        /// Gets the return address of the message that is currently being handled.
        /// </summary>
        string ReturnAddress { get; }

        /// <summary>
        /// Gets the ID of the message that is currently being handled. This ID is normally provided by 
        /// Rebus and follows the message if it's forwarded
        /// </summary>
        string RebusTransportMessageId { get; }

        /// <summary>
        /// Gets the dictionary of objects associated with this message context. This collection can be used to store stuff
        /// for the duration of the handling of this transport message.
        /// </summary>
        IDictionary<string, object> Items { get; }

        /// <summary>
        /// Gets the handlers to skip.
        /// </summary>
        IReadOnlyCollection<Type> HandlersToSkip { get; }

        /// <summary>
        /// Aborts processing the current message - i.e., after exiting from the
        /// current handler, no more handlers will be called. Note that this does
        /// not cause the current transaction to be rolled back.
        /// </summary>
        void Abort();

        /// <summary>
        /// Raised when the message context is disposed.
        /// </summary>
        event Action Disposed;

        /// <summary>
        /// Returns the logical message currently being handled.
        /// </summary>
        object CurrentMessage { get; }

        /// <summary>
        /// Contains the headers of the transport message currently being handled.
        /// </summary>
        IDictionary<string, object> Headers { get; }

        /// <summary>
        /// Instructs rebus handling infraestructure to skips the handler 
        /// specified by type on it's current invocation.
        /// </summary>
        /// <param name="type">The type of handler.</param>
        void SkipHandler(Type type);

        /// <summary>
        /// Removes the specified handler type from the list of handlers to skip.
        /// </summary>
        /// <param name="type">The type of handler.</param>
        void DoNotSkipHandler(Type type);

    }
}