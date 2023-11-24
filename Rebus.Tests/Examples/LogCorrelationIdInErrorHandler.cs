using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Examples;

[TestFixture]
public class LogCorrelationIdInErrorHandler : FixtureBase
{
    [Test]
    [Description("Verifies that the header can be found in the ambient message context when the message errors get logged")]
    public async Task CanGetMessageHeaders_NormalWithRetries()
    {
        using var activator = new BuiltinHandlerActivator();

        activator.Handle<string>(_ => throw new InvalidOperationException("oh no!"));

        var receivedValues = new ConcurrentQueue<string>();

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Options(o => o.Register<IExceptionLogger>(_ => new CustomExceptionLogger(receivedValues)))
            .Start();

        await bus.SendLocal("cannot dispatch this", new Dictionary<string, string> { ["custom-header"] = "hej med dig" });

        await receivedValues.WaitUntil(q => q.Count >= 5);

        Assert.That(receivedValues, Is.EqualTo(new[]
        {
            "hej med dig",
            "hej med dig",
            "hej med dig",
            "hej med dig",
            "hej med dig",
        }));
    }

    [Test]
    [Description("Verifies that the header can be found in the ambient message context when the message errors get logged only once as a FINAL")]
    public async Task CanGetMessageHeaders_Final()
    {
        using var activator = new BuiltinHandlerActivator();

        var receivedValues = new ConcurrentQueue<string>();

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Options(o => o.Register<IExceptionLogger>(_ => new CustomExceptionLogger(receivedValues)))
            .Start();

        await bus.SendLocal("cannot dispatch this", new Dictionary<string, string> { ["custom-header"] = "hej med dig" });

        await receivedValues.WaitUntil(q => q.Count >= 1);

        var receivedValue = receivedValues.First();

        Assert.That(receivedValue, Is.EqualTo("hej med dig"));
    }

    class CustomExceptionLogger : IExceptionLogger
    {
        readonly ConcurrentQueue<string> _receivedValues;

        public CustomExceptionLogger(ConcurrentQueue<string> receivedValues)
        {
            _receivedValues = receivedValues ?? throw new ArgumentNullException(nameof(receivedValues));
        }

        public void LogException(string messageId, Exception exception, int errorCount, bool isFinal)
        {
            var value = MessageContext.Current?.Headers?.GetValueOrNull("custom-header");

            _receivedValues.Enqueue(value);
        }
    }
}