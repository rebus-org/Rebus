using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Rebus.Tests
{
    [TestFixture]
    public class TestGeneralMsmq
    {
        string queueName = @".\private$\test";

        [SetUp]
        public void SetUp()
        {
            if (!MessageQueue.Exists(queueName))
            {
                MessageQueue.Create(queueName, transactional: true);
            }
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void CanSendAndReceiveMessagesWithMsmq()
        {
            var receivedMessages = new List<string>();

            Task.Factory.StartNew(() =>
                                      {
                                          using (var messageQueue = new MessageQueue(queueName))
                                          {
                                              for (var counter = 0; counter < 10; counter++)
                                              {
                                                      messageQueue.Send("helloooo!");
                                                      Console.WriteLine("Sent hello...");
                                              }
                                          }
                                      });

            Task.Factory.StartNew(() =>
                                      {
                                          using (var messageQueue = new MessageQueue(queueName))
                                          {
                                              messageQueue.ReceiveCompleted += (o, ea) =>
                                                                                   {
                                                                                       try
                                                                                       {
                                                                                           var body = (string) ea.Message.Body;
                                                                                           Console.WriteLine("Received {0}", body);
                                                                                           receivedMessages.Add(body);
                                                                                       }
                                                                                           catch(Exception e)
                                                                                           {
                                                                                               Console.WriteLine(e);
                                                                                           }
                                                                                       finally
                                                                                       {
                                                                                           messageQueue.BeginReceive();
                                                                                       }
                                                                                   };
                                              messageQueue.BeginReceive();
                                          }
                                      });

            Thread.Sleep(1000);

            receivedMessages.Count.ShouldBe(10);
        }
    }
}