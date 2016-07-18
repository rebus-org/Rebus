#pragma warning disable 1998
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
        static readonly Task<int> Noop = Task.FromResult(0);
        static readonly Func<Task> TerminationStep = () => Noop;

        /// <summary>
        /// Invokes the pipeline of <see cref="IIncomingStep"/> steps, passing the given <see cref="IncomingStepContext"/> to each step as it is invoked
        /// </summary>
        public Task Invoke(IncomingStepContext context, IEnumerable<IIncomingStep> pipeline)
        {
            var receivePipeline = GetPipeline(pipeline);
            var step = TerminationStep;

            for (var index = receivePipeline.Length - 1; index >= 0; index--)
            {
                var nextStep = step;
                var stepToInvoke = receivePipeline[index];
                step = () => stepToInvoke.Process(context, nextStep);
            }

            return step();

            //return Invoke(context, receivePipeline, 0);
        }

        // experimental recursive pipeline buildup - slightly slower than imperative
        //static Task Invoke(IncomingStepContext context, IIncomingStep[] receivePipeline, int index)
        //{
        //    if (index == receivePipeline.Length) return Noop;

        //    return receivePipeline[index].Process(context, () => Invoke(context, receivePipeline, index + 1));
        //}

        /// <summary>
        /// Invokes the pipeline of <see cref="IOutgoingStep"/> steps, passing the given <see cref="OutgoingStepContext"/> to each step as it is invoked
        /// </summary>
        public Task Invoke(OutgoingStepContext context, IEnumerable<IOutgoingStep> pipeline)
        {
            var receivePipeline = GetPipeline(pipeline);
            var step = TerminationStep;

            for (var index = receivePipeline.Length - 1; index >= 0; index--)
            {
                var nextStep = step;
                var stepToInvoke = receivePipeline[index];
                step = () => stepToInvoke.Process(context, nextStep);
            }

            return step();
        }

        static TStepType[] GetPipeline<TStepType>(IEnumerable<TStepType> pipeline)
        {
            return pipeline as TStepType[] ?? pipeline.ToArray();
        }
    }
}