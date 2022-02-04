using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Routing;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable RedundantLambdaParameterType
#pragma warning disable 1998

namespace Rebus.Tests.Routing;

[TestFixture]
public class TestRoutingSlip : FixtureBase
{
    readonly InMemNetwork _network = new();
    readonly ListLoggerFactory _listLoggerFactory = new(true);

    protected override void SetUp()
    {
        _listLoggerFactory.Clear();
        _network.Reset();
    }

    [Test]
    public async Task CheckHeaders()
    {
        var seqValues = new List<string>();
        var travelogueValues = new List<string>();
        var correlationIdValues = new List<string>();

        void HandleHeaders(Dictionary<string, string> headers)
        {
            Console.WriteLine($@"Headers:
{string.Join(Environment.NewLine, headers.Select(kvp => $"    {kvp.Key}: {kvp.Value}"))}
");
            seqValues.Add(headers.GetValue(Headers.CorrelationSequence));
            travelogueValues.Add(headers.GetValue(Headers.RoutingSlipTravelogue));
            correlationIdValues.Add(headers.GetValue(Headers.CorrelationId));
        }

        StartBus("endpoint-a").Activator.AddHandlerWithBusTemporarilyStopped<string>(async (_, context, _) => HandleHeaders(context.Headers));
        StartBus("endpoint-b").Activator.AddHandlerWithBusTemporarilyStopped<string>(async (_, context, _) => HandleHeaders(context.Headers));
        StartBus("endpoint-c").Activator.AddHandlerWithBusTemporarilyStopped<string>(async (_, context, _) => HandleHeaders(context.Headers));

        var routingSlipWasReturnedToSender = new ManualResetEvent(false);

        var initiator = StartBus("initiator").Activator.AddHandlerWithBusTemporarilyStopped<string>(async (_, context, _) =>
        {
            HandleHeaders(context.Headers);
            routingSlipWasReturnedToSender.Set();
        });

        var itinerary = new Itinerary("endpoint-a", "endpoint-b", "endpoint-c").ReturnToSender();

        await initiator.Bus.Advanced.Routing.SendRoutingSlip(itinerary, "HEJ MED DIG DU");

        routingSlipWasReturnedToSender.WaitOrDie(TimeSpan.FromSeconds(3));

        Assert.That(seqValues, Is.EqualTo(new[] { "0", "1", "2", "3" }));
        Assert.That(travelogueValues, Is.EqualTo(new[]
        {
            "",
            "endpoint-a",
            "endpoint-a;endpoint-b",
            "endpoint-a;endpoint-b;endpoint-c",
        }));
        Assert.That(correlationIdValues, Is.EqualTo(Enumerable.Repeat(correlationIdValues.First(), 4)));
    }

    [Test]
    public async Task WorksGreatWithMutableMessagesToo()
    {
        StartBus("endpoint-a").Activator.AddHandlerWithBusTemporarilyStopped<SomeMutableMessage>(async message => message.AddLine("Handled by endpoint-a"));
        StartBus("endpoint-b").Activator.AddHandlerWithBusTemporarilyStopped<SomeMutableMessage>(async message => message.AddLine("Handled by endpoint-b"));
        StartBus("endpoint-c").Activator.AddHandlerWithBusTemporarilyStopped<SomeMutableMessage>(async message => message.AddLine("Handled by endpoint-c"));

        var routingSlipWasReturnedToSender = new ManualResetEvent(false);
        var collectedLines = new List<string>();

        var initiator = StartBus("initiator").Activator.AddHandlerWithBusTemporarilyStopped<SomeMutableMessage>(async message =>
        {
            collectedLines.AddRange(message.Lines);
            routingSlipWasReturnedToSender.Set();
        });

        var itinerary = new Itinerary("endpoint-a", "endpoint-b", "endpoint-c").ReturnToSender();

        await initiator.Bus.Advanced.Routing.SendRoutingSlip(itinerary, new SomeMutableMessage());

        routingSlipWasReturnedToSender.WaitOrDie(TimeSpan.FromSeconds(3));

        Assert.That(collectedLines, Is.EqualTo(new[]
        {
            "Handled by endpoint-a",
            "Handled by endpoint-b",
            "Handled by endpoint-c",
        }));
    }

    class SomeMutableMessage
    {
        readonly List<string> _lines = new();

        public SomeMutableMessage(IEnumerable<string> lines = null) => _lines.AddRange(lines ?? Enumerable.Empty<string>());

        public IEnumerable<string> Lines => _lines;

        public void AddLine(string line) => _lines.Add(line);
    }

    [Test]
    public async Task CanRouteMessageAsExpected_NeverReturn()
    {
        var done = new ManualResetEvent(false);
        var events = new ConcurrentQueue<string>();

        StartBus("endpoint-a").Activator.AddHandlerWithBusTemporarilyStopped<string>(async _ => events.Enqueue("a"));
        StartBus("endpoint-b").Activator.AddHandlerWithBusTemporarilyStopped<string>(async _ => events.Enqueue("b"));
        StartBus("endpoint-c").Activator.AddHandlerWithBusTemporarilyStopped<string>(async _ => events.Enqueue("c"));
                    
        StartBus("endpoint-d").Activator.AddHandlerWithBusTemporarilyStopped<string>(async _ =>
        {
            events.Enqueue("d");
            done.Set();
        });

        var initiator = StartBus("initiator");

        var itinerary = new Itinerary("endpoint-a", "endpoint-b", "endpoint-c", "endpoint-d");

        await initiator.Bus.Advanced.Routing.SendRoutingSlip(itinerary, "YO!!");

        done.WaitOrDie(TimeSpan.FromSeconds(3));

        await Task.Delay(TimeSpan.FromSeconds(0.5));

        Assert.That(events, Is.EqualTo(new[] { "a", "b", "c", "d" }));

        var warningLinesOrWorse = _listLoggerFactory.Where(l => l.Level >= LogLevel.Warn).ToList();
        Assert.That(warningLinesOrWorse.Any, Is.False, $@"Did NOT expect any warnings or errors in the log - got these lines with WARN level or above:

{string.Join(Environment.NewLine, warningLinesOrWorse)}


They should not have been there
");
    }

    [Test]
    public async Task CanRouteMessageAsExpected_ReturnToSender()
    {
        var a = StartBus("endpoint-a");
        var b = StartBus("endpoint-b");
        var c = StartBus("endpoint-c");

        var routingSlipWasReturnedToSender = new ManualResetEvent(false);

        var initiator = StartBus("initiator", (SomeMessage _) => routingSlipWasReturnedToSender.Set());

        var itinerary = new Itinerary("endpoint-a", "endpoint-b", "endpoint-c")
            .ReturnToSender();

        await initiator.Bus.Advanced.Routing.SendRoutingSlip(itinerary, new SomeMessage("hello there!"));

        routingSlipWasReturnedToSender.WaitOrDie(TimeSpan.FromSeconds(3));

        Assert.That(a.Events, Contains.Item(@"Handled string ""hello there!"" in endpoint-a"));
        Assert.That(b.Events, Contains.Item(@"Handled string ""hello there!"" in endpoint-b"));
        Assert.That(c.Events, Contains.Item(@"Handled string ""hello there!"" in endpoint-c"));
    }

    class SomeMessage
    {
        public SomeMessage(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    RoutingSlipDestination StartBus(string queueName, Action<SomeMessage> someMessageHandler = null)
    {
        var activator = new BuiltinHandlerActivator();
        var events = new ConcurrentQueue<string>();

        Using(activator);

        if (someMessageHandler == null)
        {
            activator.Handle<SomeMessage>(async message =>
            {
                var text = $@"Handled string ""{message.Text}"" in {queueName}";
                Console.WriteLine(text);
                events.Enqueue(text);
            });
        }
        else
        {
            activator.Handle<SomeMessage>(async message => someMessageHandler(message));
        }

        Configure.With(activator)
            .Logging(l => l.Use(_listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(_network, queueName))
            .Start();

        return new RoutingSlipDestination(activator.Bus, activator, events);
    }

    class RoutingSlipDestination
    {
        public IBus Bus { get; }
        public BuiltinHandlerActivator Activator { get; }
        public ConcurrentQueue<string> Events { get; }

        public RoutingSlipDestination(IBus bus, BuiltinHandlerActivator activator, ConcurrentQueue<string> events)
        {
            Bus = bus;
            Activator = activator;
            Events = events;
        }
    }

}