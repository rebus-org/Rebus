using System.Collections.Generic;
using System.Linq;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Default pipeline implementation that can be built with a fluent syntax by calling <see cref="OnSend"/> and <see cref="OnReceive"/> respectively,
    /// in the order that the steps must be invoked in.
    /// </summary>
    public class DefaultPipeline : IPipeline
    {
        readonly List<StagedStep<IOutgoingStep, SendStage>> _sendSteps = new List<StagedStep<IOutgoingStep, SendStage>>();
        readonly List<StagedStep<IIncomingStep, ReceiveStage>> _receiveSteps = new List<StagedStep<IIncomingStep, ReceiveStage>>();

        public IEnumerable<StagedStep<IOutgoingStep, SendStage>> SendPipeline()
        {
            return _sendSteps.Select(s => new StagedStep<IOutgoingStep, SendStage>(s.Step, SendStage.None));
        }

        public IEnumerable<StagedStep<IIncomingStep, ReceiveStage>> ReceivePipeline()
        {
            return _receiveSteps.Select(s => new StagedStep<IIncomingStep,ReceiveStage>(s.Step, s.Stage));
        }

        /// <summary>
        /// Adds a new incoming step to the receive pipeline
        /// </summary>
        public DefaultPipeline OnReceive(IIncomingStep step, ReceiveStage stage)
        {
            _receiveSteps.Add(new StagedStep<IIncomingStep, ReceiveStage>(step, stage));
            return this;
        }

        /// <summary>
        /// Adds a new outgoing step to the send pipeline
        /// </summary>
        public DefaultPipeline OnSend(IOutgoingStep step)
        {
            _sendSteps.Add(new StagedStep<IOutgoingStep, SendStage>(step, SendStage.None));
            return this;
        }
    }
}