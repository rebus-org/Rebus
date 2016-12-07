using Rebus.Threading;
using Xunit;

namespace Rebus.Tests.Threading
{
    public class TestParallelOperationsManager
    {
        [Fact]
        public void DoesNotAllowMoreThanMaxParallelismToContinue()
        {
            var manager = new ParallelOperationsManager(3);

            var operation1 = manager.TryBegin();
            var operation2 = manager.TryBegin();
            var operation3 = manager.TryBegin();
            var operation4 = manager.TryBegin();

            Assert.True(operation1.CanContinue());
            Assert.True(operation2.CanContinue());
            Assert.True(operation3.CanContinue());
            Assert.False(operation4.CanContinue());
        }

        [Fact]
        public void ReleasesOperationAsExpected()
        {
            var manager = new ParallelOperationsManager(3);

            var operation1 = manager.TryBegin();
            var operation2 = manager.TryBegin();
            var operation3 = manager.TryBegin();

            operation1.Dispose();

            var operation4 = manager.TryBegin();
            var operation5 = manager.TryBegin();

            Assert.True(operation1.CanContinue());
            Assert.True(operation2.CanContinue());
            Assert.True(operation3.CanContinue());
            Assert.True(operation4.CanContinue());
            Assert.False(operation5.CanContinue());
        }

        [Fact]
        public void ReleasingOperationThatCouldNotContinueDoesNotAffectAnything()
        {
            var manager = new ParallelOperationsManager(2);

            var op1 = manager.TryBegin();
            var op2 = manager.TryBegin();

            var op3 = manager.TryBegin();
            var op4 = manager.TryBegin();

            op1.Dispose();
            op2.Dispose();
            op3.Dispose();
            op4.Dispose();

            var op5 = manager.TryBegin();
            var op6 = manager.TryBegin();
            var op7 = manager.TryBegin();

            Assert.True(op5.CanContinue());
            Assert.True(op6.CanContinue());
            Assert.False(op7.CanContinue());
        }
    }
}