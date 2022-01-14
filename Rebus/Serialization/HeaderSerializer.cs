using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
// ReSharper disable UnusedMember.Global

namespace Rebus.Serialization;

/// <summary>
/// Simple serializer that can be used to encode/decode headers to/from bytes
/// </summary>
public class HeaderSerializer
{
    static readonly Encoding DefaultEncoding = Encoding.UTF8;

    /// <summary>
    /// Configures which encoding to use for encoding the string of headers to/from bytes
    /// </summary>
    public Encoding Encoding { get; set; } = DefaultEncoding;

    /// <summary>
    /// Encodes the headers into a string
    /// </summary>
    public string SerializeToString(Dictionary<string, string> headers)
    {
        return JsonConvert.SerializeObject(headers);
    }

    /// <summary>
    /// Encodes the headers into a byte array
    /// </summary>
    public byte[] Serialize(Dictionary<string, string> headers)
    {
        var jsonString = SerializeToString(headers);

        return Encoding.GetBytes(jsonString);
    }

    /// <summary>
    /// Decodes the headers from the given byte array
    /// </summary>
    public Dictionary<string, string> Deserialize(byte[] bytes)
    {
        var jsonString = Encoding.GetString(bytes);

        return DeserializeFromString(jsonString);
    }

    /// <summary>
    /// Decodes the headers from the given string
    /// </summary>
    public Dictionary<string, string> DeserializeFromString(string str)
    {
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
        }
        catch (Exception exception)
        {
            throw new SerializationException($"Could not deserialize JSON text '{str}'", exception);
        }
    }
}