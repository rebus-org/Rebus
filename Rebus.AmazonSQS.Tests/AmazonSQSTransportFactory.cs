using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Amazon;
using Rebus.Extensions;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.AmazonSQS.Tests
{
    public class AmazonSQSTransportFactory : ITransportFactory
    {
        public string BaseUrl
        {
            get { return ConnectionInfo.BaseQueueUrl; }
        }

        private static ConnectionInfo _connectionInfo = null;

        internal static ConnectionInfo ConnectionInfo
        {
            get
            {
                return _connectionInfo ?? (_connectionInfo = ConnectionInfoFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "sqs_connectionstring.txt"))
                                                             ?? ConnectionInfoFromEnvironmentVariable("rebus2_asqs_connection_string")
                                                             ?? Throw("Could not find Amazon Sqs connetion Info!"));
            }
        }


        public ITransport Create(string inputQueueAddress, TimeSpan peeklockDuration)
        {


            return _queuesToDelete.GetOrAdd(inputQueueAddress, () =>
            {

                var transport = new AmazonSqsTransport(inputQueueAddress, ConnectionInfo.AccessKeyId, ConnectionInfo.SecretAccessKey, RegionEndpoint.GetBySystemName(ConnectionInfo.RegionEndpoint));

                transport.Initialize(peeklockDuration);
                transport.Purge();
                return transport;
            });

        }

        public ITransport Create(string inputQueueAddress)
        {
            return Create(inputQueueAddress, TimeSpan.FromSeconds(30));
        }


        readonly Dictionary<string, AmazonSqsTransport> _queuesToDelete = new Dictionary<string, AmazonSqsTransport>();


        public void CleanUp()
        {
            CleanUp(false);


        }

        public void CleanUp(bool deleteQueues)
        {
            if (deleteQueues)
            {
                foreach (var queueAndTransport in _queuesToDelete)
                {

                    var transport = queueAndTransport.Value;

                    transport.DeleteQueue();

                }
            }
        }


        static ConnectionInfo ConnectionInfoFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
            {
                Console.WriteLine("Could not find env variable {0}", environmentVariableName);
                return null;
            }

            Console.WriteLine("Using AmazonSqs connection info from env variable {0}", environmentVariableName);

            return ConnectionInfo.CreateFromString(value);
        }

        static ConnectionInfo ConnectionInfoFromFileOrNull(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not find file {0}", filePath);
                return null;
            }

            Console.WriteLine("Using Amazon SQS connectionInfo string from file {0}", filePath);
            return ConnectionInfo.CreateFromString(File.ReadAllText(filePath));
        }
        static ConnectionInfo Throw(string message)
        {
            throw new ConfigurationErrorsException(message);
        }


    }
    internal class ConnectionInfo
    {
        internal string AccessKeyId;
        internal string SecretAccessKey;
        internal string BaseQueueUrl;
        internal string RegionEndpoint;
        /// <summary>
        /// Expects format Key=Value¤Key=Value¤Key=Value
        /// Ie. AccessKeyId=xxxxx¤SecretAccessKey=yyyy¤BaseQueueUrl=asdasdas¤RegionEndpoint=asdasd
        /// </summary>
        /// <param name="textString"></param>
        /// <returns></returns>
        internal static ConnectionInfo CreateFromString(string textString)
        {
            Console.WriteLine("Parsing connectionInfo from string:{0}", textString);
            var keyValuePairs = textString.Split('¤');
            Console.WriteLine("Found {0} pairs. Expected 4", keyValuePairs.Length);
            try
            {
                var keysAndValues = keyValuePairs.ToDictionary((kv) => kv.Split('=')[0], (kv) => kv.Split('=')[1]);
                return new ConnectionInfo()
                {
                    AccessKeyId = keysAndValues["AccessKeyId"],
                    SecretAccessKey = keysAndValues["SecretAccessKey"],
                    BaseQueueUrl = keysAndValues["BaseQueueUrl"].EndsWith("/") ? keysAndValues["BaseQueueUrl"] : keysAndValues["BaseQueueUrl"] + "/",
                    RegionEndpoint = keysAndValues["RegionEndpoint"]
                };

            }
            catch (Exception exception)
            {

                Console.WriteLine("Could not extract keys and values from textstring. Ensure that the key and values are split by = \nand that the Key values used are: AccessKeyId, SecretAccessKey and BaseQueueUrl");
                Console.WriteLine("\nException message: {0}", exception.Message);

                throw;
            }
        }

    }
}