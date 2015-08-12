using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Rebus.AzureServiceBus
{
    [DataContract]
    class Envelope
    {
        [DataMember]
        public Dictionary<string, string> Headers { get; set; }

        [DataMember]
        public byte[] Body { get; set; }

        [DataMember]
        public string Label { get; set; }
    }
}