using System;
using System.IO;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Rebus.DataBus
{
    [Serializable]
    public class DataBusAttachment
    {
        public DataBusAttachment(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public static async Task<DataBusAttachment> FromStream(Stream source, IBus bus)
        {
            var attachment = new DataBusAttachment("hej");

            return attachment;
        }

        public Stream OpenRead()
        {
            throw new NotImplementedException();
        }
    }
}
