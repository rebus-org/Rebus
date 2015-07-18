using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Compression
{
    /// <summary>
    /// Unzips incoming messages if they're equipped with the <see cref="Headers.ContentEncoding"/> header
    /// (currently only handles the <see cref="ZipMessagesOutgoingStep.GzipEncodingHeader"/> type of content encoding)
    /// </summary>
    public class UnzipMessagesIncomingStep : IIncomingStep
    {
        readonly Zipper _zipper;

        /// <summary>
        /// Message pipeline step that unzips incoming messages if the <see cref="Headers.ContentEncoding"/> is present
        /// </summary>
        public UnzipMessagesIncomingStep(Zipper zipper)
        {
            _zipper = zipper;
        }

        /// <summary>
        /// Decompresses the body of the incoming <see cref="TransportMessage"/> if it has the <see cref="Headers.ContentEncoding"/> header
        /// set to an understood content encoding
        /// </summary>
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            PossiblyDecompressTransportMessage(context);

            await next();
        }

        void PossiblyDecompressTransportMessage(IncomingStepContext context)
        {
            var transportMessage = context.Load<TransportMessage>();

            string contentEncoding;

            if (!transportMessage.Headers.TryGetValue(Headers.ContentEncoding, out contentEncoding))
                return;

            if (contentEncoding != ZipMessagesOutgoingStep.GzipEncodingHeader)
            {
                throw new ArgumentException(string.Format("The message {0} has a '{1}' with the value '{2}', but this middleware only knows how to decompress '{3}'",
                    transportMessage.GetMessageLabel(), Headers.ContentEncoding, contentEncoding, ZipMessagesOutgoingStep.GzipEncodingHeader));
            }

            var headers = transportMessage.Headers.Clone();
            var compressedBody = transportMessage.Body;

            headers.Remove(Headers.ContentEncoding);

            var body = _zipper.Unzip(compressedBody);

            context.Save(new TransportMessage(headers, body));
        }
    }
}