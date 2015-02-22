using System.Collections.Generic;

namespace Rebus2.Pipeline
{
    public interface IPipeline
    {
        IEnumerable<StagedStep<IOutgoingStep>> SendPipeline();
        IEnumerable<StagedStep<IIncomingStep>> ReceivePipeline();
    }

    public class StagedStep<TStep> where TStep : IStep
    {
        public StagedStep(TStep step, ReceiveStage stage)
        {
            Step = step;
            Stage = stage;
        }

        public TStep Step { get; private set; }
        public ReceiveStage Stage { get; private set; }
    }

    public enum ReceiveStage
    {
        TransportMessageReceived = 1000,
        MessageDeserialized = 2000,
    }
}