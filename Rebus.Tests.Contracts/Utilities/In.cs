using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Utilities;

public class IntroducerOfLatency : ITransport
{
    readonly ITransport _innerTransport;
    readonly int? _sendLatencyMs;
    readonly int? _receiveLatencyMs;

    public IntroducerOfLatency(ITransport innerTransport, int? sendLatencyMs = null, int? receiveLatencyMs = null)
    {
        _innerTransport = innerTransport;
        _sendLatencyMs = sendLatencyMs;
        _receiveLatencyMs = receiveLatencyMs;
    }

    public void CreateQueue(string address)
    {
        _innerTransport.CreateQueue(address);
    }

    public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
    {
        if (_sendLatencyMs.HasValue)
        {
            await Task.Delay(_sendLatencyMs.Value);
        }

        await _innerTransport.Send(destinationAddress, message, context);
    }

    public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
    {
        if (_receiveLatencyMs.HasValue)
        {
            await Task.Delay(_receiveLatencyMs.Value, cancellationToken);
        }

        return await _innerTransport.Receive(context, cancellationToken);
    }

    public string Address => _innerTransport.Address;
}