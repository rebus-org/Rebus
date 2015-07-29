using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestMaxRetriesConfiguration : FixtureBase
    {
        ManualResetEvent resetEvent;
        BuiltinContainerAdapter adapter;
        List<string> logStatements;
        CustomizableThrowingUnifOfWorkManager unifOfWorkManager;
        const string InputQueueName = "test.configurable.retries.input";
        const string ErrorQueueName = "test.configurable.retries.error";

        protected override void DoSetUp()
        {
            resetEvent = new ManualResetEvent(false);
            adapter = TrackDisposable(new BuiltinContainerAdapter());
            logStatements = new List<string>();
            unifOfWorkManager = new CustomizableThrowingUnifOfWorkManager();
        }

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();
            MsmqUtil.Delete(InputQueueName);
            MsmqUtil.Delete(ErrorQueueName);
            base.DoTearDown();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void CanConfigureMaxRetriesForExceptionThrownWhenCommittingUnitOfWork(int retries)
        {
            // arrange
            adapter.Handle<string>(str => {/* <noop>
                                             
                                              
                                             
                                             d[-_-]b   
                                             
                                             
                                             </noop>*/
            });
            
            unifOfWorkManager.CommitShouldFail = true;

            var bus = ConfigureBus(b => b.SetMaxRetriesFor<UnitOfWorkCommitException>(retries));

            // act
            bus.SendLocal("hey!!");
            var timeout = 5.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not get poison even within {0} timeout!", timeout);

            // just let everything calm down
            Thread.Sleep(1.Seconds());

            Console.WriteLine(string.Join(Environment.NewLine, logStatements));

            // assert
            Assert.That(logStatements.Count(s => s.Contains("WARN") && s.Contains(typeof(UnitOfWorkCommitException).Name)),
                Is.EqualTo(retries),
                "Did not find the expected number of warnings containing a ToString representation of the CustomException exception");

            Assert.That(logStatements.Count(s => s.Contains("ERROR") && s.Contains(typeof(UnitOfWorkCommitException).Name)),
                Is.EqualTo(1), "Did not find exactly one error");

            var errorLine = logStatements.Single(s => s.Contains("ERROR") && s.Contains(typeof(UnitOfWorkCommitException).Name));
            var occurrencesOfExceptionInfo = errorLine.CountOcurrencesOf(typeof(UnitOfWorkCommitException).Name);
            Assert.That(occurrencesOfExceptionInfo, Is.EqualTo(retries), 
                "Did not find the expected number of exceptions aggregated into the single error message that should present full stack traces for all collected exceptions");
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void CanConfigureMaxRetriesForExceptionThrownByHandler(int retries)
        {
            // arrange
            adapter.Handle<string>(str =>
            {
                throw new CustomException();
            });

            var bus = ConfigureBus(b => b.SetMaxRetriesFor<CustomException>(retries));

            // act
            bus.SendLocal("hey!!");
            var timeout = 5.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not get poison even within {0} timeout!", timeout);

            // just let everything calm down
            Thread.Sleep(1.Seconds());

            Console.WriteLine(string.Join(Environment.NewLine, logStatements));

            // assert
            Assert.That(logStatements.Count(s => s.Contains("WARN") && s.Contains(typeof (CustomException).Name)),
                Is.EqualTo(retries),
                "Did not find the expected number of warnings containing a ToString representation of the CustomException exception");

            Assert.That(logStatements.Count(s => s.Contains("ERROR") && s.Contains(typeof (CustomException).Name)),
                Is.EqualTo(1), "Did not find exactly one error");

            var errorLine = logStatements.Single(s => s.Contains("ERROR") && s.Contains(typeof (CustomException).Name));
            var occurrencesOfExceptionInfo = errorLine.CountOcurrencesOf(typeof(CustomException).Name);
            Assert.That(occurrencesOfExceptionInfo, Is.EqualTo(retries), 
                "Did not find the expected number of exceptions aggregated into the single error message that should present full stack traces for all collected exceptions");
        }

        IBus ConfigureBus(Action<RebusBehaviorConfigurer> customizeBehavior)
        {
            return Configure.With(adapter)
                .Logging(l => l.Use(new ListLoggerFactory(logStatements)))
                .Transport(t => t.UseMsmq(InputQueueName, ErrorQueueName))
                .Behavior(customizeBehavior)
                .Events(e => e.PoisonMessage += delegate { resetEvent.Set(); })
                .Events(e => e.AddUnitOfWorkManager(unifOfWorkManager))
                .CreateBus()
                .Start();
        }

        class CustomizableThrowingUnifOfWorkManager : IUnitOfWorkManager, IUnitOfWork
        {
            public bool CommitShouldFail { get; set; }
            public bool AbortShouldFail { get; set; }
            
            public IUnitOfWork Create()
            {
                return this;
            }

            public void Dispose()
            {
            }

            public void Commit()
            {
                if (!CommitShouldFail) return;

                throw new ApplicationException("COMMIT FAILED!");
            }

            public void Abort()
            {
                if (!AbortShouldFail) return;

                throw new ApplicationException("ABORT FAILED!");
            }
        }

        public class CustomException : ApplicationException
        {
            public CustomException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public CustomException()
            {
            }
        }
    }
}