using System;
using System.Text;
using Newtonsoft.Json;
// ReSharper disable UnusedMember.Global

namespace Rebus.Serialization;

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
        var jsonString = SerializeToString(obj);

        return TextEncoding.GetBytes(jsonString);
    }

    /// <summary>
    /// Serializes the given object into a string
    /// </summary>
    public string SerializeToString(object obj)
    {
        return JsonConvert.SerializeObject(obj, Settings);
    }

    /// <summary>
    /// Deserializes the given byte[] into an object
    /// </summary>
    public object Deserialize(byte[] bytes)
    {
        var jsonString = TextEncoding.GetString(bytes);

        return DeserializeFromString(jsonString);
    }

    /// <summary>
    /// Deserializes the given string into an object
    /// </summary>
    public object DeserializeFromString(string str)
    {
        try
        {
            return JsonConvert.DeserializeObject(str, Settings);
        }
        catch (Exception exception)
        {
            throw new JsonSerializationException($"Could not deserialize '{str}'", exception);
        }
    }
}