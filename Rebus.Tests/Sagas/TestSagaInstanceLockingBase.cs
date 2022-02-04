using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Retry.Simple;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.Tests.Sagas;

public abstract class TestSagaInstanceLockingBase : FixtureBase
{
    readonly InMemNetwork _network = new();
    readonly ListLoggerFactory _listLoggerFactory = new();

    BuiltinHandlerActivator _activator;
    IBusStarter _busStarter;

    protected abstract void EnforceExclusiveAccess(StandardConfigurer<ISagaStorage> configurer);

    protected override void SetUp()
    {
        _network.Reset();
        _listLoggerFactory.Clear();

        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        _busStarter = Configure.With(_activator)
            .Logging(l => l.Use(_listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(_network, "in-process locking"))
            .Sagas(s =>
            {
                s.StoreInMemory();
                EnforceExclusiveAccess(s);
            })
            .Options(o =>
            {
                o.SimpleRetryStrategy(maxDeliveryAttempts: 0);
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(20);
            })
            .Create();
    }

    [TestCase(10)]
    //[TestCase(100)]
    //[TestCase(1000)]
    //[TestCase(100)]
    public async Task NotASingleConcurrencyExceptionPlease(int messageCount)
    {
        const string caseNumber = "case-123";

        using var sagaWasInitiated = new ManualResetEvent(false);
        using var sagaWasMarkedAsComplete = new ManualResetEvent(false);

        _activator.Register((b, _) => new MySaga(b, sagaWasInitiated, sagaWasMarkedAsComplete));

        _busStarter.Start();

        var bus = _activator.Bus;

        var replyIdsToWaitFor = Enumerable.Range(0, messageCount).Select(n => $"reply-{n}").ToList();

        // start the saga
        var initiator = new StartSaga(caseNumber, replyIdsToWaitFor.ToHashSet());
        await bus.SendLocal(initiator);

        // wait until we know it was started
        sagaWasInitiated.WaitOrDie(TimeSpan.FromSeconds(2), "Saga was not properly initiated. That was weird.");
        await Task.Delay(300); // wait an additional short while to be sure the saga was saved

        // force saga instance to handle many messages concurrently
        await Task.WhenAll(replyIdsToWaitFor
            .Select(replyId => bus.SendLocal(new SimulateReply(caseNumber, replyId))));

        try
        {
            // wait until saga is completed
            sagaWasMarkedAsComplete.WaitOrDie(TimeSpan.FromSeconds(10 * messageCount + 5),
                @"The saga was not completed within timeout. 

This is most likely a sign that too many ConcurrencyExceptions 
forced one or more messages into the error queue");
        }
        catch (Exception exception)
        {
            throw new AssertionException($@"The error queue has {_network.GetCount("error")} messages. Here's the logs:

{_listLoggerFactory}

", exception);
        }

        // check log for errors
        var errors = _listLoggerFactory.Where(l => l.Level == LogLevel.Error).ToList();
        Assert.That(errors.Count, Is.EqualTo(0), $@"Did not expect any errors in the log, but the following errors were present:

{string.Join(Environment.NewLine, errors)}

Bummer dude.");

        // check error queue for failes messages
        var errorCount = _network.GetCount("error");
        Assert.That(errorCount, Is.EqualTo(0), "Did not expect any messages in the error queue");
    }

    class StartSaga
    {
        public StartSaga(string caseNumber, HashSet<string> replyIdsToWaitFor)
        {
            CaseNumber = caseNumber;
            ReplyIdsToWaitFor = replyIdsToWaitFor;
        }

        public string CaseNumber { get; }
        public HashSet<string> ReplyIdsToWaitFor { get; }
    }

    class SimulateReply
    {
        public string CaseNumber { get; }
        public string ReplyId { get; }

        public SimulateReply(string caseNumber, string replyId)
        {
            CaseNumber = caseNumber;
            ReplyId = replyId;
        }
    }

    class MySaga : Saga<MySagaData>, IAmInitiatedBy<StartSaga>, IHandleMessages<SimulateReply>
    {
        readonly IBus _bus;
        readonly ManualResetEvent _sagaWasInitiated;
        readonly ManualResetEvent _sagaWasMarkedAsComplete;

        public MySaga(IBus bus, ManualResetEvent sagaWasInitiated, ManualResetEvent sagaWasMarkedAsComplete)
        {
            _bus = bus;
            _sagaWasInitiated = sagaWasInitiated;
            _sagaWasMarkedAsComplete = sagaWasMarkedAsComplete;
        }

        protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
        {
            config.Correlate<StartSaga>(m => m.CaseNumber, d => d.CaseNumber);
            config.Correlate<SimulateReply>(m => m.CaseNumber, d => d.CaseNumber);
        }

        public async Task Handle(StartSaga message)
        {
            Data.CaseNumber = message.CaseNumber;

            foreach (var replyId in message.ReplyIdsToWaitFor)
            {
                Data.PendingReplies.Add(replyId);
            }

            await Task.Run(() => _sagaWasInitiated.Set());
        }

        public async Task Handle(SimulateReply message)
        {
            Data.PendingReplies.Remove(message.ReplyId);

            await Task.Delay(100);

            if (!Data.PendingReplies.Any())
            {
                MarkAsComplete();

                _sagaWasMarkedAsComplete.Set();
            }
        }
    }

    class MySagaData : SagaData
    {
        public string CaseNumber { get; set; }

        public HashSet<string> PendingReplies { get; } = new HashSet<string>();
    }
}