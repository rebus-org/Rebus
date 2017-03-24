#pragma warning disable 1998
using System;
using System.Threading.Tasks;

namespace Rebus.Pipeline.Invokers
{
    /// <summary>
    /// give me a pipeline and I'll invoke it
    /// </summary>
    class DefaultPipelineInvoker : IPipelineInvoker
    {
        static readonly Task<int> Noop = Task.FromResult(0);
        static readonly Func<Task> TerminationStep = () => Noop;
        readonly IOutgoingStep[] _outgoingSteps;
        readonly IIncomingStep[] _incomingSteps;

        /// <summary>
        /// Constructs the invoker
        /// </summary>
        public DefaultPipelineInvoker(IPipeline pipeline)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            _outgoingSteps = pipeline.SendPipeline();
            _incomingSteps = pipeline.ReceivePipeline();
        }

        /// <summary>
        /// Invokes the pipeline of <see cref="IIncomingStep"/> steps, passing the given <see cref="IncomingStepContext"/> to each step as it is invoked
        /// </summary>
        public Task Invoke(IncomingStepContext context)
        {
            var step = TerminationStep;

            for (var index = _incomingSteps.Length - 1; index >= 0; index--)
            {
                var nextStep = step;
                var stepToInvoke = _incomingSteps[index];
                step = () => stepToInvoke.Process(context, nextStep);
            }

            return step();
        }

        /// <summary>
        /// Invokes the pipeline of <see cref="IOutgoingStep"/> steps, passing the given <see cref="OutgoingStepContext"/> to each step as it is invoked
        /// </summary>
        public Task Invoke(OutgoingStepContext context)
        {
            var step = TerminationStep;

            for (var index = _outgoingSteps.Length - 1; index >= 0; index--)
            {
                var nextStep = step;
                var stepToInvoke = _outgoingSteps[index];
                step = () => stepToInvoke.Process(context, nextStep);
            }

            return step();
        }
    }
}