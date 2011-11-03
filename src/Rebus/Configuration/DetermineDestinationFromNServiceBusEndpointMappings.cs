using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;

namespace Rebus.Configuration
{
    /// <summary>
    /// Implementation of <see cref="IDetermineDestination"/> that queries the specified XML document
    /// (loaded from as assembly configuration file or web.config) for endpoint mappings specified on
    /// the format that NServiceBus understands.
    /// 
    /// Please note that <see cref="DetermineDestinationFromNServiceBusEndpointMappings"/> is a tad more
    /// tolerant than NServiceBus - it does not require that the UnicastBusConfig configuration section
    /// is declared, even though that is required for NServiceBus to work.
    /// 
    /// If you for some reason have a configuration section with that name that belongs to another
    /// framework, then that might be a problem.
    /// 
    /// If you want to alter the behavior of this implementation, feel free to subclass it and override
    /// any methods that you see fit.
    /// </summary>
    public class DetermineDestinationFromNServiceBusEndpointMappings : IDetermineDestination
    {
        readonly ConcurrentDictionary<Type, string> endpoints = new ConcurrentDictionary<Type, string>();
        readonly IAppConfigLoader appConfigLoader;
        readonly object initializationLock = new object();

        volatile bool initialized;

        public DetermineDestinationFromNServiceBusEndpointMappings(IAppConfigLoader appConfigLoader)
        {
            this.appConfigLoader = appConfigLoader;
        }

        public virtual string GetEndpointFor(Type messageType)
        {
            if (!initialized)
            {
                Initialize();
            }

            string temp;
            if (endpoints.TryGetValue(messageType, out temp))
                return temp;

            return null;
        }

        protected virtual void Initialize()
        {
            lock (initializationLock)
            {
                if (initialized) return;

                DoInitialize();

                initialized = true;
            }
        }

        protected virtual void DoInitialize()
        {
            var xmlText = appConfigLoader.LoadIt();
            var doc = XDocument.Parse(xmlText);

            var configurationElement = doc.Element(XName.Get("configuration"));
            if (configurationElement == null) return;

            var unicastBusConfigElement = configurationElement.Element(XName.Get("UnicastBusConfig"));
            if (unicastBusConfigElement == null) return;

            var endpointMappingsElement = unicastBusConfigElement.Element(XName.Get("MessageEndpointMappings"));

            if (endpointMappingsElement == null) return;

            foreach (var mapping in endpointMappingsElement.Descendants())
            {
                ExtractMapping(mapping);
            }
        }

        protected virtual void ExtractMapping(XElement mapping)
        {
            if (mapping.Name != "add")
            {
                throw new ConfigurationFileFormatException("Element with name '{0}' found - expected 'add'", mapping.Name);
            }

            var messagesAttribute = mapping.Attribute(XName.Get("Messages"));

            if (messagesAttribute == null)
            {
                throw new ConfigurationFileFormatException(
                    @"'Messages' attribute of 'add' element could not be found: {0}",
                    mapping);
            }

            var endpointAttribute = mapping.Attribute(XName.Get("Endpoint"));

            if (endpointAttribute == null)
            {
                throw new ConfigurationFileFormatException(
                    @"'Endpoint' attribute of 'add' element could not be found: {0}",
                    mapping);
            }

            var messages = messagesAttribute.Value;
            var endpoint = endpointAttribute.Value;

            if (IsAssemblyName(messages))
            {
                AddMappingsForAllTypesFromAssemblyNamed(messages, endpoint);
            }
            else
            {
                AddMappingForType(messages, endpoint);
            }
        }

        protected virtual void AddMappingForType(string messages, string endpoint)
        {
            var type = Type.GetType(messages);

            Map(type, endpoint);
        }

        protected virtual void AddMappingsForAllTypesFromAssemblyNamed(string messages, string endpoint)
        {
            var assembly = Assembly.Load(messages);

            foreach (var type in assembly.GetTypes())
            {
                Map(type, endpoint);
            }
        }

        protected virtual void Map(Type type, string endpoint)
        {
            endpoints[type] = endpoint;
        }

        protected virtual bool IsAssemblyName(string messages)
        {
            return !messages.Contains(",");
        }
    }
}