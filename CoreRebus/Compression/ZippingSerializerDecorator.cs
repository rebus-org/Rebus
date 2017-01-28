using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;

namespace Rebus.Compression
{
    class ZippingSerializerDecorator : ISerializer
    {
        public const string GzipEncodingHeader = "gzip";

        readonly ISerializer _serializer;
        readonly Zipper _zipper;
        readonly int _bodySizeThresholdBytes;

        public ZippingSerializerDecorator(ISerializer serializer, Zipper zipper, int bodySizeThresholdBytes)
        {
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (zipper == null) throw new ArgumentNullException(nameof(zipper));
            if (bodySizeThresholdBytes < 0) throw new ArgumentOutOfRangeException(nameof(bodySizeThresholdBytes), bodySizeThresholdBytes, "Body size threshold must be >= 0");
            _serializer = serializer;
            _zipper = zipper;
            _bodySizeThresholdBytes = bodySizeThresholdBytes;
        }

        public async Task<TransportMessage> Serialize(Message message)
        {
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
            string contentEncoding;

            if (!transportMessage.Headers.TryGetValue(Headers.ContentEncoding, out contentEncoding))
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
}