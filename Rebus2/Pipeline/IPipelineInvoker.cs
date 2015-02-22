using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus2.Pipeline
{
    public interface IPipelineInvoker
    {
        Task Invoke(IncomingStepContext context, IEnumerable<IIncomingStep> pipeline);
        Task Invoke(OutgoingStepContext context, IEnumerable<IOutgoingStep> pipeline);
    }
}