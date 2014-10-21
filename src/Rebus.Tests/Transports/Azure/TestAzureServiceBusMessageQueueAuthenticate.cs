using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;
using Rebus.AzureServiceBus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports.Factories;

namespace Rebus.Tests.Transports.Azure
{
    [TestFixture, Category(TestCategories.Azure)]
    public class TestAzureServiceBusMessageQueuesWhenUsingAccessPolicies : FixtureBase
    {
        BuiltinContainerAdapter azureAdapter;
        const string inputQueueName = "MyInputQueue";
        const string errorQueueName = "MyErrorQueue";
        const string queueAName = "queueA";
        const string queueBName = "queueB";

        string queueAConnectionString = string.Empty;
        string queueBConnectionString = string.Empty;
        string queueErrorConnctionString=String.Empty;
        Dictionary<string, string> queueCredentials;
        string namespaceConnectionString;


        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Warn };

            namespaceConnectionString = AzureServiceBusMessageQueueFactory.ConnectionString;

            var manager = NamespaceManager.CreateFromConnectionString(namespaceConnectionString);

            queueAConnectionString = CreateQueueIfNotExistsAndReturnConnectionString(manager, namespaceConnectionString, queueAName, new[] { AccessRights.Send });
            queueBConnectionString = CreateQueueIfNotExistsAndReturnConnectionString(manager, namespaceConnectionString, queueBName, new[] { AccessRights.Send });
            queueErrorConnctionString = CreateQueueIfNotExistsAndReturnConnectionString(manager, namespaceConnectionString, errorQueueName, new[] { AccessRights.Send,AccessRights.Listen,AccessRights.Manage });


            queueCredentials = new Dictionary<string, string>{{queueAName,queueAConnectionString},
                                                                  {queueBName,queueBConnectionString},
                                                                  {errorQueueName,queueErrorConnctionString}};

            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Warn };
            azureAdapter = NewAdapter();
            


        }
        BuiltinContainerAdapter NewAdapter()
        {
            return TrackDisposable(new BuiltinContainerAdapter());
        }

        protected string CreateQueueIfNotExistsAndReturnConnectionString(NamespaceManager manager,string connectionString, string queueName, IEnumerable<AccessRights> rights)
        {
            Console.WriteLine("creating queue \"{0}\" with shared access rules",queueName);
            if (manager.QueueExists(queueName))
                manager.DeleteQueue(queueName);

            //var queueB = manager.CreateQueue(queueBName);
            var queue = new QueueDescription(queueName);
            queue.Authorization.Add(new SharedAccessAuthorizationRule("KeyName", rights));
            var queueBToken = ((SharedAccessAuthorizationRule)queue.Authorization.First()).PrimaryKey;
            var cn = new ServiceBusConnectionStringBuilder(connectionString)
            {
                SharedAccessKeyName = "KeyName",
                SharedAccessKey = queueBToken
            }.ToString();

            manager.CreateQueue(queue);
            return cn;
        }

        [Test]
        public void ItShouldBePossibleToSendAllQueuesWhenSendingFromTheNamespaceConnectionString()
        {
            Configure.With(azureAdapter)
                     .Transport(t => t.UseAzureServiceBus(namespaceConnectionString, inputQueueName, errorQueueName))
                     .CreateBus()
                     .Start();
            azureAdapter.Bus.Advanced.Routing.Send(queueAName, "HelloQueueA");
            azureAdapter.Bus.Advanced.Routing.Send(queueBName, "HelloQueueA");
        }

        [Test]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void ItShouldNOTBePossibleToSendToAllQueuesWhenSendingFromASingleQueueConnectionString()
        {
            //Setup bus to listen with connectionstring to the inputqueue (not the namespace)
            var inputQeueConnectonString=CreateQueueIfNotExistsAndReturnConnectionString(
                NamespaceManager.CreateFromConnectionString(namespaceConnectionString), namespaceConnectionString,
                "myinputquee", new[] { AccessRights.Send,AccessRights.Manage,AccessRights.Listen });
            
            //Not provide any extra credentials
            Configure.With(azureAdapter)
                     .Transport(t => t.UseAzureServiceBus(inputQeueConnectonString, "myinputquee", errorQueueName))
                     .CreateBus()
                     .Start();

            //Try to send to a queue which should not be possible to send to
            azureAdapter.Bus.Advanced.Routing.Send(queueAName, "HelloQueueA");
            
        }


        [Test]
        public void ItShouldBePossibleToSendToAllQueueWhenSendingFromSingleQueueConnectionstringAsLongAsCredentialsAreProvided()
        {
            //Setup bus to listen with connectionstring to the inputqueue (not the namespace)
            var inputQeueConnectonString = CreateQueueIfNotExistsAndReturnConnectionString(
                NamespaceManager.CreateFromConnectionString(namespaceConnectionString), namespaceConnectionString,
                "myinputquee", new[] { AccessRights.Send, AccessRights.Manage, AccessRights.Listen });
            
            
            //Provide credentials for each queue the bus is going to send to
            Configure.With(azureAdapter)
                     .Transport(t => t.UseAzureServiceBusWithCredentialsForEachQueue(inputQeueConnectonString, "myinputquee", errorQueueName,queueCredentials))
                     .CreateBus()
                     .Start();

            //Try to send to a queue which should not be possible to send to
            azureAdapter.Bus.Advanced.Routing.Send(queueAName, "HelloQueueA");
            azureAdapter.Bus.Advanced.Routing.Send(queueBName, "HelloQueueB");
            azureAdapter.Bus.Advanced.Routing.Send(errorQueueName, "HelloErrorQueue");

        }
    }
}
