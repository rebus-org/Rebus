using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class PolymorphicDoubleDispatchVerification : FixtureBase
    {
        const string InputQueueName = "test.polymorphic.double.dispatch";
        const string ErrorQueueName = "error";

        static readonly List<Type> Invocations = new List<Type>();
        protected override void DoSetUp()
        {
            Invocations.Clear();
        }

        [Test]
        public void TheTest()
        {
            var adapter = TrackDisposable(new BuiltinContainerAdapter());
            
            adapter.Register(typeof(SimpleMessageHandler));
            adapter.Register(typeof(UserContextHandler));

            var bus = Configure.With(adapter)
                .Logging(l => l.ColoredConsole(LogLevel.Warn))
                .Transport(t => t.UseMsmq(InputQueueName, ErrorQueueName))
                .SpecifyOrderOfHandlers(o => o.First<UserContextHandler>())

                .CreateBus()
                .Start();

            bus.SendLocal(new SimpleMessage
            {
                Data = Guid.NewGuid().ToString(),
                UserContext = new UserContext {UserId = 23}
            });

            Thread.Sleep(2.Seconds());

            Assert.That(Invocations.Count, Is.EqualTo(2));
            Assert.That(Invocations[0], Is.EqualTo(typeof(UserContextHandler)));
            Assert.That(Invocations[1], Is.EqualTo(typeof(SimpleMessageHandler)));
        }

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();

            MsmqUtil.Delete(InputQueueName);
            MsmqUtil.Delete(ErrorQueueName);
        }

        public class UserContext
        {
            public string Name { get; set; }

            public int UserId { get; set; }

            public Guid AppId { get; set; }
        }

        public class UserContextMessageBase
        {
            public UserContext UserContext { get; set; }
        }

        public class SimpleMessage : UserContextMessageBase
        {
            public string Data { get; set; }
        }


        internal class UserContextHandler : IHandleMessages<UserContextMessageBase>
        {
            public const string UserContextKey = "current-user-context";

            public void Handle(UserContextMessageBase message)
            {
                Invocations.Add(GetType());
                
                Console.WriteLine("Processing UserContextMessageBase");

                MessageContext.GetCurrent().Items[UserContextKey] = message.UserContext;
            }
        }

        internal class SimpleMessageHandler : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
                Invocations.Add(GetType());

                var userContext = (UserContext)MessageContext.GetCurrent().Items[UserContextHandler.UserContextKey];

                // allow to use the _context to process this message
                Console.WriteLine("Received SimpleMessage {0} - context: {1}", message.Data, userContext.UserId);
            }
        }
    }
}