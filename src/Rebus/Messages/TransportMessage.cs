using System.Xml.Serialization;

namespace Rebus.Messages
{
    [XmlInclude(typeof(SubscriptionMessage))]
    public class TransportMessage
    {
        public string ReturnAddress { get; set; }
        public object[] Messages { get; set; }
    }
}