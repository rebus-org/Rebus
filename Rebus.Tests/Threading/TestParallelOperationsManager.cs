using NUnit.Framework;
using Rebus.Threading;
using System;
using System.Threading;

namespace Rebus.Tests.Threading
{
    [TestFixture]
    public class TestParallelOperationsManager
    {
        [Test]
        public void DoesNotAllowMoreThanMaxParallelismToContinue()
        {
            var manager = new ParallelOperationsManager(3);

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var operation1 = manager.PeekOperation(cts.Token);
            var operation2 = manager.PeekOperation(cts.Token);
            var operation3 = manager.PeekOperation(cts.Token);
            Assert.Throws<OperationCanceledException>(() => manager.PeekOperation(cts.Token));
        }

        [Test]
        public void ReleasesOperationAsExpected()
        {
            var manager = new ParallelOperationsManager(3);
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var operation1 = manager.PeekOperation(cts.Token);
            var operation2 = manager.PeekOperation(cts.Token);
            var operation3 = manager.PeekOperation(cts.Token);

            operation1.Dispose();

            var operation4 = manager.PeekOperation(cts.Token);

            Assert.Throws<OperationCanceledException>(() => manager.PeekOperation(cts.Token));
        }
    }
}