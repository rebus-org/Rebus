using System;
using System.Collections.Concurrent;
using System.Linq;
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
using Rebus.Sagas.Idempotent;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Tried (without success, so far) to reproduce an issue reported by a user. It COULD have been caused by a missing await, which has been added to this code.")]
public class ReproduceFunnySagaIssue : FixtureBase
{
    [Test]
    public async Task SeeIfThisWorks()
    {
        var completedSagaInstanceIds = new ConcurrentQueue<string>();
        var listLoggerFactory = new ListLoggerFactory();

        var activator = Using(new BuiltinHandlerActivator());

        activator.Register((bús, _) => new TestSaga(bús, listLoggerFactory, completedSagaInstanceIds));

        var bus = Configure.With(activator)
            .Logging(l => l.Use(listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "saga-tjekkerino"))
            .Sagas(s => s.StoreInMemory())
            .Start();

        await bus.Subscribe<SagaMessageEarth>();
        await bus.Subscribe<SagaMessageWind>();
        await bus.Subscribe<SagaMessageFire>();

        var starters = Enumerable.Range(0, 100)
            .Select(n => new KickoffSagaMessages { SagaInstanceId = n.ToString() })
            .ToList();

        await Task.WhenAll(starters.Select(starter => bus.SendLocal(starter)));

        await completedSagaInstanceIds.WaitUntil(q => q.Count == starters.Count, timeoutSeconds: 10);

        Assert.That(completedSagaInstanceIds.Except(starters.Select(s => s.SagaInstanceId)).ToList(), Has.Count.EqualTo(0));
    }

    public class KickoffSagaMessages
    {
        public string SagaInstanceId { get; set; }
    }

    public interface ISagaMessage
    {
        string SagaInstanceId { get; set; }
    }

    public class SagaMessageEarth : ISagaMessage
    {
        public string SagaInstanceId { get; set; }
    }

    public class SagaMessageWind : ISagaMessage
    {
        public string SagaInstanceId { get; set; }
    }

    public class SagaMessageFire : ISagaMessage
    {
        public string SagaInstanceId { get; set; }
    }

    public class TestSagaData : IdempotentSagaData
    {
        public string stuffDone;
        public bool Task1Processed { get; set; }
        public bool Task2Processed { get; set; }
        public bool Task3Processed { get; set; }
        public string SagaInstanceId { get; set; }
    }

    public class TestSaga : IdempotentSaga<TestSagaData>,
        IAmInitiatedBy<KickoffSagaMessages>,
        IHandleMessages<SagaMessageEarth>,
        IHandleMessages<SagaMessageWind>,
        IHandleMessages<SagaMessageFire>,
        IHandleMessages<IFailed<ISagaMessage>>
    {
        readonly ConcurrentQueue<string> _completedSagaInstanceIds;
        readonly ILog _logger;
        readonly IBus _bus;

        public TestSaga(IBus bus, IRebusLoggerFactory rebusLoggerFactory, ConcurrentQueue<string> completedSagaInstanceIds)
        {
            _bus = bus ?? throw new ArgumentNullException();
            _completedSagaInstanceIds = completedSagaInstanceIds;
            _logger = rebusLoggerFactory.GetLogger<TestSaga>();
        }

        protected override async Task ResolveConflict(TestSagaData otherSagaData)
        {
            Data.Task1Processed = Data.Task1Processed || otherSagaData.Task1Processed;
            Data.Task2Processed = Data.Task2Processed || otherSagaData.Task2Processed;
            Data.Task3Processed = Data.Task3Processed || otherSagaData.Task3Processed;
        }

        public async Task Handle(SagaMessageEarth message)
        {
            try
            {
                if (!Data.Task1Processed)
                {
                    _logger.Info("Processing Earth - {id}", Data.SagaInstanceId);
                    Data.stuffDone += "Earth;";
                    Data.Task1Processed = true;
                }
                await _bus.Publish(new SagaMessageWind()
                {
                    SagaInstanceId = message.SagaInstanceId
                }).ConfigureAwait(false);
                PossiblyDone();
                _logger.Info("Published Wind...Done Processing Earth - {id}", Data.SagaInstanceId);
            }
            catch (Exception e)
            {
                _logger.Error(e, "WHAT Earth? - {id}", Data.SagaInstanceId);
                throw;
            }
        }

        public async Task Handle(SagaMessageWind message)
        {
            try
            {
                if (!Data.Task2Processed)
                {
                    _logger.Info("Processing Wind - {id}", Data.SagaInstanceId);
                    Data.stuffDone += "Wind;";
                    Data.Task2Processed = true;
                }
                await _bus.Publish(new SagaMessageFire()
                {
                    SagaInstanceId = message.SagaInstanceId
                }).ConfigureAwait(false);
                PossiblyDone();
                _logger.Info("Published Fire...Done Processing Wind - {id}", Data.SagaInstanceId);
            }
            catch (Exception e)
            {
                _logger.Error(e, "WHAT Wind? - {id}", Data.SagaInstanceId);
                throw;
            }
        }

        public async Task Handle(SagaMessageFire message)
        {
            try
            {
                if (!Data.Task3Processed)
                {
                    //throw new Exception("Not going to finish this");
                    _logger.Info("Processing Fire - {id}", Data.SagaInstanceId);
                    Data.stuffDone += "Fire;";
                    Data.Task3Processed = true;
                }
                PossiblyDone();
                _logger.Info("Done Processing Fire - {id}", Data.SagaInstanceId);
            }
            catch (Exception e)
            {
                _logger.Error(e, "WHAT Fire? - {id}", Data.SagaInstanceId);
                throw;
            }
        }

        void PossiblyDone()
        {
            if (Data.Task1Processed && Data.Task2Processed && Data.Task3Processed)
            {
                _logger.Info("Completed everything for {id}: {msg}", Data.SagaInstanceId, Data.stuffDone);
                _completedSagaInstanceIds.Enqueue(Data.SagaInstanceId);
                MarkAsComplete();
            }
            else
            {
                _logger.Info("NOT Completed everything for {id}: {task1},{task2},{task3}", Data.SagaInstanceId, Data.Task1Processed, Data.Task2Processed, Data.Task3Processed);
            }
        }

        public async Task Handle(KickoffSagaMessages message)
        {
            _logger.Info("Processing Kickoff - {id}", Data.SagaInstanceId);
            Data.SagaInstanceId = message.SagaInstanceId;
            Data.stuffDone += "Initiated;";
            await _bus.Publish(new SagaMessageEarth
            {
                SagaInstanceId = message.SagaInstanceId
            });
            _logger.Info("Published Earth....Done Processing Kickoff - {id}", Data.SagaInstanceId);
        }

        protected override void CorrelateMessages(ICorrelationConfig<TestSagaData> config)
        {
            config.Correlate<KickoffSagaMessages>(m => m.SagaInstanceId, d => d.SagaInstanceId);
            config.Correlate<SagaMessageFire>(m => m.SagaInstanceId, d => d.SagaInstanceId);
            config.Correlate<SagaMessageEarth>(m => m.SagaInstanceId, d => d.SagaInstanceId);
            config.Correlate<SagaMessageWind>(m => m.SagaInstanceId, d => d.SagaInstanceId);
            config.Correlate<IFailed<ISagaMessage>>(m => m.Message.SagaInstanceId, d => d.SagaInstanceId);
        }

        public async Task Handle(IFailed<ISagaMessage> message)
        {
            _logger.Error("Unable to handle the message of type {msgtype} with error message {errMsg}", message.Message.GetType().Name, message.ErrorDescription);
        }
    }
}