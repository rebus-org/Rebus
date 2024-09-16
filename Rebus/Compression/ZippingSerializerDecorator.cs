using System;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.Compression;

sealed class ZippingSerializerDecorator : ZipDecoratorBase, ISerializer
{
    readonly ISerializer _serializer;
    readonly Zipper _zipper;
    readonly int _bodySizeThresholdBytes;

    public ZippingSerializerDecorator(ISerializer serializer, Zipper zipper, int bodySizeThresholdBytes)
    {
        if (bodySizeThresholdBytes < 0) throw new ArgumentOutOfRangeException(nameof(bodySizeThresholdBytes), bodySizeThresholdBytes, "Body size threshold must be >= 0");
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _zipper = zipper ?? throw new ArgumentNullException(nameof(zipper));
        _bodySizeThresholdBytes = bodySizeThresholdBytes;
    }

    public async Task<TransportMessage> Serialize(Message message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var transportMessage = await _serializer.Serialize(message);
        var body = transportMessage.Body;

        if (body.Length < _bodySizeThresholdBytes)
        {
            return transportMessage;
        }

        var headers = transportMessage.Headers.Clone();
        var compressedBody = _zipper.Zip(transportMessage.Body);

        headers[Headers.ContentEncoding] = GzipEncodingHeader;

        var compressedTransportMessage = new TransportMessage(headers, compressedBody);

        return compressedTransportMessage;
    }

    public async Task<Message> Deserialize(TransportMessage transportMessage)
    {
        if (transportMessage == null) throw new ArgumentNullException(nameof(transportMessage));

        return await _serializer.Deserialize(transportMessage);
    }
}