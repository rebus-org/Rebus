using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSqsSimpleSend : BasicSendReceive<AmazonSQSTransportFactory> { }

    public class AmazonSqsMessageExpiration : MessageExpiration<AmazonSQSTransportFactory> { }



    public class AmazonSqsVisibiltyTimeout : FixtureBase
    {

        [Test]
        public async void WhenMessageVisibilityIsRenewed_ThenItsNotVisibleForOthers()
        {
            //arrange

            var transportFactory = new AmazonSQSTransportFactory();

            const string inputqueueName = "inputQueue";
            var inputQueue = transportFactory.Create(TestConfig.QueueName(inputqueueName), 3);//3 seconds timeout on queue should be overrided by renewalcode
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
                                            Thread.Sleep(5000);
                                            var innerMessage = await inputQueue.Receive(innerContext);

                                            Assert.That(innerMessage,Is.Null);
                                        });


                Assert.That(transportMessage,Is.Not.Null);

            });

        }



        readonly Encoding _defaultEncoding = Encoding.UTF8;


        async Task WithContext(Func<ITransactionContext, Task> contextAction, bool completeTransaction = true)
        {
            using (var context = new DefaultTransactionContext())
            {
                await contextAction(context);

                if (completeTransaction)
                {
                    await context.Complete();
                }
            }
        }

        string GetStringBody(TransportMessage transportMessage)
        {
            if (transportMessage == null)
            {
                throw new InvalidOperationException("Cannot get string body out of null message!");
            }

            return _defaultEncoding.GetString(transportMessage.Body);
        }

        TransportMessage MessageWith(string stringBody)
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()}
            };
            var body = _defaultEncoding.GetBytes(stringBody);
            return new TransportMessage(headers, body);
        }
    }

}
