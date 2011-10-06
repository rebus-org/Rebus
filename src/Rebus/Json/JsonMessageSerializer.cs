using Newtonsoft.Json;

namespace Rebus.Json
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, Settings);
        }

        public object Deserialize(string str)
        {
            return JsonConvert.DeserializeObject(str, Settings);
        }
    }
}