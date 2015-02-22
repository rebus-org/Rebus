using System.Collections.Generic;

namespace Rebus2.Pipeline
{
    public interface IPipeline
    {
        IEnumerable<IOutgoingStep> SendPipeline();
        IEnumerable<StagedReceiveStep<IIncomingStep>> ReceivePipeline();
    }

    public class StagedReceiveStep<T> where T:IStep
    {
        public StagedReceiveStep(T step, ReceiveStage stage)
        {
            Step = step;
            Stage = stage;
        }

        public T Step { get; private set; }
        public ReceiveStage Stage { get; private set; }
    }

    public enum ReceiveStage
    {
        TransportMessageReceived = 1000,
        MessageDeserialized = 2000,
    }
}