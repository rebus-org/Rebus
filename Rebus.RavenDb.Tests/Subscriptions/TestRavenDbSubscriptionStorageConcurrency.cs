using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Tests;
using Rebus.Tests.Contracts;

namespace Rebus.RavenDb.Tests.Subscriptions
{
    [TestFixture]
    public class TestRavenDbSubscriptionStorageConcurrency : FixtureBase
    {
        RavenDbSubscriptionStorageFactory _factory;

        protected override void SetUp()
        {
            _factory = new RavenDbSubscriptionStorageFactory();
        }

        protected override void TearDown()
        {
            _factory.Cleanup();
        }

        [Test]
        public void HandlesConcurrencyExceptionsWell()
        {
            const string contentedTopic = "contention!!";

            var subscriptionStorage = _factory.Create();

            var caughtException = false;

            var threads = Enumerable.Range(0, 10)
                .Select(i => new Thread(() =>
                {
                    try
                    {
                        subscriptionStorage.RegisterSubscriber(contentedTopic, $"sub{i}").Wait();
                        Thread.Sleep(5);
                        subscriptionStorage.UnregisterSubscriber(contentedTopic, $"sub{i}").Wait();
                        Thread.Sleep(3);
                        subscriptionStorage.RegisterSubscriber(contentedTopic, $"sub{i}").Wait();
                        Thread.Sleep(2);
                        subscriptionStorage.UnregisterSubscriber(contentedTopic, $"sub{i}").Wait();
                        Thread.Sleep(7);
                        subscriptionStorage.RegisterSubscriber(contentedTopic, $"sub{i}").Wait();
                        Thread.Sleep(1);
                        subscriptionStorage.UnregisterSubscriber(contentedTopic, $"sub{i}").Wait();
                        Thread.Sleep(1);
                        subscriptionStorage.RegisterSubscriber(contentedTopic, $"sub{i}").Wait();
                        Thread.Sleep(1);
                        subscriptionStorage.UnregisterSubscriber(contentedTopic, $"sub{i}").Wait();
                        Thread.Sleep(1);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        caughtException = true;
                    }
                }))
                .ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            Assert.That(caughtException, Is.False);
        }
    }
}