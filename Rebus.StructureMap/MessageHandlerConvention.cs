using System;
using System.Linq;
using Rebus.Handlers;
using StructureMap.Graph;
using StructureMap.Graph.Scanning;
using StructureMap.Pipeline;
using StructureMap.TypeRules;

namespace Rebus.StructureMap
{
    /// <summary>
    /// StructureMap <see cref="IRegistrationConvention"/> that registers found Rebus handlers in the container
    /// </summary>
    public class MessageHandlerConvention : IRegistrationConvention
    {
        /// <summary>
        /// Registers found Rebus handler types in the container
        /// </summary>
        public void ScanTypes(TypeSet types, global::StructureMap.Registry registry)
        {
            var messageHandlers = types.FindTypes(TypeClassification.Concretes)
                .Where(t => t.CanBeCastTo(typeof(IHandleMessages)));

            foreach (var handlerType in messageHandlers)
            {
                var handlerInterfaces = handlerType.GetInterfaces().Where(IsHandler).ToList();

                foreach (var handlerInterface in handlerInterfaces)
                {
                    registry
                        .For(handlerInterface)
                        .Use(handlerType)
                        .LifecycleIs<UniquePerRequestLifecycle>();
                }
            }
        }

        static bool IsHandler(Type type)
        {
            return type.IsGenericType
                   && type.GetGenericTypeDefinition() == typeof(IHandleMessages<>);
        }
    }
}
