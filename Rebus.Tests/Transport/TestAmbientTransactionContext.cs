using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts;
using Rebus.Transport;
// ReSharper disable UnusedVariable

namespace Rebus.Tests.Transport;

[TestFixture]
public class TestAmbientTransactionContext : FixtureBase
{
    protected override void SetUp()
    {
        base.SetUp();

        Using(new DisposableCallback(() => AmbientTransactionContext.SetCurrent(null)));
    }

    [Test]
    public async Task ReproduceInconvenientCopyingOfAsyncLocalStuff()
    {
        // create an object and stash it in the transaction context
        var obj = new object();
        var transactionContext = new TransactionContext();
        transactionContext.Items["who cares"] = obj;

        // "start an ambient transaction"
        AmbientTransactionContext.SetCurrent(transactionContext);

        var queue = new ConcurrentQueue<object>();
        
        // start an asynchronous task, thus copying the current execution context to it
        var loop = WaitShortWhileAndEnqueueAmbientTransactionContext(queue);

        // "end the ambient transaction"
        AmbientTransactionContext.SetCurrent(null);

        // wait for task to complete
        await loop;

        // clear the local references
        obj = null;
        transactionContext = null;

        // force collection
        GC.Collect();

        // check what we got
        if (!queue.TryDequeue(out var returnedTransactionContext))
        {
            throw new AssertionException("No transaction context was apparently returned");
        }

        Assert.That(returnedTransactionContext, Is.Null,
            @"Expected the returned transaction context to have been NULL. 

When it isn't NULL, it means that the 'long-running task' has been capable of returning it after having been running for a while, which
in turn must mean that the task's execution context is still holding a reference to it somehow.");
    }

    static Task WaitShortWhileAndEnqueueAmbientTransactionContext(ConcurrentQueue<object> transactionContexts)
    {
        var task = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            var transactionContext = AmbientTransactionContext.Current;

            transactionContexts.Enqueue(transactionContext);
        });

        return task;
    }
}