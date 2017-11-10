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

        readonly Func<IncomingStepContext, Task> _processIncoming;
        readonly Func<OutgoingStepContext, Task> _processOutgoing;

        /// <summary>
        /// Constructs the invoker
        /// </summary>
        public DefaultPipelineInvokerNew(IPipeline pipeline)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

            var outgoingSteps = pipeline.SendPipeline();
            var incomingSteps = pipeline.ReceivePipeline();

            Task ProcessIncoming(IncomingStepContext context)
            {
                Task InvokerFunction(int index)
                {
                    if (index == incomingSteps.Length) return Noop;

                    Task InvokeNext() => InvokerFunction(index + 1);

                    return incomingSteps[index]
                        .Process(context, InvokeNext);
                }

                return InvokerFunction(0);
            }

            _processIncoming = ProcessIncoming;

            Task ProcessOutgoing(OutgoingStepContext context)
            {
                Task InvokerFunction(int index)
                {
                    if (index == outgoingSteps.Length) return Noop;

                    Task InvokeNext() => InvokerFunction(index + 1);

                    return outgoingSteps[index]
                        .Process(context, InvokeNext);
                }

                return InvokerFunction(0);
            }

            _processOutgoing = ProcessOutgoing;
        }

        /// <summary>
        /// Invokes the pipeline of <see cref="IIncomingStep"/> steps, passing the given <see cref="IncomingStepContext"/> to each step as it is invoked
        /// </summary>
        public Task Invoke(IncomingStepContext context) => _processIncoming(context);

        /// <summary>
        /// Invokes the pipeline of <see cref="IOutgoingStep"/> steps, passing the given <see cref="OutgoingStepContext"/> to each step as it is invoked
        /// </summary>
        public Task Invoke(OutgoingStepContext context) => _processOutgoing(context);
    }
}