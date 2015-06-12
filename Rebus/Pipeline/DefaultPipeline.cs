using System.Collections.Generic;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Default pipeline implementation that can be built with a fluent syntax by calling <see cref="OnSend"/> and <see cref="OnReceive"/> respectively,
    /// in the order that the steps must be invoked in.
    /// </summary>
    public class DefaultPipeline : IPipeline
    {
        readonly List<IOutgoingStep> _sendSteps = new List<IOutgoingStep>();
        readonly List<IIncomingStep> _receiveSteps = new List<IIncomingStep>();

        /// <summary>
        /// Gets the send pipeline
        /// </summary>
        public IEnumerable<IOutgoingStep> SendPipeline()
        {
            return _sendSteps;
        }

        /// <summary>
        /// Gets the receive pipeline
        /// </summary>
        public IEnumerable<IIncomingStep> ReceivePipeline()
        {
            return _receiveSteps;
        }

        /// <summary>
        /// Adds a new incoming step to the receive pipeline
        /// </summary>
        public DefaultPipeline OnReceive(IIncomingStep step)
        {
            _receiveSteps.Add(step);
            return this;
        }

        /// <summary>
        /// Adds a new outgoing step to the send pipeline
        /// </summary>
        public DefaultPipeline OnSend(IOutgoingStep step)
        {
            _sendSteps.Add(step);
            return this;
        }
    }
}