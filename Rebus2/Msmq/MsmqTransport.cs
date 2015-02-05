using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using Rebus2.Messages;
using Message = System.Messaging.Message;

namespace Rebus2.Msmq
{
    public class MsmqTransport
    {
        readonly ExtensionSerializer _extensionSerializer = new ExtensionSerializer();
        readonly string _inputQueueName;

        public MsmqTransport(string inputQueueName)
        {
            _inputQueueName = inputQueueName;
        }

        public async Task Send(string destinationAddress, TransportMessage msg)
        {
            using (var queue = new MessageQueue(MsmqUtil.GetPath(destinationAddress)))
            {
                var message = new Message
                {
                    Extension = _extensionSerializer.Serialize(msg.Headers),
                    BodyStream = msg.Body,
                    UseJournalQueue = false,
                };

                using (var messageQueueTransaction = new MessageQueueTransaction())
                {
                    messageQueueTransaction.Begin();
                    
                    queue.Send(message, messageQueueTransaction);

                    messageQueueTransaction.Commit();
                }
            }
        }

        class ExtensionSerializer
        {
            static readonly Encoding DefaultEncoding = Encoding.UTF8;
            const char Splitter = '\r';
            static readonly string SplitterAsString = Splitter.ToString();

            public byte[] Serialize(Dictionary<string, string> headers)
            {
                return
                    DefaultEncoding.GetBytes(string.Join(SplitterAsString,
                        headers.Select(kvp => string.Format("{0}={1}", kvp.Key, EnsureDoesNotContainNewline(kvp.Value)))));
            }

            public Dictionary<string, string> Deserialize(byte[] bytes)
            {
                var dictionary = DefaultEncoding.GetString(bytes)
                    .Split(Splitter)
                    .Select(line =>
                    {
                        var tokens = line.Split('=');
                        
                        if (tokens.Length < 2)
                        {
                            throw new FormatException(string.Format("Cannot parse '{0}' as a key-value pair", line));
                        }
                      
                        return new KeyValuePair<string, string>(tokens[0], string.Join("=", tokens.Skip(1)));
                    })
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return dictionary;
            }

            static string EnsureDoesNotContainNewline(string value)
            {
                if (value.Any(c => c == '\r'))
                {
                    throw new FormatException(string.Format("The header value '{0}' contains \\r which is invalid in a header value!", value));
                }

                return value;
            }
        }
    }
}