using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus2.Pipeline
{
    public interface IPipelineInvoker
    {
        Task Invoke(StepContext context, IEnumerable<IStep> pipeline);
    }
}