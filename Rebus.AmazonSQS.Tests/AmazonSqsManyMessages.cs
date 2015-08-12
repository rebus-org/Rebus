using NUnit.Framework;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.AmazonSQS.Tests
{
    [TestFixture, Category(Category.AmazonSqs)]
    public class AmazonSqsManyMessages : TestManyMessages<AmazonSQSManyMessagesTransportFactory>
    {

    }
}