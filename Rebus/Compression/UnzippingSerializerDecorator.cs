using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.Compression;

sealed class UnzippingSerializerDecorator : ZipDecoratorBase, ISerializer
{
    readonly ISerializer _serializer;
    readonly Zipper _zipper;

    public UnzippingSerializerDecorator(ISerializer serializer, Zipper zipper)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _zipper = zipper ?? throw new ArgumentNullException(nameof(zipper));
    }

    public Task<TransportMessage> Serialize(Message message) => _serializer.Serialize(message);

    public async Task<Message> Deserialize(TransportMessage transportMessage)
    {
        if (!transportMessage.Headers.TryGetValue(Headers.ContentEncoding, out var contentEncoding))
        {
            return await _serializer.Deserialize(transportMessage);
        }

        if (contentEncoding != GzipEncodingHeader)
        {
            var message = $"The message {transportMessage.GetMessageLabel()} has a '{Headers.ContentEncoding}' with the" +
                          $" value '{contentEncoding}', but this serializer decorator only knows how to decompress" +
                          $" '{GzipEncodingHeader}'";

            throw new ArgumentException(message);
        }

        var headers = transportMessage.Headers.Clone();
        var compressedBody = transportMessage.Body;

        headers.Remove(Headers.ContentEncoding);

        var uncompressedBody = _zipper.Unzip(compressedBody);
        var uncompressedTransportMessage = new TransportMessage(headers, uncompressedBody);

        return await _serializer.Deserialize(uncompressedTransportMessage);
    }
}