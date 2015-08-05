using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rebus.Serialization
{
    public class HeaderSerializer
    {
        readonly JsonSerializerSettings _settings = new JsonSerializerSettings();

        public string SerializeToString(Dictionary<string, string> headers)
        {
            return JsonConvert.SerializeObject(headers, _settings);
        }

        public Dictionary<string, string> DeserializeFromString(string headers)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(headers, _settings);
            }
            catch (Exception exception)
            {
                throw new JsonSerializationException(string.Format("Could not deserialize JSON text as headers: '{0}'", headers), exception);
            }
        } 
    }
}