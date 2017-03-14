using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Implementation of <see cref="IPipelineInvoker"/> that builds an expression to invoke the steps
    /// of a pipeline
    /// </summary>
    class CompiledPipelineInvoker : IPipelineInvoker
    {
        static readonly Task CompletedTask = Task.FromResult(0);

        readonly Func<IncomingStepContext, Task> _invokeReceivePipeline;
        readonly Func<OutgoingStepContext, Task> _invokeSendPipeline;

        public CompiledPipelineInvoker(IPipeline pipeline)
        {
            var receivePipeline = pipeline.ReceivePipeline();

            _invokeReceivePipeline = GenerateFunc<IncomingStepContext, IIncomingStep>(receivePipeline, nameof(IIncomingStep.Process));

            var sendPipeline = pipeline.SendPipeline();

            _invokeSendPipeline = GenerateFunc<OutgoingStepContext, IOutgoingStep>(sendPipeline, nameof(IOutgoingStep.Process));
        }

        public Task Invoke(IncomingStepContext context)
        {
            return _invokeReceivePipeline(context);
        }

        public Task Invoke(OutgoingStepContext context)
        {
            return _invokeSendPipeline(context);
        }

        /* we want to compile this bad boy
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
        static Func<TContext, Task> GenerateFunc<TContext, TStep>(IReadOnlyList<TStep> sendPipeline, string processMethodName)
            where TContext : StepContext
            where TStep : IStep
        {
            // pipeline terminator: create function (context) => CompletedTask
            var contextParameter = Expression.Parameter(typeof(TContext), "contextPPP");
            var noopExpression = Expression.Constant(CompletedTask);
            var expression = Expression.Lambda<Func<TContext, Task>>(noopExpression, contextParameter);

            // start with the end - construct each step function such that its invocation looks like this
            // (context) => step[n-1].Process(context, () => step[n].Process(...))
            for (var index = sendPipeline.Count - 1; index >= 0; index--)
            {
                var step = sendPipeline[index];
                var processMethod = GetProcessMethod(step, processMethodName, typeof(TContext));

                var contextParameterp = Expression.Parameter(typeof(TContext), $"context{index}");
                var stepReference = Expression.Constant(step);
                var invocationExpression = Expression.Invoke(expression, contextParameterp);
                var nextExpression = Expression.Lambda<Func<Task>>(invocationExpression);
                var callExpression = Expression.Call(stepReference, processMethod, contextParameterp, nextExpression);

                expression = Expression.Lambda<Func<TContext, Task>>(callExpression, contextParameterp);
            }

            return expression.Compile();
        }

        static MethodInfo GetProcessMethod(IStep step, string methodName, Type contextType)
        {
            var processMethod = step.GetType().GetMethod(methodName, new[] { contextType, typeof(Func<Task>) });

            if (processMethod == null)
            {
                throw new ArgumentException($"Could not find method with signature {methodName}({contextType.Name} context, Func<Task> next) on {step}");
            }

            return processMethod;
        }
    }
}