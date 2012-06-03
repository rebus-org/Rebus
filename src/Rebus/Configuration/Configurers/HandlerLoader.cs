using System;
using System.Linq;
using System.Reflection;
using Rebus.Logging;

namespace Rebus.Configuration.Configurers
{
    public class HandlerLoader
    {
        static ILog log;

        static HandlerLoader()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly IContainerAdapter containerAdapter;

        public HandlerLoader(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public HandlerLoader LoadFrom(Assembly assemblyToScan, params Assembly[] additionalAssemblies)
        {
            return LoadFrom(t => true, assemblyToScan, additionalAssemblies);
        }

        public HandlerLoader LoadFrom(Predicate<Type> shouldRegisterType, Assembly assemblyToScan, params Assembly[] additionalAssemblies)
        {
            log.Debug("Loading handlers");

            var assembliesToScan = new[] { assemblyToScan }.Union(additionalAssemblies);

            foreach(var assembly in assembliesToScan)
            {
                log.Debug("Scanning {0}", assembly);

                RegisterHandlersFrom(assembly, shouldRegisterType);
            }

            return this;
        }

        void RegisterHandlersFrom(Assembly assembly, Predicate<Type> predicate)
        {
            var messageHandlers = assembly.GetTypes()
                .Select(t => new
                                 {
                                     Type = t,
                                     HandlerInterfaces = t.GetInterfaces().Where(IsHandler)
                                 })
                .Where(a => a.HandlerInterfaces.Any())
                .Where(a => predicate(a.Type))
                .SelectMany(a => a.HandlerInterfaces
                                     .Select(i => new
                                                      {
                                                          Service = i,
                                                          Implementation = a.Type,
                                                      }));

            foreach(var handler in messageHandlers)
            {
                log.Debug("Registering handler {0} -> {1}", handler.Implementation, handler.Service);

                containerAdapter.Register(handler.Implementation, Lifestyle.Instance, handler.Service);
            }
        }

        static bool IsHandler(Type i)
        {
            return i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>);
        }
    }
}