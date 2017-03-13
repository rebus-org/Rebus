using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Pipeline;

namespace Rebus.Tests.Pipeline
{
    class CompiledPipelineInvoker : IPipelineInvoker
    {
        static readonly Task CompletedTask = Task.FromResult(0);

        readonly IIncomingStep[] _receivePipeline;
        readonly IOutgoingStep[] _sendPipeline;

        readonly Func<IncomingStepContext, Task> _invokeReceivePipeline;

        public CompiledPipelineInvoker(IPipeline pipeline)
        {
            _receivePipeline = pipeline.ReceivePipeline();
            _sendPipeline = pipeline.SendPipeline();

            _invokeReceivePipeline = GenerateReceiveAction(_receivePipeline);
        }

        public Task Invoke(IncomingStepContext context)
        {
            return _invokeReceivePipeline(context);
        }

        public async Task Invoke(OutgoingStepContext context)
        {
        }

        // we want to compile this bad boy
        /*
            return firstStep.Process(context0, () => {
                return secondStep.Process(context1, () => {
                    return thirdStep.Process(context2, () => {
                        return fourthStep.Process(context3, () => {
                            return Task.FromResult(0);
                        });
                    });
                });            
            }); 
        */
        Func<IncomingStepContext, Task> GenerateReceiveAction(IIncomingStep[] receivePipeline)
        {
            // pipeline terminator: create function (context) => CompletedTask
            var contextParameter = Expression.Parameter(typeof(IncomingStepContext), "context");
            var noopExpression = Expression.Constant(CompletedTask);
            var expression = Expression.Lambda<Func<IncomingStepContext, Task>>(noopExpression, contextParameter);

            // start with the end - construct each step function such that its invocation looks like this
            // (context) => step[n].Process(context, () => step[n+1].Process(...))
            for (var index = receivePipeline.Length - 1; index >= 0; index--)
            {
                var step = receivePipeline[index];
                var processMethod = GetProcessMethod(step);
                var currentExpression = expression.Compile();

                var stepReference = Expression.Constant(step);
                var nextExpression = Expression.Lambda<Func<Task>>();
                var invocation = Expression.Call(stepReference, processMethod, contextParameter, nextExpression);

                expression = Expression.Lambda<Func<IncomingStepContext, Task>>(invocation, contextParameter);
            }

            return expression.Compile();
        }

        Func<IncomingStepContext, Task> GenerateReceiveAction_initial(IIncomingStep[] receivePipeline)
        {
            var initialContextVariable = Expression.Parameter(typeof(IncomingStepContext), $"context{receivePipeline.Length}");
            var noopExpression = Expression.Constant(CompletedTask);
            var expression = Expression.Lambda<Func<IncomingStepContext, Task>>(noopExpression, initialContextVariable);

            for (var index = receivePipeline.Length - 1; index >= 0; index--)
            {
                var step = receivePipeline[index];
                var processMethod = GetProcessMethod(step);

                var contextVariable = Expression.Parameter(typeof(IncomingStepContext), $"context{index}");
                var stepReference = Expression.Constant(step);
                var nextVariable = Expression.Constant(expression);
                var invocation = Expression.Call(stepReference, processMethod, contextVariable, nextVariable);

                expression = Expression.Lambda<Func<IncomingStepContext, Task>>(invocation);
            }

            return expression.Compile();
        }

        static MethodInfo GetProcessMethod(IIncomingStep step)
        {
            var processMethod = step.GetType().GetMethod(nameof(IIncomingStep.Process), new[] {typeof(IncomingStepContext), typeof(Func<Task>)});

            if (processMethod == null)
            {
                throw new ArgumentException($"Could not find method with signature Process(IncomingStepContext context, Func<Task> next) on {step}");
            }

            return processMethod;
        }
    }
}