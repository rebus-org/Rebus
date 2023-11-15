using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Pipeline.Invokers;

/// <summary>
/// Action-based pipeline invoker that recursively composes actions to invoke the pipelines
/// </summary>
class ActionPipelineInvoker : IPipelineInvoker
{
    static readonly Task CompletedTask = Task.FromResult(0);

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

    static Func<IncomingStepContext, Task> GenerateReceiveFunction(IReadOnlyList<IIncomingStep> steps)
    {
        if (!steps.Any())
        {
            Task CompletedFunction(IncomingStepContext context) => CompletedTask;
            return CompletedFunction;
        }

        var head = steps.First();
        var tail = steps.Skip(1).ToList();
        var invokeTail = GenerateReceiveFunction(tail);

        Task ReceiveFunction(IncomingStepContext context)
        {
            Task NextFunction() => invokeTail(context);
            return head.Process(context, NextFunction);
        }

        return ReceiveFunction;
    }

    static Func<OutgoingStepContext, Task> GenerateSendFunction(IReadOnlyList<IOutgoingStep> steps)
    {
        if (!steps.Any())
        {
            Task CompletedFunction(OutgoingStepContext context) => CompletedTask;
            return CompletedFunction;
        }

        var head = steps.First();
        var tail = steps.Skip(1).ToList();
        var invokeTail = GenerateSendFunction(tail);

        Task SendFunction(OutgoingStepContext context)
        {
            Task NextFunction() => invokeTail(context);
            return head.Process(context, NextFunction);
        }

        return SendFunction;
    }
}