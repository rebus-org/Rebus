using Newtonsoft.Json;

namespace Rebus.Tests.Extensions
{
    static class TestExtensions
    {
        public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}