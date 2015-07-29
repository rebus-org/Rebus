using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Transports.Msmq;
using System.Linq;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Description("Verify that retries and logging behave as expected")]
    public class UnitOfWorkCommitFailDoesNotEscapeRetries : RebusBusMsmqIntegrationTestBase
    {
        const string InputQueueName = "test.uow.commit.input";
        const string RecognizableErrorMessage = "NOOOOO!!!!!!111";
        
        List<IDisposable> stuffToDispose;
        BuiltinContainerAdapter adapter;
        ManualResetEvent resetEvent;
        List<string> logStatements;
        WillAlwaysThrowWhenCommitting specialUnitOfWorkManager;

        protected override void DoSetUp()
        {
            logStatements = new List<string>();
            resetEvent = new ManualResetEvent(false);
            specialUnitOfWorkManager = new WillAlwaysThrowWhenCommitting();
            adapter = new BuiltinContainerAdapter();
            stuffToDispose = new List<IDisposable> {adapter};
            
            Configure.With(adapter)
                     .Logging(l => l.Use(new ListLoggerFactory(logStatements)))
                     .Transport(t => t.UseMsmq(InputQueueName, "error"))
                     .Events(e => e.AddUnitOfWorkManager(specialUnitOfWorkManager))
                     .Events(e => e.PoisonMessage += (bus, msg, info) => resetEvent.Set())
                     .CreateBus()
                     .Start(1);
        }

        protected override void DoTearDown()
        {
            Console.WriteLine(string.Join(Environment.NewLine, logStatements));
            stuffToDispose.ForEach(d => d.Dispose());
        }

        [Test]
        public void ItWorksWhenUowCommitFails()
        {
            specialUnitOfWorkManager.ThrowOnCommit = true;
            var deliveryAttempts = 0;
            adapter.Handle<string>(str => Interlocked.Increment(ref deliveryAttempts));

            adapter.Bus.SendLocal("hello!");

            var timeout = 5.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not receive PoisonMessage event within {0} timeout", timeout);
            Assert.That(deliveryAttempts, Is.EqualTo(5));

            Assert.That(logStatements.Count(s => s.Contains("ERROR")), Is.EqualTo(1));
            Assert.That(logStatements.Count(s => s.Contains("WARN") && s.Contains(RecognizableErrorMessage)), Is.EqualTo(5));
        }

        [Test]
        public void ItWorksWhenMessageHandlingFails()
        {
            var deliveryAttempts = 0;
            adapter.Handle<string>(str =>
                {
                    Interlocked.Increment(ref deliveryAttempts);
                    throw new ApplicationException(RecognizableErrorMessage);
                });

            adapter.Bus.SendLocal("hello!");

            var timeout = 5.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not receive PoisonMessage event within {0} timeout", timeout);
            Assert.That(deliveryAttempts, Is.EqualTo(5));

            Assert.That(logStatements.Count(s => s.Contains("ERROR")), Is.EqualTo(1));
            Assert.That(logStatements.Count(s => s.Contains("WARN") && s.Contains(RecognizableErrorMessage)), Is.EqualTo(5));
        }

        public class WillAlwaysThrowWhenCommitting : IUnitOfWorkManager, IUnitOfWork
        {
            public bool ThrowOnCommit { get; set; }

            public IUnitOfWork Create()
            {
                return this;
            }

            public void Dispose()
            {

            }

            public void Commit()
            {
                if (ThrowOnCommit)
                {
                    throw new ApplicationException(RecognizableErrorMessage);
                }
            }

            public void Abort()
            {
            }
        }
    }
}