using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus2.Pipeline
{
    /// <summary>
    /// give me a pipeline and I'll invoke it
    /// </summary>
    public class PipelineInvoker
    {
        public void Invoke(StepContext context, IEnumerable<IStep> pipeline)
        {
            var receivePipeline = pipeline.ToList();

            Func<Task> step = (async () => { });

            for (var index = receivePipeline.Count - 1; index >= 0; index--)
            {
                var nextStep = step;
                var stepToInvoke = receivePipeline[index];
                step = () => stepToInvoke.Process(context, () => nextStep());
            }

            step();
        }
    }
}