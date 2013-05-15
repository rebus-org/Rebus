using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Tests.Integration;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestWorker_UnitOfWork : FixtureBase
    {
        MessageReceiverForTesting receiveMessages;
        HandlerActivatorForTesting handlerActivatorForTesting;
        Worker worker;
        UnitOfWorkManagerForTesting unitOfWorkManager;

        protected override void DoSetUp()
        {
            receiveMessages = new MessageReceiverForTesting(new JsonMessageSerializer());
            handlerActivatorForTesting = new HandlerActivatorForTesting();

            unitOfWorkManager = new UnitOfWorkManagerForTesting();
            worker = new Worker(new ErrorTracker("error") {MaxRetries = 1},
                                receiveMessages,
                                handlerActivatorForTesting,
                                new InMemorySubscriptionStorage(),
                                new JsonMessageSerializer(),
                                new InMemorySagaPersister(),
                                new TrivialPipelineInspector(), "Just some test worker",
                                new DeferredMessageHandlerForTesting(),
                                new IncomingMessageMutatorPipelineForTesting(),
                                null,
                                new IUnitOfWorkManager[] {unitOfWorkManager});
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
            handlerActivatorForTesting.Handle<string>(str => unitOfWorkManager.RegisterEvent("Handled message: " + str));

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
                .Handle<string>(str =>
                    {
                        unitOfWorkManager.RegisterEvent("Handled message: " + str);
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
            handlerActivatorForTesting.Handle<string>(str => unitOfWorkManager.RegisterEvent("Handled message: " + str));

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
        public void DoesNotChokeIfBothCommitAndAbortFail()
        {
            // arrange
            handlerActivatorForTesting.Handle<string>(str => unitOfWorkManager.RegisterEvent("Handled message: " + str));

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

        Message MessageWith(object contents)
        {
            return new Message { Headers = new Dictionary<string, object>(), Messages = new[] { contents } };
        }
    }
}