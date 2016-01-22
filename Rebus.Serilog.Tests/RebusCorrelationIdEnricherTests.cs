using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Serilog.Tests.Support;
using Rebus.Tests;
using Rebus.Transport.InMem;
using Serilog;
using Serilog.Events;

namespace Rebus.Serilog.Tests
{
    public class RebusCorrelationIdEnricherTests : FixtureBase
    {
        [Test]
        public void IncludesCorrelationIdInEventProperties()
        {
            LogEvent evt = null;
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .Enrich.WithRebusCorrelationId(Headers.CorrelationId)
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var activator = new BuiltinHandlerActivator();
            
            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "input"))
                .Logging(l => l.Serilog(Log.Logger))
                .Options(o => o.LogPipeline(true))
                .Start();

            var counter = new SharedCounter(1);

            Using(counter);

            activator.Handle<DateTime>(timestamp => 
            {
                Assert.NotNull(evt);

                var correlationId = (string)evt.Properties[Headers.CorrelationId].LiteralValue();
                Assert.AreEqual("known-correlation-id", correlationId);

                counter.Decrement();

                return Task.FromResult(0);
            });

            var headers = new Dictionary<string, string>
            {
                { Headers.CorrelationId, "known-correlation-id" }
            };

            activator.Bus.SendLocal(DateTime.UtcNow, headers).Wait();

            counter.WaitForResetEvent(10);
        }
    }
}
