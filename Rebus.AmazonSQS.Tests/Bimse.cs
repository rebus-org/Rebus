using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using NUnit.Framework;

namespace Rebus.AmazonSQS.Tests
{
    [TestFixture]
    public class Bimse
    {
        [Test]
        public async Task NizzleName()
        {
            var credentials = new BasicAWSCredentials("AKIAI6CUQBQYYT7COZ3Q", "m8bkPaoJ6CGia9GPQ6LjjwlE9eNuwpRynZtaErdx");
            var client = new AmazonSQSClient(credentials, new AmazonSQSConfig {ServiceURL = "https://sqs.eu-central-1.amazonaws.com" });

            await client.CreateQueueAsync(new CreateQueueRequest("myqueue"));
        }
    }
}