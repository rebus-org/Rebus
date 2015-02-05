using System.Collections.Generic;
using System.Linq;

namespace Rebus2.Extensions
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, string> Clone(this Dictionary<string, string> dictionary)
        {
            return new Dictionary<string, string>(dictionary);
        }

        public static string GetValue(this Dictionary<string, string> dictionary, string key)
        {
            string value;
            
            if (dictionary.TryGetValue(key, out value)) 
                return value;

            throw new KeyNotFoundException(string.Format("Could not find the key '{0}' - have the following keys only: {1}", 
                key, dictionary.Keys.Select(k => string.Format("'{0}'", k))));
        }
    }
}