using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using FastExpressionCompiler;

namespace Rebus.Pipeline.Invokers
{
    /// <summary>
    /// Expression-based pipeline invoker that builds a compiled function to invoke the pipeline(s)
    /// </summary>
    class CompiledPipelineInvoker : IPipelineInvoker
    {
        static readonly Task CompletedTask = Task.FromResult(0);

        readonly Func<IncomingStepContext, Task> _invokeReceivePipeline;
        readonly Func<OutgoingStepContext, Task> _invokeSendPipeline;

        public CompiledPipelineInvoker(IPipeline pipeline)
        {
            _invokeReceivePipeline = GenerateFunc<IncomingStepContext, IIncomingStep>(
                pipeline.ReceivePipeline(),
                nameof(IIncomingStep.Process)
            );

            _invokeSendPipeline = GenerateFunc<OutgoingStepContext, IOutgoingStep>(
                pipeline.SendPipeline(),
                nameof(IOutgoingStep.Process)
            );
        }

        public Task Invoke(IncomingStepContext context)
        {
            return _invokeReceivePipeline(context);
        }

        public Task Invoke(OutgoingStepContext context)
        {
            return _invokeSendPipeline(context);
        }

        static Func<TContext, Task> GenerateFunc<TContext, TStep>(IReadOnlyList<TStep> steps, string processMethodName)
            where TContext : StepContext
            where TStep : IStep
        {
            var expression = GenerateExpression<TContext, TStep>(steps, processMethodName, 0);

            // use Dadhi's fast expression compiler because he's awesome - https://github.com/dadhi
            return ExpressionCompiler.Compile<Func<TContext, Task>>(expression);
            
            //return expression.Compile();
        }

        /// <summary>
        /// Recursively builds and expression that invokes the pipeline, FUN STYLE BABY!
        /// </summary>
        static Expression<Func<TContext, Task>> GenerateExpression<TContext, TStep>(IReadOnlyCollection<TStep> steps, string processMethodName, int index)
            where TContext : StepContext
            where TStep : IStep
        {
            // we need a context parameter no matter what
            var contextParameter = Expression.Parameter(typeof(TContext), $"context{index}");

            // if we have no steps to invoke, just return the terminator expression
            if (!steps.Any())
            {
                var noopExpression = Expression.Constant(CompletedTask);

                return Expression.Lambda<Func<TContext, Task>>(noopExpression, contextParameter);
            }

            // otherwise, get an expression for invoking the tail of the pipeline...
            var tail = steps.Skip(1).ToList();
            var tailExpression = GenerateExpression<TContext, TStep>(tail, processMethodName, index + 1);

            // ...and attach it to the invocation of the head of the pipeline:
            var head = steps.First();
            var processMethod = GetProcessMethod(head, processMethodName, typeof(TContext));

            var stepReference = Expression.Constant(head);
            var invocationExpression = Expression.Invoke(tailExpression, contextParameter);
            var nextExpression = Expression.Lambda<Func<Task>>(invocationExpression);
            var callExpression = Expression.Call(stepReference, processMethod, contextParameter, nextExpression);

            return Expression.Lambda<Func<TContext, Task>>(callExpression, contextParameter);
        }

        static MethodInfo GetProcessMethod(IStep step, string methodName, Type contextType)
        {
            var processMethod = step.GetType().GetMethod(methodName, new[] { contextType, typeof(Func<Task>) })
                ?? throw new ArgumentException($"Could not find method with signature {methodName}({contextType.Name} context, Func<Task> next) on {step}");

            return processMethod;
        }
    }
}