using System.Collections.Generic;
using System.Messaging;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Message = Rebus.Messages.Message;

namespace Rebus.Tests.Util
{
    public class MsmqTestHelper
    {
        public static IEnumerable<Message> GetMessagesFrom(string queueName)
        {
            using (var queue = new MessageQueue(MsmqUtil.GetPath(queueName)))
            {
                queue.Formatter = new RebusTransportMessageFormatter();
                queue.MessageReadPropertyFilter = RebusTransportMessageFormatter.PropertyFilter;

                bool gotMessage;

                do
                {
                    Message messageToReturn;
                    try
                    {
                        var msmqMessage = queue.Receive(3.Seconds());
                        if (msmqMessage == null)
                        {
                            yield break;
                        }
                        var receivedTransportMessage = (ReceivedTransportMessage)msmqMessage.Body;
                        var serializer = new JsonMessageSerializer();

                        messageToReturn = serializer.Deserialize(receivedTransportMessage);
                    }
                    catch (MessageQueueException exception)
                    {
                        if (exception.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                        {
                            yield break;
                        }

                        throw;
                    }

                    if (messageToReturn != null)
                    {
                        gotMessage = true;
                        yield return messageToReturn;
                    }
                    else
                    {
                        gotMessage = false;
                    }
                } while (gotMessage);
            }
        }
 
    }
}