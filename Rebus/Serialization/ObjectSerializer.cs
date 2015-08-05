using System;
using System.Text;
using Newtonsoft.Json;

namespace Rebus.Serialization
{
    /// <summary>
    /// Generic serializer that happily serializes rich objects. Uses JSON.NET internally with full type information.
    /// </summary>
    public class ObjectSerializer
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        static readonly Encoding TextEncoding = Encoding.UTF8;

        /// <summary>
        /// Serializes the given object into a byte[]
        /// </summary>
        public byte[] Serialize(object obj)
        {
            var jsonString = JsonConvert.SerializeObject(obj, Settings);

            return TextEncoding.GetBytes(jsonString);
        }

        /// <summary>
        /// Deserializes the given byte[] into an object
        /// </summary>
        public object Deserialize(byte[] bytes)
        {
            var jsonString = TextEncoding.GetString(bytes);

            try
            {
                return JsonConvert.DeserializeObject(jsonString, Settings);
            }
            catch (Exception exception)
            {
                throw new JsonSerializationException(string.Format("Could not deserialize '{0}'", jsonString), exception);
            }
        }
    }
}