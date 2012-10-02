using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rebus.Serialization
{
    public class DictionarySerializer
    {
        static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings();

        public string Serialize(IDictionary<string, object> dictionary)
        {
            return JsonConvert.SerializeObject(dictionary, Formatting.Indented, JsonSettings);
        }

        public IDictionary<string, object> Deserialize(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<IDictionary<string, object>>(str, JsonSettings);
            }
            catch(Exception e)
            {
                throw new ApplicationException(
                    string.Format(@"Could not JSON deserialize the following string:
{0}", str ?? "(null)"), e);
            }
        }
    }
}