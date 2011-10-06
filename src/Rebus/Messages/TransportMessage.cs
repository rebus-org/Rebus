using System.Xml.Serialization;
using System.Linq;

namespace Rebus.Messages
{
    [XmlInclude(typeof(SubscriptionMessage))]
    public class TransportMessage
    {
        public string ReturnAddress { get; set; }
        public object[] Messages { get; set; }

        public string GetLabel()
        {
            if (Messages == null || Messages.Length == 0)
                return "Empty TransportMessage";

            return Messages.Length == 1
                       ? Messages[0].GetType().Name
                       : string.Join(", ", Messages.Select(m => m.GetType().Name));
        }
    }
}