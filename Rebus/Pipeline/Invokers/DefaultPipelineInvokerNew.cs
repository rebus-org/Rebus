#pragma warning disable 1998
using System;
using System.Threading.Tasks;

namespace Rebus.Pipeline.Invokers
{
    /// <summary>
    /// give me a pipeline and I'll invoke it
    /// </summary>
    class DefaultPipelineInvokerNew : IPipelineInvoker
    {
        static readonly Task<int> Noop = Task.FromResult(0);
        readonly IOutgoingStep[] _outgoingSteps;
        readonly IIncomingStep[] _incomingSteps;

        /// <summary>
        /// Constructs the invoker
        /// </summary>
        public DefaultPipelineInvokerNew(IPipeline pipeline)
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
            Task InvokerFunction(int index)
            {
                if (index == _incomingSteps.Length) return Noop;

                Task InvokeNext() => InvokerFunction(index + 1);

                return _incomingSteps[index]
                    .Process(context, InvokeNext);
            }

            return InvokerFunction(0);
        }

        /// <summary>
        /// Invokes the pipeline of <see cref="IOutgoingStep"/> steps, passing the given <see cref="OutgoingStepContext"/> to each step as it is invoked
        /// </summary>
        public Task Invoke(OutgoingStepContext context)
        {
            Task InvokerFunction(int index)
            {
                if (index == _outgoingSteps.Length) return Noop;

                Task InvokeNext() => InvokerFunction(index + 1);

                return _outgoingSteps[index]
                    .Process(context, InvokeNext);
            }

            return InvokerFunction(0);
        }
    }
}