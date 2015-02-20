using System.Collections.Generic;

namespace Rebus2.Pipeline
{
    public interface IPipelineManager
    {
        IEnumerable<IStep> SendPipeline();
        IEnumerable<IStep> ReceivePipeline();
    }
}