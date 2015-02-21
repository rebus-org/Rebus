using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus2.Pipeline
{
    /// <summary>
    /// give me a pipeline and I'll invoke it
    /// </summary>
    public class DefaultPipelineInvoker : IPipelineInvoker
    {
        public async Task Invoke(StepContext context, IEnumerable<IStep> pipeline)
        {
            var receivePipeline = pipeline.ToList();

            Func<Task> step = (async () => { });

            for (var index = receivePipeline.Count - 1; index >= 0; index--)
            {
                var nextStep = step;
                var stepToInvoke = receivePipeline[index];
                step = async () => await stepToInvoke.Process(context, async () => await nextStep());
            }

            await step();
        }
    }
}