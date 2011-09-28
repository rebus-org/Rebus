using System.Xml.Serialization;

namespace Rebus
{
    [XmlInclude(typeof(SubscriptionMessage))]
    public class TransportMessage
    {
        public string ReturnAddress { get; set; }
        public object[] Messages { get; set; }
    }
}