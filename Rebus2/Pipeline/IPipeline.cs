using System.Collections.Generic;

namespace Rebus2.Pipeline
{
    public interface IPipeline
    {
        IEnumerable<StagedStep<IOutgoingStep, SendStage>> SendPipeline();
        IEnumerable<StagedStep<IIncomingStep, ReceiveStage>> ReceivePipeline();
    }

    public class StagedStep<TStep, TStage> where TStep : IStep
    {
        public StagedStep(TStep step, TStage stage)
        {
            Step = step;
            Stage = stage;
        }

        public TStep Step { get; private set; }
        public TStage Stage { get; private set; }
    }

    public enum ReceiveStage
    {
        TransportMessageReceived = 1000,
        MessageDeserialized = 2000,
    }

    public enum SendStage
    {
        None
    }
}