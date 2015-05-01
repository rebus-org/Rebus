using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSqsSimpleSend : BasicSendReceive<AmazonSQSTransportFactory> { }

    public class AmazonSqsMessageExpiration : MessageExpiration<AmazonSQSTransportFactory> { }

}
