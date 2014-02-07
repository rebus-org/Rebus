using System;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NUnit.Framework;
using Rebus.Tests.Contracts.Transports.Factories;

namespace Rebus.Tests.Transports.Azure
{
    [TestFixture, Category(TestCategories.Azure)]
    public class TestAzureServiceBusMessageQueueFactory
    {
        const string SubscriptionName1 = "Sub1";
        const string SubscriptionName2 = "Sub2";
        const string TopicName = "Rebus";
        
        readonly string connectionString = AzureServiceBusMessageQueueFactory.ConnectionString;

        [Test]
        public void CanConnect()
        {
            // arrange
            Console.WriteLine("Creating namespace manager");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            try
            {
                Console.WriteLine("Deleting topic {0}", TopicName);
                namespaceManager.DeleteTopic(TopicName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Creating topic {0}", TopicName);
            var topicDescription = namespaceManager.GetOrCreateTopic(TopicName);

            Console.WriteLine("Creating subscription {0}", SubscriptionName1);
            namespaceManager.GetOrCreateSubscription(topicDescription, SubscriptionName1);

            Console.WriteLine("Creating subscription {0}", SubscriptionName2);
            namespaceManager.GetOrCreateSubscription(topicDescription, SubscriptionName2);

            // act
            Console.WriteLine("Creating topic client");
            var topicClient = TopicClient.CreateFromConnectionString(connectionString, topicDescription.Path);
            topicClient.Send(CreateMessage("hello world! 1", SubscriptionName1));
            topicClient.Send(CreateMessage("hello world! 2", SubscriptionName2));

            Console.WriteLine("Creating subscription clients");
            var sub1client = SubscriptionClient.CreateFromConnectionString(connectionString, TopicName, SubscriptionName1);
            var sub2client = SubscriptionClient.CreateFromConnectionString(connectionString, TopicName, SubscriptionName2);

            var envelope1 = Encoding.UTF8.GetString(sub1client.Receive().GetBody<Envelope>().Body);
            var envelope2 = Encoding.UTF8.GetString(sub2client.Receive().GetBody<Envelope>().Body);

            // assert

            Console.WriteLine(envelope1);
            Console.WriteLine(envelope2);
        }

        static BrokeredMessage CreateMessage(string text, string destinationQueue)
        {
            var message = new BrokeredMessage(new Envelope { Body = Encoding.UTF8.GetBytes(text) });
            message.Properties["LogicalDestinationQueue"] = destinationQueue;
            return message;
        }

        [DataContract]
        class Envelope
        {
            [DataMember]
            public byte[] Body { get; set; }
        }
    }

    public static class NamespaceManagerExtensions
    {
        public static TopicDescription GetOrCreateTopic(this NamespaceManager namespaceManager, string name)
        {
            if (!namespaceManager.TopicExists(name))
            {
                try
                {
                    return namespaceManager.CreateTopic(name);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                }
            }

            return namespaceManager.GetTopic(name);
        }

        public static SubscriptionDescription GetOrCreateSubscription(this NamespaceManager namespaceManager, TopicDescription topicDescription,
                                                                      string name)
        {
            if (!namespaceManager.SubscriptionExists(topicDescription.Path, name))
            {
                try
                {
                    var filter = new SqlFilter(string.Format("LogicalDestinationQueue = '{0}'", name));
                    var subscription = namespaceManager.CreateSubscription(topicDescription.Path, name, filter);
                    return subscription;
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                }
            }

            return namespaceManager.GetSubscription(topicDescription.Path, name);
        }
    }
}