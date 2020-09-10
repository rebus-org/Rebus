using Newtonsoft.Json;
using System;
using System.Text;

namespace Rebus.Sagas.Json
{
    public class SagaSerializer : ISagaSerializer
    {
        static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public SagaSerializer(JsonSerializerSettings jsonSerializerSettings = null, Encoding encoding = null)
        {
            _settings = jsonSerializerSettings ?? DefaultSettings;
            _encoding = encoding ?? DefaultEncoding;
        }

        readonly JsonSerializerSettings _settings;
        readonly Encoding _encoding;

        /// <summary>
        /// Serializes the given object into a byte[]
        /// </summary>
        public byte[] Serialize(object obj)
        {
            var jsonString = SerializeToString(obj);

            return _encoding.GetBytes(jsonString);
        }

        /// <summary>
        /// Serializes the given object into a string
        /// </summary>
        public string SerializeToString(object obj)
        {
            return JsonConvert.SerializeObject(obj, _settings);
        }

        /// <summary>
        /// Deserializes the given byte[] into an object
        /// </summary>
        public object Deserialize(byte[] bytes)
        {
            var jsonString = _encoding.GetString(bytes);

            return DeserializeFromString(jsonString);
        }

        /// <summary>
        /// Deserializes the given string into an object
        /// </summary>
        public object DeserializeFromString(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject(str, _settings);
            }
            catch (Exception exception)
            {
                throw new JsonSerializationException($"Could not deserialize '{str}'", exception);
            }
        }
    }
}
