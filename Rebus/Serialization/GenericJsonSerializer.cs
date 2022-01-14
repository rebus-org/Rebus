using System;
using Newtonsoft.Json;
// ReSharper disable UnusedMember.Global

namespace Rebus.Serialization;

/// <summary>
/// Generic serializer that serializes an object into a string and vice versa. Uses a normal, compact JSON format,
/// requiring the serialized type to be known at deserialization time
/// </summary>
public class GenericJsonSerializer
{
    static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>
    /// Serializes the given object to a string. No type information is included - therefore, abstract members etc.
    /// cannot be reproduced when deserializing
    /// </summary>
    public string Serialize<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj, Settings);
    }

    /// <summary>
    /// Deserializes the given JSON string to the type specified
    /// </summary>
    public T Deserialize<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }
        catch (Exception exception)
        {
            throw new JsonSerializationException($"Could not deserialize JSON text '{json}'", exception);
        }
    }
}