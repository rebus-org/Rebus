using System.Reflection;
using System.Linq;
using Rebus.Logging;

namespace Rebus.Configuration.Configurers
{
    public class DiscoveryConfigurer
    {
        IContainerAdapter containerAdapter;

        readonly HandlerLoader handlerLoader;

        public DiscoveryConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
            handlerLoader = new HandlerLoader(containerAdapter);
        }

        public HandlerLoader Handlers
        {
            get { return handlerLoader; }
        }
    }

    public class HandlerLoader
    {
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly IContainerAdapter containerAdapter;

        public HandlerLoader(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
        }

        public void LoadFrom(Assembly assemblyToScan, params Assembly[] additionalAssemblies)
        {
            Log.Debug("Loading handlers");

            var assembliesToScan = new[] { assemblyToScan }.Concat(additionalAssemblies);

            foreach(var assembly in assembliesToScan)
            {
                Log.Debug("Scanning {0}", assembly);

                RegisterHandlersFrom(assembly);
            }
        }

        void RegisterHandlersFrom(Assembly assembly)
        {
            var messageHandlers = assembly.GetTypes()
                .Select(t => new
                                 {
                                     Type = t,
                                     HandlerInterfaces = t.GetInterfaces()
                                 .Where(i => i.IsGenericType
                                             && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>))
                                 })
                .Where(a => a.HandlerInterfaces.Any())
                .SelectMany(a => a.HandlerInterfaces.Select(i => new
                                                                     {
                                                                         Service = i,
                                                                         Implementation = a.Type,
                                                                     }));

            foreach(var handler in messageHandlers)
            {
                Log.Debug("Registering handler {0} -> {1}", handler.Implementation, handler.Service);

                containerAdapter.Register(handler.Implementation, Lifestyle.Instance, handler.Service);
            }
        }
    }
}