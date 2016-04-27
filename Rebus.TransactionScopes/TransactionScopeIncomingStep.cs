using System;
using System.Threading.Tasks;
using System.Transactions;
using Rebus.Pipeline;

namespace Rebus.TransactionScopes
{
    [StepDocumentation("Executes the rest of the pipeline inside a transaction scope, completing the transaction if the execution succeeds. TransactionScope is created with TransactionScopeAsyncFlowOption.Enabled, which allows it to flow properly to continuations (which is why this package requires at least .NET 4.5.1)")]
    class TransactionScopeIncomingStep : IIncomingStep
    {
        const TransactionScopeOption ScopeOption = TransactionScopeOption.Required;
        const TransactionScopeAsyncFlowOption AsyncFlowOption = TransactionScopeAsyncFlowOption.Enabled;

        readonly TransactionOptions _transactionOptions;

        public TransactionScopeIncomingStep(TransactionOptions transactionOptions)
        {
            _transactionOptions = transactionOptions;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            using (var scope = new TransactionScope(ScopeOption, _transactionOptions, AsyncFlowOption))
            {
                await next();
                scope.Complete();
            }
        }
    }
}