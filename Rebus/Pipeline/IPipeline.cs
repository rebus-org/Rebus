using System.Collections.Generic;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Models a pipeline of steps that will be executed for each sent/received message respectively
    /// </summary>
    public interface IPipeline
    {
        /// <summary>
        /// Gets the send pipeline, i.e. the sequence of <see cref="IOutgoingStep"/> implementations that will be executed for each outgoing message
        /// </summary>
        IEnumerable<StagedStep<IOutgoingStep, SendStage>> SendPipeline();

        /// <summary>
        /// Gets the receive pipeline, i.e. the sequence of <see cref="IIncomingStep"/> implementations that will be executed for each incoming message
        /// </summary>
        IEnumerable<StagedStep<IIncomingStep, ReceiveStage>> ReceivePipeline();
    }
}