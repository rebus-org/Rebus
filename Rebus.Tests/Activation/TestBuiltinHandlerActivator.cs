using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Tests.Extensions;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.Tests.Activation;

[TestFixture]
public class TestBuiltinHandlerActivator : FixtureBase
{
    BuiltinHandlerActivator _activator;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();
    }

    protected override void TearDown()
    {
        AmbientTransactionContext.SetCurrent(null);

        _activator.Dispose();
    }

    /*
     * Start:
     * 10000000 resolutions took 23.0 s - that's 434865.4 /s
     * 10000000 resolutions took 22.9 s - that's 437611.7 /s
     *
     * After caching some things:
     * 10000000 resolutions took 20.6 s - that's 485046.6 /s
     * 10000000 resolutions took 21.0 s - that's 476136.2 /s
     *
     * After replacing LINQ concat with Array.Copy:
     * 10000000 resolutions took 18.2 s - that's 550570.0 /s
     *
     * After replacing LINQ totally:
     * 10000000 resolutions took 14.1 s - that's 709939.2 /s
     *
     * Down to one single array allocation:
     * 10000000 resolutions took 13.6 s - that's 735231.0 /s
     */

    [TestCase(10 * 1000 * 1000)]
    public void TakeTime(int count)
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        activator.Handle<string>(async str => { });
        activator.Handle<string>(async (bus, str) => { });
        activator.Handle<string>(async (bus, context, str) => { });

        var stopwatch = Stopwatch.StartNew();

        for (var counter = 0; counter < count; counter++)
        {
            using (var scope = new FakeMessageContextScope())
            {
                // ReSharper disable once UnusedVariable
                var handlers = _activator.GetHandlers("hej med dig", scope.TransactionContext).Result;
            }
        }

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"{count} resolutions took {elapsedSeconds:0.0} s - that's {count/elapsedSeconds:0.0} /s");
    }

    [Test]
    public void CanGetHandlerWithoutArguments()
    {
        _activator.Register(() => new SomeHandler());

        using (var scope = new FakeMessageContextScope())
        {
            var handlers = _activator.GetHandlers("hej med dig", scope.TransactionContext).Result;

            Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
        }
    }

    [Test]
    public void CanGetHandlerWithMessageContextArgument()
    {
        _activator.Register(context => new SomeHandler());

        using (var scope = new FakeMessageContextScope())
        {
            var handlers = _activator.GetHandlers("hej med dig", scope.TransactionContext).Result;

            Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
        }
    }

    [Test]
    public void CanGetHandlerWithBusAndMessageContextArgument()
    {
        _activator.Register((bus, context) => new SomeHandler());

        using (var scope = new FakeMessageContextScope())
        {
            var handlers = _activator.GetHandlers("hej med dig", scope.TransactionContext).Result;

            Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
        }
    }

    class SomeHandler : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new System.NotImplementedException();
        }
    }
}