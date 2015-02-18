using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Rebus.Messages;
using Rebus.Extensions;
using Rebus.Shared;

namespace Rebus.Serialization.Json
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
    public class NewtonSoftJsonMessageSerializer : ISerializeMessages
    {
        const string JsonContentTypeName = "text/json";

        static readonly Encoding DefaultEncoding = Encoding.UTF7;

        /// <summary>
        /// Used as the default serailizer setting if settings is not passed into the constructor.
        /// </summary>
        readonly JsonSerializerSettings settings = 
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        
        readonly CultureInfo serializationCulture = CultureInfo.InvariantCulture;
        
        Encoding customEncoding;

        /// <summary>
        /// Constructs the serializer
        /// </summary>
        public NewtonSoftJsonMessageSerializer(JsonSerializerSettings settings)
        {
            if(settings != null)
            {
                this.settings = settings;
            }
        }

        /// <summary>
        /// Serializes the transport message <see cref="Message"/> using JSON.NET and wraps it in a <see cref="TransportMessageToSend"/>
        /// </summary>
        public TransportMessageToSend Serialize(Message message)
        {
            using (new CultureContext(serializationCulture))
            {
                var messageAsString = JsonConvert.SerializeObject(message.Messages, Formatting.Indented, settings);
                var encodingToUse = customEncoding ?? DefaultEncoding;

                var headers = message.Headers.Clone();
                headers[Headers.AssemblyQualifiedName] = message.Messages.GetType().AssemblyQualifiedName;
                headers[Headers.ContentType] = JsonContentTypeName;
                headers[Headers.Encoding] = encodingToUse.WebName;

                return new TransportMessageToSend
                           {
                               Body = encodingToUse.GetBytes(messageAsString),
                               Headers = headers,
                               Label = message.GetLabel(),
                           };
            }
        }

        /// <summary>
        /// Deserializes the transport message using JSON.NET from a <see cref="ReceivedTransportMessage"/> and wraps it in a <see cref="Message"/>
        /// </summary>
        public Message Deserialize(ReceivedTransportMessage transportMessage)
        {
            if (transportMessage == null) throw new ArgumentNullException("transportMessage", "A transport message must be passed to this function in order to deserialize");
            
            using (new CultureContext(serializationCulture))
            {
                var headers = transportMessage.Headers.Clone();
                var encodingToUse = GetEncodingOrThrow(headers);

                var serializedTransportMessage = encodingToUse.GetString(transportMessage.Body);
                try
                {
                    var typeName = headers[Headers.AssemblyQualifiedName].ToString();
                    var messages = (object[]) JsonConvert.DeserializeObject(serializedTransportMessage, Type.GetType(typeName), settings);

                    return new Message
                               {
                                   Headers = headers,
                                   Messages = messages
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