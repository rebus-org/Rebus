using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rebus.Serialization
{
    /// <summary>
    /// Generic serializer that serializes <see cref="Dictionary{TKey,TValue}"/> of <see cref="String"/> keys and <see cref="String"/> values
    /// into JSON and back
    /// </summary>
    public class HeaderSerializer
    {
        readonly JsonSerializerSettings _settings = new JsonSerializerSettings();

        /// <summary>
        /// Serializes the given dictionary into a JSON string
        /// </summary>
        public string SerializeToString(Dictionary<string, string> headers)
        {
            return JsonConvert.SerializeObject(headers, _settings);
        }

        /// <summary>
        /// Deserializes the given JSON string into a dictionary
        /// </summary>
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