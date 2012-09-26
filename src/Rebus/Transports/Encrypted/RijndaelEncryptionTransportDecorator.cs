using Rebus.Extensions;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Transports.Encrypted
{
    public class RijndaelEncryptionTransportDecorator : ISendMessages, IReceiveMessages
    {
        readonly RijndaelHelper helper;
        readonly ISendMessages innerSendMessages;
        readonly IReceiveMessages innerReceiveMessages;

        public RijndaelEncryptionTransportDecorator(ISendMessages innerSendMessages, IReceiveMessages innerReceiveMessages, string keyBase64)
        {
            this.innerSendMessages = innerSendMessages;
            this.innerReceiveMessages = innerReceiveMessages;
            helper = new RijndaelHelper(keyBase64);
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            var iv = helper.GenerateNewIv();

            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Headers = message.Headers.Clone(),
                                                 Label = message.Label,
                                                 Body = helper.Encrypt(message.Body, iv),
                                             };

            transportMessageToSend.Headers[Headers.Encrypted] = null;
            transportMessageToSend.Headers[Headers.EncryptionSalt] = iv;

            innerSendMessages.Send(destinationQueueName, transportMessageToSend, context);
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            var receivedTransportMessage = innerReceiveMessages.ReceiveMessage(context);

            if (receivedTransportMessage == null) return null;

            byte[] body;
            var headers = receivedTransportMessage.Headers.Clone();

            if (headers.ContainsKey(Headers.Encrypted))
            {
                var iv = headers[Headers.EncryptionSalt];
                body = helper.Decrypt(receivedTransportMessage.Body, iv);

                headers.Remove(Headers.EncryptionSalt);
                headers.Remove(Headers.Encrypted);
            }
            else
            {
                body = receivedTransportMessage.Body;
            }

            return new ReceivedTransportMessage
                       {
                           Id = receivedTransportMessage.Id,
                           Headers = headers,
                           Label = receivedTransportMessage.Label,
                           Body = body,
                       };
        }

        public string InputQueue
        {
            get { return innerReceiveMessages.InputQueue; }
        }

        public string InputQueueAddress
        {
            get { return innerReceiveMessages.InputQueueAddress; }
        }

        public static string GenerateKeyBase64()
        {
            return RijndaelHelper.GenerateNewKey();
        }
    }
}