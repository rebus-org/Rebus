using System;
using System.Transactions;
using Rebus.Transport;

namespace Rebus.TransactionScopes
{
    /// <summary>
    /// Extension for <see cref="TransactionScope"/> that allows for enlisting an ambient Rebus transaction in an ambient .NET transaction.
    /// </summary>
    public static class TransactionScopeExtensions
    {
        /// <summary>
        /// Starts a new Rebus transcation, enlisting it to be committed in the COMMIT phase of the ambient .NET transaction. Can only be
        /// called once
        /// </summary>
        public static TransactionScope EnlistRebus(this TransactionScope transactionScope)
        {
            if (transactionScope == null) throw new ArgumentNullException(nameof(transactionScope));

            if (AmbientTransactionContext.Current != null)
            {
                throw new InvalidOperationException("Cannot start a new ambient Rebus transaction because there is already one associated with the current execution context!");
            }

            if (Transaction.Current == null)
            {
                throw new InvalidOperationException(
                    "Cannot enlist a new ambient Rebus transaction in the current transaction scope, but there's no current transaction" +
                    " on the thread!! Did you accidentally begin the transaction scope WITHOUT the TransactionScopeAsyncFlowOption.Enabled" +
                    " option? You must ALWAYS remember the TransactionScopeAsyncFlowOption.Enabled switch when you start an ambient .NET" +
                    " transaction and you intend to work with async/await, because otherwise the ambient .NET transaction will not flow" +
                    " properly to threads when executing continuations.");
            }

            var transactionContext = new DefaultTransactionContext();

            Transaction.Current.EnlistVolatile(new AmbientTransactionBridge(transactionContext), EnlistmentOptions.None);

            AmbientTransactionContext.Current = transactionContext;

            return transactionScope;
        }

        class AmbientTransactionBridge : IEnlistmentNotification
        {
            readonly DefaultTransactionContext _transactionContext;

            public AmbientTransactionBridge(DefaultTransactionContext transactionContext)
            {
                _transactionContext = transactionContext;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                AssertTransactionIsThere();

                try
                {
                    using (_transactionContext)
                    {
                        _transactionContext.Complete().Wait();
                    }
                }
                finally
                {
                    CleanUp();
                }

                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                AssertTransactionIsThere();

                try
                {
                    _transactionContext.Dispose();
                }
                finally
                {
                    CleanUp();
                }

                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }

            static void AssertTransactionIsThere()
            {
                if (AmbientTransactionContext.Current == null)
                {
                    throw new InvalidOperationException("WHERE IS THE REBUS TRANSCATION?");
                }
            }

            static void CleanUp()
            {
                AmbientTransactionContext.Current = null;
            }
        }
    }
}