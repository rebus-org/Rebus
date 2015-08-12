using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Rebus.Messages;
using Rebus.Shared;

namespace Rebus.NewtonsoftJson
{
    /// <summary>
    /// Implementation of <see cref="ISerializeMessages"/> that uses Newtonsoft JSON.NET internally to serialize transport messages.
    /// The used JSON.NET DLL is merged into Rebus, which allows it to be used by Rebus without bothering people by an extra dependency.
    /// 
    /// JSON.NET has the following license:
    /// ----------------------------------------------------------------------------------------------------------------------------
    /// Copyright (c) 2007 James Newton-King
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
    /// files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
    /// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
    /// is furnished to do so, subject to the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
    /// 
    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    /// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
    /// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
    /// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    /// ----------------------------------------------------------------------------------------------------------------------------
    /// Bam!1 Thanks James :)
    /// </summary>
    public class NewtonsoftJsonMessageSerializer : ISerializeMessages
    {
        const string JsonContentTypeName = "text/json";

        static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Used as the default serailizer setting if settings is not passed into the constructor.
        /// </summary>
        readonly JsonSerializerSettings settings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        readonly CultureInfo serializationCulture = CultureInfo.InvariantCulture;

        const char LineSeparator = '\r';

        /// <summary>
        /// Constructs the serializer with default serializer settings
        /// </summary>
        public NewtonsoftJsonMessageSerializer()
        {
        }

        /// <summary>
        /// Constructs the serializer with the given serializer settings
        /// </summary>
        public NewtonsoftJsonMessageSerializer(JsonSerializerSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            this.settings = settings;
        }

        /// <summary>
        /// Serializes the transport message <see cref="Message"/> using JSON.NET and wraps it in a <see cref="TransportMessageToSend"/>
        /// </summary>
        public TransportMessageToSend Serialize(Message message)
        {
            using (new CultureContext(serializationCulture))
            {
                var encodingToUse = DefaultEncoding;

                var headers = new Dictionary<string, object>(message.Headers);

                // one line of JSON object per logical message
                var logicalMessageLines = string.Join(LineSeparator.ToString(), message.Messages.Select(m => JsonConvert.SerializeObject(m, Formatting.None, settings)));

                // include the types of the messages
                var messageTypes = string.Join(";", message.Messages.Select(m => GetMinimalAssemblyQualifiedName(m.GetType())));

                headers[Headers.MessageTypes] = messageTypes;
                headers[Headers.ContentType] = JsonContentTypeName;
                headers[Headers.Encoding] = encodingToUse.WebName;

                return new TransportMessageToSend
                           {
                               Body = encodingToUse.GetBytes(logicalMessageLines),
                               Headers = headers,
                               Label = message.GetLabel(),
                           };
            }
        }

        /// <summary>
        /// Gets an assembly-qualified name without the version and public key token stuff
        /// </summary>
        static string GetMinimalAssemblyQualifiedName(Type type)
        {
            return string.Format("{0},{1}", type.FullName, type.Assembly.GetName().Name);
        }

        /// <summary>
        /// Deserializes the transport message using JSON.NET from a <see cref="ReceivedTransportMessage"/> and wraps it in a <see cref="Message"/>
        /// </summary>
        public Message Deserialize(ReceivedTransportMessage transportMessage)
        {
            if (transportMessage == null) throw new ArgumentNullException("transportMessage", "A transport message must be passed to this function in order to deserialize");

            using (new CultureContext(serializationCulture))
            {
                var headers = new Dictionary<string, object>(transportMessage.Headers);
                var encodingToUse = GetEncodingOrThrow(headers);

                var serializedTransportMessage = encodingToUse.GetString(transportMessage.Body);

                if (!headers.ContainsKey(Headers.MessageTypes))
                {
                    throw new SerializationException(string.Format("Could not find the '{0}' header in the message", Headers.MessageTypes));
                }

                try
                {
                    var concatenatedTypeNames = headers[Headers.MessageTypes].ToString();
                    var typeNames = concatenatedTypeNames.Split(';');
                    var jsonLines = serializedTransportMessage.Split(LineSeparator);

                    if (typeNames.Length != jsonLines.Length)
                    {
                        throw new SerializationException(string.Format("Number of message types in the '{0}' header does not correspond to the number of lines in the body - here's the header: '{1}' - here's the body: {2}", 
                            Headers.MessageTypes, concatenatedTypeNames, serializedTransportMessage));
                    }

                    var messages = typeNames
                        .Zip(jsonLines, (typeName, jsonLine) =>
                        {
                            var messageType = Type.GetType(typeName);
                            if (messageType == null)
                            {
                                throw new SerializationException(string.Format("Could not find message type '{0}' in the current AppDomain", typeName));
                            }

                            var message = JsonConvert.DeserializeObject(jsonLine, messageType, settings);

                            return message;
                        });

                    return new Message
                               {
                                   Headers = headers,
                                   Messages = messages.ToArray()
                               };
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        string.Format(
                            "An error occurred while attempting to deserialize JSON text '{0}' into an object[]",
                            serializedTransportMessage), e);
                }
            }
        }

        Encoding GetEncodingOrThrow(IDictionary<string, object> headers)
        {
            if (!headers.ContainsKey(Headers.ContentType))
            {
                throw new ArgumentException(
                    string.Format("Received message does not have a proper '{0}' header defined!",
                                  Headers.ContentType));
            }

            var contentType = (headers[Headers.ContentType] ?? "").ToString();
            if (!JsonContentTypeName.Equals(contentType, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException(
                    string.Format(
                        "Received message has content type header with '{0}' which is not supported by the JSON serializer!",
                        contentType));
            }

            if (!headers.ContainsKey(Headers.Encoding))
            {
                throw new ArgumentException(
                    string.Format(
                        "Received message has content type '{0}', but the corresponding '{1}' header was not present!",
                        contentType, Headers.Encoding));
            }

            var encodingWebName = (headers[Headers.Encoding] ?? "").ToString();

            try
            {
                return Encoding.GetEncoding(encodingWebName);
            }
            catch (Exception e)
            {
                throw new ArgumentException(
                    string.Format("An error occurred while attempting to treat '{0}' as a proper text encoding",
                                  encodingWebName), e);
            }
        }

        class CultureContext : IDisposable
        {
            readonly CultureInfo currentCulture;
            readonly CultureInfo currentUiCulture;

            public CultureContext(CultureInfo cultureInfo)
            {
                var thread = Thread.CurrentThread;

                currentCulture = thread.CurrentCulture;
                currentUiCulture = thread.CurrentUICulture;

                thread.CurrentCulture = cultureInfo;
                thread.CurrentUICulture = cultureInfo;
            }

            public void Dispose()
            {
                var thread = Thread.CurrentThread;

                thread.CurrentCulture = currentCulture;
                thread.CurrentUICulture = currentUiCulture;
            }
        }
    }
}