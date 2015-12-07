using System;
using System.Threading.Tasks;
using System.Transactions;
using Rebus.Pipeline;

namespace Rebus.TransactionScopes
{
    [StepDocumentation("Executes the rest of the pipeline inside a transaction scope, completing the transaction if the execution succeeds. TransactionScope is created with TransactionScopeAsyncFlowOption.Enabled, which allows it to flow properly to continuations (which is why this package requires at least .NET 4.5.1)")]
    class TransactionScopeIncomingStep : IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await next();
                scope.Complete();
            }
        }
    }
}