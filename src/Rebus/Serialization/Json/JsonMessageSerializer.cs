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
    public class JsonMessageSerializer : ISerializeMessages
    {
        static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        static readonly CultureInfo SerializationCulture = CultureInfo.InvariantCulture;

        static readonly Encoding Encoding = Encoding.UTF7;
        readonly NonDefaultSerializationBinder binder;

        public JsonMessageSerializer()
        {
            binder = new NonDefaultSerializationBinder();
            Settings.Binder = binder;
        }

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

        public class NonDefaultSerializationBinder : DefaultSerializationBinder
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

        public void AddNameResolver(Func<Type, TypeDescriptor> resolver)
        {
            binder.Add(resolver);
        }

        public void AddTypeResolver(Func<TypeDescriptor, Type> resolver)
        {
            binder.Add(resolver);
        }
    }
}