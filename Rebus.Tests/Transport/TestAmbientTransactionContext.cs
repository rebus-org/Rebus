using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Tests.Contracts;
using Rebus.Transport;
// ReSharper disable UnusedVariable
// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable RedundantAssignment
#pragma warning disable IDE0059

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
        var obj = new object();
        var weakObjectReference = new WeakReference(obj);

        Assert.That(weakObjectReference.IsAlive, Is.True, "Wait wat?");

        var task = StartSomethingAsync(obj);

        // clear this one, so the longevity of the object relies solely on being referenced from the transaction context
        obj = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.That(weakObjectReference.IsAlive, Is.True, 
            "One-second task has not finished running yet, so the weak reference should still be alive!");

        await task;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.That(weakObjectReference.IsAlive, Is.False,
            "One-second task has finished running, so the transaction context should have been cleared by now, thus rendering the weak reference dead");
    }

    static Task StartSomethingAsync(object obj)
    {
        Task task;

        using (var scope = new RebusTransactionScope())
        {
            scope.TransactionContext.Items["key"] = obj;

            task = Task.Run(async () => await Task.Delay(TimeSpan.FromSeconds(1)));

            scope.Complete();
        }

        return task;
    }
}