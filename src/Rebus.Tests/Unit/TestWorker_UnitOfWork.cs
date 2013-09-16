using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Tests.Integration;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    internal class TestWorker_UnitOfWork : WorkerFixtureBase
    {
        MessageReceiverForTesting receiveMessages;
        HandlerActivatorForTesting handlerActivatorForTesting;
        Worker worker;
        UnitOfWorkManagerForTesting unitOfWorkManager;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(true) {MinLevel = LogLevel.Info};

            receiveMessages = new MessageReceiverForTesting(new JsonMessageSerializer());
            handlerActivatorForTesting = new HandlerActivatorForTesting();

            unitOfWorkManager = new UnitOfWorkManagerForTesting();
            worker = CreateWorker(receiveMessages, handlerActivatorForTesting,
                                  unitOfWorkManagers: new IUnitOfWorkManager[] {unitOfWorkManager},
                                  errorTracker: new ErrorTracker("error") {MaxRetries = 1});
        }

        class UnitOfWorkManagerForTesting : IUnitOfWorkManager
        {
            readonly List<string> events = new List<string>();
            readonly List<UnitOfWorkForTesting> unitsOfWork = new List<UnitOfWorkForTesting>();
            int uowInstanceCounter = 1;

            public void RegisterEvent(string whatHappened)
            {
                events.Add(whatHappened);
            }

            public IEnumerable<string> Events
            {
                get { return events.ToArray(); }
            }

            public bool CommitShouldFail { get; set; }

            public bool AbortShouldFail { get; set; }

            public IUnitOfWork Create()
            {
                var instanceNumber = uowInstanceCounter++;
                var unitOfWorkForTesting = new UnitOfWorkForTesting(this, instanceNumber, CommitShouldFail, AbortShouldFail);
                unitsOfWork.Add(unitOfWorkForTesting);
                RegisterEvent(string.Format("Unit of work created: {0}", instanceNumber));
                return unitOfWorkForTesting;
            }
        }

        class UnitOfWorkForTesting : IUnitOfWork
        {
            readonly UnitOfWorkManagerForTesting manager;
            readonly int instanceNumber;
            readonly bool commitShouldFail;
            readonly bool abortShouldFail;

            public UnitOfWorkForTesting(UnitOfWorkManagerForTesting manager, int instanceNumber, bool commitShouldFail, bool abortShouldFail)
            {
                this.manager = manager;
                this.instanceNumber = instanceNumber;
                this.commitShouldFail = commitShouldFail;
                this.abortShouldFail = abortShouldFail;
            }

            public void Dispose()
            {
                manager.RegisterEvent(string.Format("{0}: disposed", instanceNumber));
            }

            public void Commit()
            {
                manager.RegisterEvent(string.Format("{0}: committed", instanceNumber));

                if (commitShouldFail)
                {
                    throw new ApplicationException("COMMIT FAILED!!");
                }
            }

            public void Abort()
            {
                manager.RegisterEvent(string.Format("{0}: aborted", instanceNumber));

                if (abortShouldFail)
                {
                    throw new ApplicationException("ABORT FAILED!!");
                }
            }
        }

        [Test]
        public void CanProperlyCommitUnitOfWork()
        {
            // arrange
            handlerActivatorForTesting.Handle<TextMessage>(msg => unitOfWorkManager.RegisterEvent("Handled message: " + msg.Text));

            // act
            receiveMessages.Deliver(MessageWith("hello there!"));
            worker.Start();
            Thread.Sleep(500);
            worker.Stop();

            // assert
            unitOfWorkManager.Events
                             .ShouldBe(new[]
                                           {
                                               "Unit of work created: 1",
                                               "Handled message: hello there!",
                                               "1: committed",
                                               "1: disposed",
                                           });
        }

        [Test]
        public void CanProperlyRollBackUnitOfWork()
        {
            // arrange
            handlerActivatorForTesting
                .Handle<TextMessage>(msg =>
                    {
                        unitOfWorkManager.RegisterEvent("Handled message: " + msg.Text);
                        throw new OmfgExceptionThisIsBad("wut?!");
                    });

            // act
            receiveMessages.Deliver(MessageWith("hello there!"));
            worker.Start();
            Thread.Sleep(500);
            worker.Stop();

            // assert
            unitOfWorkManager.Events
                             .ShouldBe(new[]
                                           {
                                               "Unit of work created: 1",
                                               "Handled message: hello there!",
                                               "1: aborted",
                                               "1: disposed",
                                           });
        }

        [Test]
        public void WillTryToRollBackIfUowCommitFails()
        {
            // arrange
            handlerActivatorForTesting.Handle<TextMessage>(msg => unitOfWorkManager.RegisterEvent("Handled message: " + msg.Text));

            unitOfWorkManager.CommitShouldFail = true;

            // act
            receiveMessages.Deliver(MessageWith("hello there!"));
            worker.Start();
            Thread.Sleep(500);
            worker.Stop();

            // assert
            unitOfWorkManager.Events
                             .ShouldBe(new[]
                                           {
                                               "Unit of work created: 1",
                                               "Handled message: hello there!",
                                               "1: committed",
                                               "1: aborted",
                                               "1: disposed",
                                           });
        }

        [Test]
        public void WhenUowCommitFailsItIsConsideredToBeAUserException()
        {
            // arrange
            RebusLoggerFactory.Current = new NullLoggerFactory();
            var exceptions = new List<Tuple<string, Exception>>();
            worker.UserException += (w, e) =>
                {
                    Console.WriteLine("USER exception: {0}", e.Message);
                    exceptions.Add(Tuple.Create("USER", e));
                };
            worker.SystemException += (w, e) =>
                {
                    Console.WriteLine("SYSTEM exception: {0}", e.Message);
                    exceptions.Add(Tuple.Create("SYSTEM", e));
                };

            worker.PoisonMessage += (m, info) =>
                {
                    var lastException = info.Exceptions.Last().Value;
                    Console.WriteLine("POISON message: {0}", lastException.Message);
                    exceptions.Add(Tuple.Create("POISON", lastException));
                };

            handlerActivatorForTesting.Handle<TextMessage>(str => { });

            unitOfWorkManager.CommitShouldFail = true;

            // act
            receiveMessages.Deliver(MessageWith("hello there!"));
            worker.Start();
            Thread.Sleep(2.Seconds());
            worker.Stop();

            // assert
            CollectionAssert.AreEquivalent(new[] {"USER", "POISON"},
                                           exceptions.Select(e => e.Item1).ToArray(),
                                           @"Expected a USER followed by a POISON because:

    1) Attempt to deliver => Commit fails => BAM!!1 USER EXCEPTION
    2) Attempt to deliver => MaxRetries reached => BAM!1 POISON

");
        }

        [Test]
        public void DoesNotChokeIfBothCommitAndAbortFail()
        {
            // arrange
            handlerActivatorForTesting.Handle<TextMessage>(msg => unitOfWorkManager.RegisterEvent("Handled message: " + msg.Text));

            unitOfWorkManager.CommitShouldFail = true;
            unitOfWorkManager.AbortShouldFail = true;

            // act
            receiveMessages.Deliver(MessageWith("hello there!"));
            worker.Start();
            Thread.Sleep(500);
            worker.Stop();

            // assert
            unitOfWorkManager.Events
                             .ShouldBe(new[]
                                           {
                                               "Unit of work created: 1",
                                               "Handled message: hello there!",
                                               "1: committed",
                                               "1: aborted",
                                               "1: disposed",
                                           });
        }

        Message MessageWith(string text)
        {
            return new Message
                       {
                           Headers = new Dictionary<string, object>(),
                           Messages = new object[] {new TextMessage {Text = text}}
                       };
        }

        class TextMessage
        {
            public string Text { get; set; }
        }
    }
}