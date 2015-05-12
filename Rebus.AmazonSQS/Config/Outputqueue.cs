using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.SQS.Model;

namespace Rebus.AmazonSQS.Config
{
    class InMemOutputQueue : ConcurrentDictionary<string, List<SendMessageBatchRequestEntry>>
    {


        public void AddMessage(string destinationAddressUrl, SendMessageBatchRequestEntry message)
        {

            AddOrUpdate(destinationAddressUrl,
                (key) => new List<SendMessageBatchRequestEntry>(new[] { message }),
                                    (key, list) =>
                                    {
                                        list.Add(message);
                                        return list;
                                    });

        }

        public IEnumerable<InMemDestinationRequests> GetMessages()
        {

            return ToArray().Select(lists => new InMemDestinationRequests(lists.Key, lists.Value));
        }






    }

    class InMemDestinationRequests
    {
        public InMemDestinationRequests(string destinationAddressUrl, IEnumerable<SendMessageBatchRequestEntry> messageRequests)
        {
            DestinationAddressUrl = destinationAddressUrl;
            Messages = messageRequests;
        }

        public string DestinationAddressUrl { get; private set; }

        public IEnumerable<SendMessageBatchRequestEntry> Messages { get; private set; }


    }

}