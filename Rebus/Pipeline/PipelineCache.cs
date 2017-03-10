using System.Linq;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Cache that can be used as the outermost decorator in order to avoid contantly running any pipeline step injection logic
    /// that might be configured
    /// </summary>
    class PipelineCache : IPipeline
    {
        readonly IOutgoingStep[] _outgoingSteps;
        readonly IIncomingStep[] _incomingSteps;

        public PipelineCache(IPipeline pipeline)
        {
            _outgoingSteps = pipeline.SendPipeline().ToArray();
            _incomingSteps = pipeline.ReceivePipeline().ToArray();
        }

        public IOutgoingStep[] SendPipeline()
        {
            return _outgoingSteps;
        }

        public IIncomingStep[] ReceivePipeline()
        {
            return _incomingSteps;
        }
    }
}