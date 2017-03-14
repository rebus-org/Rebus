using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Rebus.Pipeline
{
    class CompiledPipelineInvoker : IPipelineInvoker
    {
        static readonly Task CompletedTask = Task.FromResult(0);

        readonly Func<IncomingStepContext, Task> _invokeReceivePipeline;
        readonly Func<OutgoingStepContext, Task> _invokeSendPipeline;

        public CompiledPipelineInvoker(IPipeline pipeline)
        {
            var receivePipeline = pipeline.ReceivePipeline();
            _invokeReceivePipeline = GenerateReceiveAction(receivePipeline);

            var sendPipeline = pipeline.SendPipeline();
            _invokeSendPipeline = GenerateSendAction(sendPipeline);
        }

        public Task Invoke(IncomingStepContext context)
        {
            return _invokeReceivePipeline(context);
        }

        public Task Invoke(OutgoingStepContext context)
        {
            return _invokeSendPipeline(context);
        }

        Func<OutgoingStepContext, Task> GenerateSendAction(IOutgoingStep[] sendPipeline)
        {
            // pipeline terminator: create function (context) => CompletedTask
            var contextParameter = Expression.Parameter(typeof(OutgoingStepContext), "context");
            var noopExpression = Expression.Constant(CompletedTask);
            var expression = Expression.Lambda<Func<OutgoingStepContext, Task>>(noopExpression, contextParameter);

            // start with the end - construct each step function such that its invocation looks like this
            // (context) => step[n-1].Process(context, () => step[n].Process(...))
            for (var index = sendPipeline.Length - 1; index >= 0; index--)
            {
                var step = sendPipeline[index];
                var processMethod = GetProcessMethod(step, nameof(IOutgoingStep.Process), typeof(OutgoingStepContext));

                var stepReference = Expression.Constant(step);
                var nextExpression = Expression.Lambda<Func<Task>>(Expression.Invoke(expression, contextParameter));
                var invocation = Expression.Call(stepReference, processMethod, contextParameter, nextExpression);

                expression = Expression.Lambda<Func<OutgoingStepContext, Task>>(invocation, contextParameter);
            }

            return expression.Compile();
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
            // (context) => step[n-1].Process(context, () => step[n].Process(...))
            for (var index = receivePipeline.Length - 1; index >= 0; index--)
            {
                var step = receivePipeline[index];
                var processMethod = GetProcessMethod(step, nameof(IIncomingStep.Process), typeof(IncomingStepContext));

                var stepReference = Expression.Constant(step);
                var nextExpression = Expression.Lambda<Func<Task>>(Expression.Invoke(expression, contextParameter));
                var invocation = Expression.Call(stepReference, processMethod, contextParameter, nextExpression);

                expression = Expression.Lambda<Func<IncomingStepContext, Task>>(invocation, contextParameter);
            }

            return expression.Compile();
        }

        static MethodInfo GetProcessMethod(IStep step, string methodName, Type contextType)
        {
            var processMethod = step.GetType().GetMethod(methodName, new[] {contextType, typeof(Func<Task>)});

            if (processMethod == null)
            {
                throw new ArgumentException($"Could not find method with signature {methodName}({contextType.Name} context, Func<Task> next) on {step}");
            }

            return processMethod;
        }
    }
}