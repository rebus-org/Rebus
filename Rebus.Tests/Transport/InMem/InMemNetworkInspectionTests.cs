using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Transport.InMem;

[TestFixture]
public class InMemNetworkInspectionTests
{
    private InMemNetwork _inMemNetwork;

    [SetUp]
    public void SetUp()
    {
        _inMemNetwork = new InMemNetwork();
        _inMemNetwork.CreateQueue("input");
    }

    [Test]
    public void Queues_NoQueues_ReturnsEmpty()
    {
        Assert.That(new InMemNetwork().Queues, Is.Empty);
    }

    [Test]
    public void Queues_ReturnsCreatedQueues()
    {
        _inMemNetwork.CreateQueue("test");
        Assert.That(_inMemNetwork.Queues, Is.EquivalentTo(new[] {"input", "test"}));
    }
        
    [Test]
    public void GetMessages_NonExistingQueue_ReturnsEmptyList()
    {
        var messages = _inMemNetwork.GetMessages("non_existing");
        Assert.That(messages, Is.Empty);
    }

    [Test]
    public void GetMessages_ExistingEmptyQueue_ReturnsEmptyList()
    {
        var messages = _inMemNetwork.GetMessages("input");
        Assert.That(messages, Is.Empty);
    }
        
    [Test]
    public void GetMessages_QueueWithMessages_ReturnsMessages()
    {
        _inMemNetwork.Deliver("input", CreateMessage("Hello"));
        _inMemNetwork.Deliver("input", CreateMessage("World"));

        var messages = _inMemNetwork.GetMessages("input");
        Assert.That(messages.Count, Is.EqualTo(2));
        AssertMessage(messages[0], "Hello");
        AssertMessage(messages[1], "World");
    }
        
    [Test]
    public void GetMessages_QueueWithMessages_DoesNotReturnConsumedMessages()
    {
        _inMemNetwork.Deliver("input", CreateMessage("Hello"));
        _inMemNetwork.Deliver("input", CreateMessage("World"));

        var nextMessage = _inMemNetwork.GetNextOrNull("input");
        AssertMessage(nextMessage, "Hello");

        var messages = _inMemNetwork.GetMessages("input");
        Assert.That(messages.Count, Is.EqualTo(1));
        AssertMessage(messages[0], "World");
    }

    private InMemTransportMessage CreateMessage(string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var transportMessage = new TransportMessage(new Dictionary<string, string>(), bytes);
            
        return new InMemTransportMessage(transportMessage);
    }

    private void AssertMessage(InMemTransportMessage message, string expectedBody)
    {
        var body = Encoding.UTF8.GetString(message.Body);

        Assert.That(message.Headers, Is.Empty);
        Assert.That(body, Is.EqualTo(expectedBody));
    }
}