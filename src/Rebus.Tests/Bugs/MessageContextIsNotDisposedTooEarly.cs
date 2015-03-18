using System;
using System.Collections.Generic;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Castle.Windsor;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class MessageContextIsNotDisposedTooEarly : FixtureBase
    {
        const string InputQueueName = "test.input.dispose.context";
        const string ErrorQueueName = "error";

        [Test]
        public void ItIsTrueItIsNot()
        {
            using (var container = new WindsorContainer())
            {
                var events = new List<string>();

                // we use these two bad boys to get callbacks when stuff happens
                var unitOfWorkManager = new FactoryUnitOfWorkManager();

                // register event listeners
                unitOfWorkManager.Created += () => events.Add("uow created");
                unitOfWorkManager.Committed += () => events.Add("uow committed");
                unitOfWorkManager.Aborted += () => events.Add("uow aborted");
                unitOfWorkManager.Disposed += () => events.Add("uow disposed");
                
                StringHandler.MessageHandled += () => events.Add("message handled (StringHandler)");
                AnotherStringHandler.MessageHandled += () => events.Add("message handled (AnotherStringHandler)");

                // put the bad boys to use
                container.Register(
                    Component.For<IHandleMessages<string>>()
                        .ImplementedBy<StringHandler>(),

                    Component.For<IHandleMessages<string>>()
                        .ImplementedBy<AnotherStringHandler>()
                    );

                Configure.With(new WindsorContainerAdapter(container))
                    .Logging(l => l.ColoredConsole(minLevel:LogLevel.Warn))
                    .Transport(t => t.UseMsmq(InputQueueName, ErrorQueueName))
                    .Events(e =>
                    {
                        e.AddUnitOfWorkManager(unitOfWorkManager);

                        // also gets callbacks when stuff happens on the context
                        e.MessageContextEstablished += (bus, context) =>
                        {
                            events.Add("context established");
                            context.Disposed += () => events.Add("context disposed");
                        };
                    })
                    .CreateBus()
                    .Start();

                container.Resolve<IBus>().SendLocal("hej der!");

                Thread.Sleep(1000);

                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine(string.Join(Environment.NewLine, events));
                Console.WriteLine("------------------------------------------------------------------------");

                Assert.That(events.IndexOf("uow committed"), Is.LessThan(events.IndexOf("context disposed")));
            }
        }

        class StringHandler : IHandleMessages<string>
        {
            public static event Action MessageHandled = delegate { };

            public StringHandler(IMessageContext context /* trigger Windsor's tracking */)
            {
            }

            public void Handle(string message)
            {
                MessageHandled();
            }
        }

        class AnotherStringHandler : IHandleMessages<string>
        {
            public static event Action MessageHandled = delegate { };

            public AnotherStringHandler(IMessageContext context /* trigger Windsor's tracking */)
            {
            }

            public void Handle(string message)
            {
                MessageHandled();
            }
        }

        class FactoryUnitOfWorkManager : IUnitOfWorkManager
        {
            public event Action Created = delegate { };
            public event Action Committed = delegate { };
            public event Action Aborted = delegate { };
            public event Action Disposed = delegate { };

            public IUnitOfWork Create()
            {
                var uow = new UnitOfWork();
                uow.Committed += Committed;
                uow.Aborted += Aborted;
                uow.Disposed += Disposed;
                Created();
                return uow;
            }

            class UnitOfWork : IUnitOfWork
            {
                public event Action Committed = delegate { };
                public event Action Aborted = delegate { };
                public event Action Disposed = delegate { };

                public void Dispose()
                {
                    Disposed();
                }

                public void Commit()
                {
                    Committed();
                }

                public void Abort()
                {
                    Aborted();
                }
            }
        }

        protected override void DoTearDown()
        {
            MsmqUtil.Delete(InputQueueName);
            MsmqUtil.Delete(ErrorQueueName);
        }
    }
}