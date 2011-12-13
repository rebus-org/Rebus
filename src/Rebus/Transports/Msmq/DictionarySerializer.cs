// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Rebus.Transports.Msmq
{
    /// <summary>
    /// HACKETYHACK!! (please don't serialize dictionaries containing the following chars:
    ///     {
    ///     }
    ///     ,
    ///     ;
    /// </summary>
    public class DictionarySerializer
    {
        public string Serialize(IDictionary<string, string> dictionary)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            var first = true;
            foreach (var kvp in dictionary)
            {
                if (!first) builder.Append(";");
                builder.Append(KvpToString(kvp));
                first = false;
            }
            builder.Append("]");
            return builder.ToString();
        }

        public IDictionary<string, string> Deserialize(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return null;
            var dictionary = new Dictionary<string, string>();
            if (!(str.StartsWith("[") && str.EndsWith("]")))
            {
                throw FormatException("Cannot deserialize {0} - string must start with '[' and end with ']'", str);
            }
            var substring = str.Substring(1, str.Length - 2);
            if (string.IsNullOrWhiteSpace(substring)) return dictionary;
            foreach (var kvpStr in substring.Split(';'))
            {
                var kvp = DeserializeKvp(kvpStr);
                dictionary.Add(kvp.Key, kvp.Value);
            }
            return dictionary;
        }

        KeyValuePair<string,string> DeserializeKvp(string kvpStr)
        {
            if (!(kvpStr.StartsWith("{") && kvpStr.EndsWith("}")))
            {
                throw FormatException(@"Cannot deserialize {0} - string must start with '{{' and end with '}}'", kvpStr);
            }
            var substring = kvpStr.Substring(1, kvpStr.Length - 2);
            var tokens = substring.Split(',');
            if (tokens.Length != 2)
            {
                throw FormatException("Cannot deserialize {0} - string must consist of two comma-separated strings");
            }
            return new KeyValuePair<string, string>(Unquote(tokens[0]), Unquote(tokens[1]));
        }

        string Unquote(string token)
        {
            if (!(token.StartsWith(@"""") && token.EndsWith(@"""")))
            {
                throw FormatException("Could not understand {0} - expected string surrounded by quotes", token);
            }
            return token.Substring(1, token.Length - 2);
        }

        string KvpToString(KeyValuePair<string, string> kvp)
        {
            return string.Format(@"{{""{0}"",""{1}""}}", kvp.Key, kvp.Value);
        }

        FormatException FormatException(string message, params object[] objs)
        {
            return new FormatException(string.Format(message, objs));
        }
    }
}