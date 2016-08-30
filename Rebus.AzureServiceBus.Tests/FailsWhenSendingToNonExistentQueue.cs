using System;
using System.Linq;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AzureServiceBus.Tests.Factories;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Contracts;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture]
    public class FailsWhenSendingToNonExistentQueue : FixtureBase
    {
        static readonly string ConnectionString = StandardAzureServiceBusTransportFactory.ConnectionString;

        [Test]
        public void YesItDoes()
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            Configure.With(activator)
                .Transport(t => t.UseAzureServiceBus(ConnectionString, "bimmelim"))
                .Start();

            var exception = Assert.Throws<AggregateException>(() =>
            {
                activator.Bus.Advanced.Routing.Send("yunoexist", "hej med dig min ven!").Wait();
            });

            var notFoundException = exception.InnerExceptions
                .OfType<MessagingException>()
                .Single();

            Console.WriteLine(notFoundException);

            var bimse = notFoundException.ToString();


        }

        [Test]
        public void ExceptionsWithOverriddenToString()
        {
            var typesWithOverriddenToStringMethod = typeof(MessagingException).Assembly.GetTypes()
                .Where(typeof (MessagingException).IsAssignableFrom)
                .Where(exceptionType => exceptionType.GetMethod("ToString", new Type[0]).DeclaringType == exceptionType)
                .ToList();

            Console.WriteLine($@"Here they are:

{string.Join(Environment.NewLine, typesWithOverriddenToStringMethod)}");
        }
    }
}