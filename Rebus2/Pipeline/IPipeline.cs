using System.Collections.Generic;

namespace Rebus2.Pipeline
{
    public interface IPipeline
    {
        IEnumerable<IStep> SendPipeline();
        IEnumerable<StagedReceiveStep> ReceivePipeline();
    }

    public class StagedReceiveStep
    {
        public StagedReceiveStep(IStep step, ReceiveStage stage)
        {
            Step = step;
            Stage = stage;
        }

        public IStep Step { get; private set; }
        public ReceiveStage Stage { get; private set; }
    }

    public enum ReceiveStage
    {
        TransportMessageReceived = 1000,
        MessageDeserialized = 2000,
    }
}