using System;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Castle.Windsor;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class SendAndPublishAutomaticallyEnlistInAmbientTransaction : IDetermineMessageOwnership
    {
        const string InputQueueName = "test.txbug.input";

        public string GetEndpointFor(Type messageType)
        {
            return InputQueueName;
        }

        [Test, Description(@"This test verified that there was a bug that would leave a NoTransaction instance as the current Rebus transaction context after calling bus.Subscribe, which would then prevent an AmbientTransactionContext from being set when entering the TransactionScope")]
        public void ItWorks()
        {
            PrintPerson.ReceivedPeople = 0;

            using (var container = new WindsorContainer())
            {
                container.Register(Component.For<IHandleMessages<Person>>()
                                            .ImplementedBy<PrintPerson>()
                                            .LifestyleTransient());

                var bus = Configure.With(new WindsorContainerAdapter(container))
                                   .Logging(l => l.None())
                                   .Transport(t => t.UseMsmq(InputQueueName, "error"))
                                   .Serialization(s => s.UseJsonSerializer())
                                   .MessageOwnership(d => d.Use(this))
                                   .Subscriptions(s => s.StoreInMemory())
                                   .CreateBus()
                                   .Start();

                bus.Subscribe<Person>();

                Thread.Sleep(1000);

                using (var tx = new System.Transactions.TransactionScope())
                {
                    bus.Publish(new Person { Name = "Anders (Publish)" });
                    bus.Send(new Person { Name = "Anders (Send)" });

                    Thread.Sleep(1000);

                   // tx.Complete();
                }
            }

            PrintPerson.ReceivedPeople.ShouldBe(0);
        }


        class PrintPerson : IHandleMessages<Person>
        {
            public static int ReceivedPeople;

            public void Handle(Person person)
            {
                ReceivedPeople++;
                Console.WriteLine("Name: {0}", person.Name);
            }
        }

        class Person
        {
            public string Name { get; set; }
        }
    }
}