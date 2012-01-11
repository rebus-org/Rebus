using System;
using System.Diagnostics;
using System.Messaging;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Rebus.Tests
{
    [TestFixture]
    public class TestStuff
    {
        [TestCase(1000)]
        public void CompareTracePerformance(int iterations)
        {
            PrintTiming("Trace", () => iterations.Times(() => Trace.TraceInformation("HWLLO WRRLD!")));
            PrintTiming("Console.WriteLine", () => iterations.Times(() => Console.WriteLine("HWLLO WRRLD!")));
        }

        void PrintTiming(string what, Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            var elapsed = stopwatch.Elapsed;
            Console.WriteLine("{0} took {1:0.0} s", what, elapsed.TotalSeconds);
        }

        [Test]
        public void InvokeViaReflectionWorksLikeExpected()
        {
            var instance = new SomeClass();

            typeof(SomeClass).GetMethod("Handle", new[] { typeof(string) }).Invoke(instance, new object[] { "yo!" });
            typeof(SomeClass).GetMethod("Handle", new[] { typeof(object) }).Invoke(instance, new object[] { "yo!" });

            instance.StringCalled.ShouldBe(true);
            instance.ObjectCalled.ShouldBe(true);
        }

        class SomeClass : IHandleMessages<string>, IHandleMessages<object>
        {
            public bool StringCalled { get; set; }
            public bool ObjectCalled { get; set; }

            public void Handle(string message)
            {
                StringCalled = true;
            }

            public void Handle(object message)
            {
                ObjectCalled = true;
            }
        }

        [Test]
        public void StatementOfFunctionality()
        {
            var messageQueue = GetOrCreate("test.headers");
            messageQueue.Purge();

            var tx = new MessageQueueTransaction();
            tx.Begin();
            messageQueue.Send("this is just some random message", "THIS IS THE LABEL", tx);
            tx.Commit();
        }

        MessageQueue GetOrCreate(string name)
        {
            var path = string.Format(@".\private$\{0}", name);

            if (MessageQueue.Exists(path))
            {
                return new MessageQueue(path);
            }

            var messageQueue = MessageQueue.Create(path, true);
            messageQueue.SetPermissions(Thread.CurrentPrincipal.Identity.Name, MessageQueueAccessRights.FullControl);
            return messageQueue;
        }
    }
}