using System;
using System.Collections.Generic;
using Amazon;
using Amazon.EC2.Model;
using Amazon.SQS;
using Rebus.Extensions;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSQSTransportFactory : ITransportFactory
    {
        private string accessKeyId = "AKIAIFLMHSFJHKP5US5Q";
        private const string secretAccessKey = "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";
        private const string baseQueueUrl = "https://sqs.eu-central-1.amazonaws.com/706962889542/";

        private string _inputAddress;


        public ITransport Create(string inputQueueAddress, int visibiltyTimeout)
        {

            _inputAddress = StripPrefixSlash(inputQueueAddress);
            return _queuesToDelete.GetOrAdd(_inputAddress, () =>
            {

                var transport = new AmazonSqsTransport(_inputAddress, accessKeyId, secretAccessKey, baseQueueUrl, RegionEndpoint.EUCentral1);

                transport.Initialize(visibiltyTimeout);
                transport.Purge();
                return transport;
            });

        }

        public ITransport Create(string inputQuquAddress)
        {
            return Create(inputQuquAddress, 30);
        }
        readonly Dictionary<string, AmazonSqsTransport> _queuesToDelete = new Dictionary<string, AmazonSqsTransport>();
        private string StripPrefixSlash(string inputQueueAddress)
        {
            if (inputQueueAddress.StartsWith("/"))
                return inputQueueAddress.Substring(1);

            return inputQueueAddress;
        }

        public void CleanUp()
        {




        }
    }
}