using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Transport;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSqsVisibiltyTimeout : SqsFixtureBase
    {

        [Test]
        public async void WhenMessageVisibilityIsRenewed_ThenItsNotVisibleForOthers()
        {
            //arrange

            var transportFactory = new AmazonSQSTransportFactory();

            const string inputqueueName = "inputQueue";
            var inputQueue = transportFactory.Create(TestConfig.QueueName(inputqueueName), TimeSpan.FromSeconds(3));
            const string inputqueueName2 = "outputQueue";
            var outputQueue = transportFactory.Create(TestConfig.QueueName(inputqueueName2));

            await WithContext(async context =>
                                    {
                                        await outputQueue.Send(inputqueueName, MessageWith("hej"), context);
                                    });

            await WithContext(async context =>
                                    {
                                        var transportMessage = await inputQueue.Receive(context);


                                        await WithContext(async innerContext =>
                                                                {
                                                                    Thread.Sleep(6000);
                                                                    var innerMessage = await inputQueue.Receive(innerContext);

                                                                    Assert.That(innerMessage, Is.Null);
                                                                });


                                        Assert.That(transportMessage, Is.Not.Null);

                                    });

        }



       
    }
}