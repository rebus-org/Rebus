using System;
using System.Reflection;
using NUnit.Framework;
using System.Linq;

namespace Rebus.Tests.Analysis
{
    [TestFixture]
    public class TestExceptions
    {
        [Test]
        public void AllDeclaredExceptionsAreSerializable()
        {
            var exceptionTypes = new[]
                                     {
                                         "Rebus",

                                         //containers
                                         "Rebus.Castle.Windsor", "Rebus.Unity", "Rebus.Autofac", "Rebus.StructureMap", "Rebus.Ninject", "Rebus.SimpleInjector",

                                         //logging
                                         "Rebus.NLog", "Rebus.Log4Net", "Rebus.Serilog",

                                         //transports
                                         "Rebus.RabbitMq", "Rebus.Azure", "Rebus.AzureServiceBus",

                                         //persistence
                                         "Rebus.RavenDb", "Rebus.MongoDb", "Rebus.PostgreSql",

                                         //other stuff
                                         "Rebus.Timeout", "Rebus.HttpGateway",
                                     }
                .Select(Assembly.Load)
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof (Exception).IsAssignableFrom(t));

            var badExceptions = exceptionTypes.Select(Evaluate)
                                              .Where(e => !e.IsGood)
                                              .ToList();

            if (badExceptions.Any())
            {
                Assert.Fail(@"The following declared exception types don't comply with Rebus exceptional rules:

{0}", string.Join(Environment.NewLine + Environment.NewLine, badExceptions.Select(b => string.Format(@"--------------------------------
{0}
--------------------------------
{1}",b.ExceptionType,  b.Message))));
            }
        }

        ExceptionEvalation Evaluate(Type exceptionType)
        {
            if (!exceptionType.IsSerializable)
                return ExceptionEvalation.Bad(exceptionType, "Is not serializable");

            if (!typeof (ApplicationException).IsAssignableFrom(exceptionType))
                return ExceptionEvalation.Bad(exceptionType, "Is not derived off of ApplicationException");

            return ExceptionEvalation.Good(exceptionType);
        }

        class ExceptionEvalation
        {
            ExceptionEvalation(){}

            public bool IsGood { get; private set; }
            public string Message { get; private set; }
            public Type ExceptionType { get; set; }

            public static ExceptionEvalation Bad(Type exceptionType, string whatWasWrong, params object[] objs)
            {
                return new ExceptionEvalation { IsGood = false, Message = string.Format(whatWasWrong, objs), ExceptionType = exceptionType };
            }

            public static ExceptionEvalation Good(Type exceptionType)
            {
                return new ExceptionEvalation { IsGood = true, ExceptionType = exceptionType };
            }
        }
    }
}