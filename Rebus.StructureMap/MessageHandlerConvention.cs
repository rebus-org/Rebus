using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Handlers;
using StructureMap.Graph;
using StructureMap.Graph.Scanning;
using StructureMap.Pipeline;
using StructureMap.TypeRules;

namespace Rebus.StructureMap
{
    public class MessageHandlerConvention : IRegistrationConvention
    {
        public void ScanTypes(TypeSet types, global::StructureMap.Registry registry)
        {
            var messageHandlers = types.FindTypes(TypeClassification.Concretes)
                .Where(t => t.CanBeCastTo(typeof(IHandleMessages)));
            foreach (var t in messageHandlers)
            {
                var handlers = t.GetInterfaces().Where(IsHandler).ToList();
                if (handlers.Any())
                {
                    foreach (var h in handlers)
                    {
                        registry.For(h).Use(t).LifecycleIs<UniquePerRequestLifecycle>();
                    }
                }
            }
        }

        private static bool IsHandler(Type i)
        {
            return i.IsGenericType &&
                   (i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
        }
    }
}
