using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rebus.Serialization
{
    /// <summary>
    /// Helper that can serialize a dictionary to a string and vice versa
    /// </summary>
    internal class DictionarySerializer
    {
        static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings();

        /// <summary>
        /// Serialized the specified dictionary
        /// </summary>
        public string Serialize(IDictionary<string, object> dictionary)
        {
            return JsonConvert.SerializeObject(dictionary, Formatting.Indented, JsonSettings);
        }

        /// <summary>
        /// Deserializes the specified string
        /// </summary>
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