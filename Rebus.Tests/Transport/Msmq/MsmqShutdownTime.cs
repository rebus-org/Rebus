using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Transport.Msmq
{
    [TestFixture]
    public class MsmqShutdownTime : FixtureBase
    {
        [Test]
        public void CheckReceiveTimeout()
        {
            var stopwatch = new Stopwatch();

            using (var activator = new BuiltinHandlerActivator())
            {
                Configure.With(activator)
                    .Transport(t => t.UseMsmq(TestConfig.GetName("receive-timeout")))
                    .Start();

                Thread.Sleep(1000);

                stopwatch.Start();
            }

            stopwatch.Stop();
            var shutdownTime = stopwatch.Elapsed;

            Console.WriteLine($"Shutdown took {shutdownTime}");
        }
    }
}