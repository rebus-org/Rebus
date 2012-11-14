using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rebus.Messages;
using Rebus.Extensions;
using Rebus.Shared;

namespace Rebus.Serialization.Json
{
    /// <summary>
    /// Implementation of <see cref="ISerializeMessages"/> that uses Newtonsoft JSON.NET internally to serialize
    /// transport messages.
    /// </summary>
    public class JsonMessageSerializer : ISerializeMessages
    {
        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        static readonly CultureInfo SerializationCulture = CultureInfo.InvariantCulture;

        static readonly Encoding Encoding = Encoding.UTF7;
        readonly NonDefaultSerializationBinder binder;

        /// <summary>
        /// Constructs the serializer
        /// </summary>
        public JsonMessageSerializer()
        {
            binder = new NonDefaultSerializationBinder();
            Settings.Binder = binder;
        }

        /// <summary>
        /// Serializes the transport message <see cref="Message"/> using JSON.NET and wraps it in a <see cref="TransportMessageToSend"/>
        /// </summary>
        public TransportMessageToSend Serialize(Message message)
        {
            using (new CultureContext(SerializationCulture))
            {
                var messageAsString = JsonConvert.SerializeObject(message.Messages, Formatting.Indented, Settings);

                var headers = message.Headers.Clone();
                headers[Headers.ContentType] = "text/json";
                headers[Headers.Encoding] = Encoding.WebName;

                return new TransportMessageToSend
                           {
                               Body = Encoding.GetBytes(messageAsString),
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
            using (new CultureContext(SerializationCulture))
            {
                var messages = (object[])JsonConvert.DeserializeObject(Encoding.GetString(transportMessage.Body), Settings);

                return new Message
                           {
                               Headers = transportMessage.Headers.Clone(),
                               Messages = messages
                           };
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

        /// <summary>
        /// JSON.NET serialization binder that can be extended with a pipeline of name and type resolvers,
        /// allowing for customizing how types are bound
        /// </summary>
        class NonDefaultSerializationBinder : DefaultSerializationBinder
        {
            readonly List<Func<Type, TypeDescriptor>> nameResolvers = new List<Func<Type, TypeDescriptor>>();
            readonly List<Func<TypeDescriptor, Type>> typeResolvers = new List<Func<TypeDescriptor, Type>>();

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                foreach (var tryResolve in nameResolvers)
                {
                    var typeDescriptor = tryResolve(serializedType);
                    
                    if (typeDescriptor != null)
                    {
                        assemblyName = typeDescriptor.AssemblyName;
                        typeName = typeDescriptor.TypeName;
                        return;
                    }
                }

                base.BindToName(serializedType, out assemblyName, out typeName);
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                foreach (var tryResolve in typeResolvers)
                {
                    var typeDescriptor = new TypeDescriptor(assemblyName, typeName);
                    var type = tryResolve(typeDescriptor);

                    if (type != null)
                    {
                        return type;
                    }
                }

                return base.BindToType(assemblyName, typeName);
            }

            public void Add(Func<Type, TypeDescriptor> resolve)
            {
                nameResolvers.Add(resolve);
            }

            public void Add(Func<TypeDescriptor, Type> resolve)
            {
                typeResolvers.Add(resolve);
            }
        }

        /// <summary>
        /// Adds the specified function to the pipeline of resolvers that can get a <see cref="TypeDescriptor"/>
        /// from a .NET type. If the function returns null, it means that it doesn't care and the next resulver
        /// will be called, until ultimately it will fall back to default JSON.NET behavior
        /// </summary>
        public void AddNameResolver(Func<Type, TypeDescriptor> resolver)
        {
            binder.Add(resolver);
        }

        /// <summary>
        /// Adds the specified function to the pipeline of resolvers that can get a .NET type from a
        /// <see cref="TypeDescriptor"/>. If the function returns null, it means that it doesn't care and the next resulver
        /// will be called, until ultimately it will fall back to default JSON.NET behavior
        /// </summary>
        public void AddTypeResolver(Func<TypeDescriptor, Type> resolver)
        {
            binder.Add(resolver);
        }
    }
}