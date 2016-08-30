using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Tests;
using Rebus.Tests.Contracts;

namespace Rebus.AmazonSQS.Tests
{
    [TestFixture, Category(Category.AmazonSqs)]
    public class AmazonSqsVisibiltyTimeout : SqsFixtureBase
    {
        [Test]
        public async void WhenMessageVisibilityIsRenewed_ThenItsNotVisibleForOthers()
        {
            //arrange
            var peeklockDuration = TimeSpan.FromSeconds(3);

            var transportFactory = new AmazonSqsTransportFactory();
            
            var inputqueueName = TestConfig.QueueName("inputQueue");
            var inputQueue = transportFactory.Create(inputqueueName, peeklockDuration);

            var inputqueueName2 = TestConfig.QueueName("outputQueue");
            var outputQueue = transportFactory.Create(inputqueueName2);

            await WithContext(async context =>
            {
                await outputQueue.Send(inputqueueName, MessageWith("hej"), context);
            });

            var cancellationToken = new CancellationTokenSource().Token;

            await WithContext(async context =>
            {
                var transportMessage = await inputQueue.Receive(context, cancellationToken);

                Assert.That(transportMessage, Is.Not.Null, "Expected to receive the message that we just sent");

                // pretend that it takes a while to handle the message
                Thread.Sleep(6000);

                // pretend that another thread attempts to receive from the same input queue
                await WithContext(async innerContext =>
                {
                    var innerMessage = await inputQueue.Receive(innerContext, cancellationToken);

                    Assert.That(innerMessage, Is.Null, "Did not expect to receive a message here because its peek lock should have been renewed automatically");
                });
            });
        }
    }
}