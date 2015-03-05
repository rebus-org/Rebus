using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Pipeline
{
    public interface IPipelineInvoker
    {
        Task Invoke(IncomingStepContext context, IEnumerable<IIncomingStep> pipeline);
        Task Invoke(OutgoingStepContext context, IEnumerable<IOutgoingStep> pipeline);
    }
}