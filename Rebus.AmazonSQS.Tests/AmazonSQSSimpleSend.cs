using System.Linq;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSqsSimpleSend : BasicSendReceive<AmazonSQSTransportFactory> { }
}
