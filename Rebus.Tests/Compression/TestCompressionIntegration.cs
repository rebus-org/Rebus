using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Compression;
using Rebus.Config;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Compression
{
    [TestFixture]
    public class TestCompressionIntegration : FixtureBase
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ItWorksWithString(bool withCompressionEnabled)
        {
            var activator = new BuiltinHandlerActivator();
            var gotIt = new ManualResetEvent(false);

            activator.Handle<string>(async str =>
            {
                if (string.Equals(str, LongText))
                {
                    gotIt.Set();
                }
                else
                {
                    throw new Exception(string.Format("Received text with {0} chars did not match expected text with {1} chars!",
                        str.Length, LongText.Length));
                }
            });

            Using(activator);

            var bus = CreateBus(withCompressionEnabled, activator);

            bus.SendLocal(LongText).Wait();

            gotIt.WaitOrDie(TimeSpan.FromSeconds(10));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ItWorksWithComplexMessage(bool withCompressionEnabled)
        {
            var activator = new BuiltinHandlerActivator();
            var gotIt = new ManualResetEvent(false);

            activator.Handle<TextMessage>(async str =>
            {
                if (string.Equals(str.Text, LongText))
                {
                    gotIt.Set();
                }
                else
                {
                    throw new Exception(string.Format("Received text with {0} chars did not match expected text with {1} chars!",
                        str.Text.Length, LongText.Length));
                }
            });

            Using(activator);

            var bus = CreateBus(withCompressionEnabled, activator);

            bus.SendLocal(new TextMessage {Text = LongText}).Wait();

            gotIt.WaitOrDie(TimeSpan.FromSeconds(10));
        }

        static IBus CreateBus(bool withCompressionEnabled, BuiltinHandlerActivator activator)
        {
            var bus = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "compressor"))
                .Options(o =>
                {
                    o.LogPipeline();

                    if (withCompressionEnabled)
                    {
                        o.EnableCompression(128);
                    }
                })
                .Start();
            return bus;
        }

        class TextMessage
        {
            public string Text { get; set; }
        }

        const string LongText = @"hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....

hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....

hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....

hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....
hooloo boolooo hvasså der lang tekst mange gentagelser helt sikker over 128 bytes....";
    }
}