using System;
using System.Threading.Tasks;

namespace Rebus.Pipeline.Invokers;

/// <summary>
/// Action-based pipeline invoker that recursively composes actions to invoke the pipelines
/// </summary>
sealed class ActionPipelineInvoker : IPipelineInvoker
{
    readonly Func<IncomingStepContext, Task> _invokeReceivePipeline;
    readonly Func<OutgoingStepContext, Task> _invokeSendPipeline;

    public ActionPipelineInvoker(IPipeline pipeline)
    {
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

        _invokeReceivePipeline = GenerateReceiveFunction(
            pipeline.ReceivePipeline()
        );

        _invokeSendPipeline = GenerateSendFunction(
            pipeline.SendPipeline()
        );
    }

    public Task Invoke(IncomingStepContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        return _invokeReceivePipeline(context);
    }

    public Task Invoke(OutgoingStepContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        return _invokeSendPipeline(context);
    }

    static Func<IncomingStepContext, Task> GenerateReceiveFunction(Span<IIncomingStep> steps)
    {
        if (steps.IsEmpty)
        {
            Task CompletedFunction(IncomingStepContext context) => Task.CompletedTask;
            return CompletedFunction;
        }

        var head = steps[0];
        var tail = steps.Slice(1);
        var invokeTail = GenerateReceiveFunction(tail);

        Task ReceiveFunction(IncomingStepContext context)
        {
            Task NextFunction() => invokeTail(context);
            return head.Process(context, NextFunction);
        }

        return ReceiveFunction;
    }

    static Func<OutgoingStepContext, Task> GenerateSendFunction(Span<IOutgoingStep> steps)
    {
        if (steps.IsEmpty)
        {
            Task CompletedFunction(OutgoingStepContext context) => Task.CompletedTask;
            return CompletedFunction;
        }

        var head = steps[0];
        var tail = steps.Slice(1);
        var invokeTail = GenerateSendFunction(tail);

        Task SendFunction(OutgoingStepContext context)
        {
            Task NextFunction() => invokeTail(context);
            return head.Process(context, NextFunction);
        }

        return SendFunction;
    }
}