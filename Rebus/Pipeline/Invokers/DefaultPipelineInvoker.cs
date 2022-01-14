#pragma warning disable 1998
using System;
using System.Threading.Tasks;

namespace Rebus.Pipeline.Invokers;

/// <summary>
/// give me a pipeline and I'll invoke it
/// </summary>
class DefaultPipelineInvoker : IPipelineInvoker
{
    static readonly Task<int> Noop = Task.FromResult(0);
    static readonly Func<Task> TerminationStep = () => Noop;

    readonly Func<IncomingStepContext, Task> _processIncoming;
    readonly Func<OutgoingStepContext, Task> _processOutgoing;

    /// <summary>
    /// Constructs the invoker
    /// </summary>
    public DefaultPipelineInvoker(IPipeline pipeline)
    {
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

        var incomingSteps = pipeline.ReceivePipeline();
        var outgoingSteps = pipeline.SendPipeline();

        Task ProcessIncoming(IncomingStepContext context)
        {
            var step = TerminationStep;

            for (var index = incomingSteps.Length - 1; index >= 0; index--)
            {
                var nextStep = step;
                var stepToInvoke = incomingSteps[index];

                Task Next() => stepToInvoke.Process(context, nextStep);

                step = Next;
            }

            return step();
        }

        Task ProcessOutgoing(OutgoingStepContext context)
        {
            var step = TerminationStep;

            for (var index = outgoingSteps.Length - 1; index >= 0; index--)
            {
                var nextStep = step;
                var stepToInvoke = outgoingSteps[index];

                Task Next() => stepToInvoke.Process(context, nextStep);

                step = Next;
            }

            return step();
        }

        _processIncoming = ProcessIncoming;
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