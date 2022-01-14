using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rebus.Tests.Extensions;

static class TestExtensions
{
    public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj, Formatting.Indented);

    public static string PrettifyJson(this string json)
    {
        try
        {
            return JObject.Parse(json).ToString(Formatting.Indented);
        }
        catch (Exception exception)
        {
            throw new FormatException($"Could not prettify JSON text '{json}'", exception);
        }
    }
}