using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Transport;

/// <summary>
/// <see cref="ITransport"/> decorator that snatches a copy of all sent/received messages
/// </summary>
class TransportTap : ITransport
{
    readonly List<TransportMessage> _receivedMessages = new List<TransportMessage>();
    readonly List<TransportMessage> _sentMessages = new List<TransportMessage>();
    readonly ITransport _innerTransport;

    public TransportTap(ITransport innerTransport)
    {
        _innerTransport = innerTransport;
    }

    public event Action<TransportMessage> MessageSent = delegate { }; 
        
    public event Action<TransportMessage> MessageReceived = delegate { }; 
        
    public event Action NoMessageReceived = delegate { }; 

    public void CreateQueue(string address)
    {
        _innerTransport.CreateQueue(address);
    }

    public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
    {
        await _innerTransport.Send(destinationAddress, message, context);

        _sentMessages.Add(message);

        MessageSent(message);
    }

    public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
    {
        var transportMessage = await _innerTransport.Receive(context, cancellationToken);

        if (transportMessage != null)
        {
            _receivedMessages.Add(transportMessage);

            MessageReceived(transportMessage);
        }
        else
        {
            NoMessageReceived();
        }

        return transportMessage;
    }

    public List<TransportMessage> ReceivedMessages
    {
        get { return _receivedMessages; }
    }

    public List<TransportMessage> SentMessages
    {
        get { return _sentMessages; }
    }

    public string Address
    {
        get { return _innerTransport.Address; }
    }
}

public static class TransportTapExtensions
{
    public static void TapSentMessagesInto(this StandardConfigurer<ITransport> configurer, ICollection<TransportMessage> sentMessages)
    {
        configurer.Decorate(c =>
        {
            var tap = new TransportTap(c.Get<ITransport>());
            tap.MessageSent += sentMessages.Add;
            return tap;
        });
    }

    public static void TapReceivedMessagesInto(this StandardConfigurer<ITransport> configurer, ICollection<TransportMessage> receivedMessages)
    {
        configurer.Decorate(c =>
        {
            var tap = new TransportTap(c.Get<ITransport>());
            tap.MessageReceived += receivedMessages.Add;
            return tap;
        });
    }
}