using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Messaging;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Rebus.Tests
{
    [TestFixture]
    public class TestMsmq
    {
        [Test]
        public void CanSendAndReceive()
        {
            var queueName = @".\private$\test3";

            TakeTime(() => 1.Times(() =>
                                          {
                                              using (var queue = new Msmq(queueName))
                                              {
                                                  2000.Times(() => queue.Send("hello there!"));
                                              }
                                          }));

            TakeTime(() => 1.Times(() =>
                                          {
                                              using (var queue = new Msmq(queueName))
                                              {
                                                  2000.Times(() =>
                                                                 {
                                                                     var message = (string)queue.Receive();
                                                                     message.ShouldBe("hello there!");
                                                                 });
                                              }
                                          }));
        }

        [Test]
        public void StatementOfFunctionality()
        {
            var queueName = @".\private$\test5";

            using (var sender = new Msmq(queueName))
            {
                2.Times(() => sender.Send("new msg"));
            }

            Thread.Sleep(1000);

                            var messageQueue = MessageQueue.Exists(queueName)
                                   ? new MessageQueue(queueName)
                                   : MessageQueue.Create(queueName, transactional: true);

                messageQueue.Formatter = new XmlMessageFormatter(new[] { typeof(string) });


                using (var receiver = new Msmq.Receiver(messageQueue))
            {
                var objects = new List<object>();
                receiver.Receive(objects.Add);
                receiver.Receive(objects.Add);
                receiver.Receive(objects.Add);

                objects.Count.ShouldBe(2);
            }
        }

        void TakeTime(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            Console.WriteLine("Elapsed: {0:0.## s}", stopwatch.Elapsed.TotalSeconds);
        }
    }

    public static class IntExtensions
    {
        public static void Times(this int count, Action action)
        {
            for (var counter = 0; counter < count; counter++)
            {
                action();
            }
        }
    }
}