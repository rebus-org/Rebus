using System;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Compression
{
    /// <summary>
    /// Outgoing step that can be inserted in order to compress the message body of outgoing messages when their length
    /// exceeds a configurable value
    /// </summary>
    public class ZipMessagesOutgoingStep : IOutgoingStep
    {
        /// <summary>
        /// Header value used when the contents is encoded with gzip
        /// </summary>
        public const string GzipEncodingHeader = "gzip";
        
        readonly Zipper _zipper;
        readonly int _bodySizeThresholdBytes;

        /// <summary>
        /// Constructs the step
        /// </summary>
        public ZipMessagesOutgoingStep(Zipper zipper, int bodySizeThresholdBytes)
        {
            _zipper = zipper;
            _bodySizeThresholdBytes = bodySizeThresholdBytes;
        }

        /// <summary>
        /// Compresses theo outgoing transport message body
        /// </summary>
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            PossiblyCompressTransportMessage(context);
            
            await next();
        }

        void PossiblyCompressTransportMessage(OutgoingStepContext context)
        {
            var transportMessage = context.Load<TransportMessage>();

            if (transportMessage.Body == null) return;

            if (transportMessage.Body.Length < _bodySizeThresholdBytes) return;

            var headers = transportMessage.Headers.Clone();
            var compressedBody = _zipper.Zip(transportMessage.Body);
            
            headers[Headers.ContentEncoding] = GzipEncodingHeader;

            var compressedTransportMessage = new TransportMessage(headers, compressedBody);

            context.Save(compressedTransportMessage);
        }
    }
}