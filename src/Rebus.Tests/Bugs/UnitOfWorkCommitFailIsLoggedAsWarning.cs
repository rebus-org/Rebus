using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Raven.Client.Extensions;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class UnitOfWorkCommitFailIsLoggedAsWarning : FixtureBase, IUnitOfWorkManager
    {
        const string RecognizableErrorMessage = "BAM!!!!1111";
        const string InputQueueName = "test.uow.commit.logging";
        BuiltinContainerAdapter adapter;
        List<IDisposable> disposables;
        List<string> emittedLogStatements;
        ManualResetEvent resetEvent;

        protected override void DoSetUp()
        {
            emittedLogStatements = new List<string>();
            adapter = new BuiltinContainerAdapter();
            disposables = new List<IDisposable> { adapter };
            resetEvent = new ManualResetEvent(false);
            Configure.With(adapter)
                .Logging(l => l.Use(new ListLoggerFactory(emittedLogStatements)))
                .Transport(t => t.UseMsmq(InputQueueName, "error"))
                .Events(e => e.AddUnitOfWorkManager(this))
                .Events(e => e.PoisonMessage += delegate { resetEvent.Set(); })
                .CreateBus()
                .Start();
        }

        protected override void DoTearDown()
        {
            disposables.ForEach(d => d.Dispose());
            MsmqUtil.Delete(InputQueueName);
        }

        [Test]
        public void StatementOfSomethingThatMustHold()
        {
            adapter.Handle<string>(Console.WriteLine);
            MakeUnitOfWorkThrowOnCommit = true;

            adapter.Bus.SendLocal("you'll throw now!");
            var timeout = 5.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not receive PoisonMessage event within {0} timeout", timeout);

            Console.WriteLine(string.Join(Environment.NewLine, emittedLogStatements));

            Assert.That(emittedLogStatements
                .Count(s => s.Contains("WARN") && s.Contains(RecognizableErrorMessage)), Is.EqualTo(5),
                "Expected 5 warnings in the log prior to the logging of the full exception information");

            Assert.That(emittedLogStatements.Count(s => s.Contains("ERROR") && s.Contains(RecognizableErrorMessage)),
                Is.EqualTo(1),
                "Expected exactly one single ERROR in the log due to the message having failed <max-retries> times");

            Assert.That(emittedLogStatements.Single(s => s.Contains("ERROR")).CountOcurrencesOf(RecognizableErrorMessage),
                Is.EqualTo(5),
                "Expected that the error message would turn up exactly 5 times withing the detailed logging of the error");
        }

        public IUnitOfWork Create()
        {
            return new CustomizableUnitOfWorkForTesting { ThrowOnCommit = MakeUnitOfWorkThrowOnCommit };
        }

        bool MakeUnitOfWorkThrowOnCommit { get; set; }

        class CustomizableUnitOfWorkForTesting : IUnitOfWork
        {
            public bool ThrowOnCommit { get; set; }
            public void Commit()
            {
                if (ThrowOnCommit)
                {
                    throw new ApplicationException(RecognizableErrorMessage);
                }
            }

            public void Dispose()
            {
            }

            public void Abort()
            {
            }
        }
    }
}