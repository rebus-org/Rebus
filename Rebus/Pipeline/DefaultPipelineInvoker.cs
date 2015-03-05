using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Pipeline
{
    /// <summary>
    /// give me a pipeline and I'll invoke it
    /// </summary>
    public class DefaultPipelineInvoker : IPipelineInvoker
    {
        public async Task Invoke(IncomingStepContext context, IEnumerable<IIncomingStep> pipeline)
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

        public async Task Invoke(OutgoingStepContext context, IEnumerable<IOutgoingStep> pipeline)
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