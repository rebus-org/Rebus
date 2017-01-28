using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rebus.Messages;

namespace Rebus.Pipeline.Receive
{
    /// <summary>
    /// Represents a sequence of handler invokers
    /// </summary>
    public class HandlerInvokers : IEnumerable<HandlerInvoker>
    {
        readonly List<HandlerInvoker> _handlerInvokers;

        /// <summary>
        /// Constructs the sequence
        /// </summary>
        public HandlerInvokers(Message message, IEnumerable<HandlerInvoker> handlerInvokers)
        {
            Message = message;
            _handlerInvokers = handlerInvokers.ToList();
        }

        /// <summary>
        /// Gets the logical message that the accompanying handler invokers are working on
        /// </summary>
        public Message Message { get; }

        /// <summary>
        /// Gets all the <see cref="HandlerInvoker"/>s that this <see cref="HandlerInvokers"/> contains
        /// </summary>
        public IEnumerator<HandlerInvoker> GetEnumerator()
        {
            return _handlerInvokers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}