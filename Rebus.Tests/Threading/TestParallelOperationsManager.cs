using NUnit.Framework;
using Rebus.Threading;

namespace Rebus.Tests.Threading;

[TestFixture]
public class TestParallelOperationsManager
{
    [Test]
    public void DoesNotAllowMoreThanMaxParallelismToContinue()
    {
        var manager = new ParallelOperationsManager(3);

        var operation1 = manager.TryBegin();
        var operation2 = manager.TryBegin();
        var operation3 = manager.TryBegin();
        var operation4 = manager.TryBegin();

        Assert.That(operation1.CanContinue(), Is.True);
        Assert.That(operation2.CanContinue(), Is.True);
        Assert.That(operation3.CanContinue(), Is.True);
        Assert.That(operation4.CanContinue(), Is.False);
    }

    [Test]
    public void ReleasesOperationAsExpected()
    {
        var manager = new ParallelOperationsManager(3);

        var operation1 = manager.TryBegin();
        var operation2 = manager.TryBegin();
        var operation3 = manager.TryBegin();

        operation1.Dispose();

        var operation4 = manager.TryBegin();
        var operation5 = manager.TryBegin();

        Assert.That(operation1.CanContinue(), Is.True);
        Assert.That(operation2.CanContinue(), Is.True);
        Assert.That(operation3.CanContinue(), Is.True);
        Assert.That(operation4.CanContinue(), Is.True);
        Assert.That(operation5.CanContinue(), Is.False);
    }

    [Test]
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

        Assert.That(op5.CanContinue(), Is.True);
        Assert.That(op6.CanContinue(), Is.True);
        Assert.That(op7.CanContinue(), Is.False);
    }
}