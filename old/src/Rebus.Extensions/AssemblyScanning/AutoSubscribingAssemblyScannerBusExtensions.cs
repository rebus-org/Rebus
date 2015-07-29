using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rebus.Extensions.AssemblyScanning
{
    public static class AutoSubscribingAssemblyScannerBusExtensions
    {
        /// <summary>
        ///     Scans the assemblies supplied in <paramref name="assemblies" /> for handlers that implement
        ///     <see cref="IHandleMessages{TMessage}" /> and adds a subscription for the handled message types.
        /// </summary>
        public static void SubscribeByScanningForHandlers(this IBus bus, params Assembly[] assemblies)
        {
            var typesOfMessagesHandledByRebus = GetTypesOfMessagesHandledByRebus(assemblies);
            var subscribeMethod = bus.GetType().GetMethod("Subscribe", new Type[0]);

            foreach (var messageType in typesOfMessagesHandledByRebus)
            {
                subscribeMethod.MakeGenericMethod(messageType).Invoke(bus, new object[0]);
            }
        }

        static IEnumerable<Type> GetTypesOfMessagesHandledByRebus(IEnumerable<Assembly> assemblies)
        {
            return assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetInterfaces()
                    .Select(@interface => new
                    {
                        Type = type,
                        Interface = @interface
                    }))
                .Where(a => IsGenericRebusHandler(a.Interface))
                .Select(a =>
                {
                    var genericTypeArguments = a.Interface.GenericTypeArguments;

                    if (genericTypeArguments.Length != 1)
                    {
                        throw new ApplicationException(string.Format("The type {0} implements {1}, which was detected to be a Rebus message handler - but it has {2} generic type arguments which is not what we expected: {3}",
                            a.Type, a.Interface, genericTypeArguments.Length, string.Join(", ", genericTypeArguments.Select(t => t.FullName))));
                    }

                    return genericTypeArguments[0];
                })
                .Distinct();
        }

        static bool IsGenericRebusHandler(Type implementedInterface)
        {
            if (!implementedInterface.IsGenericType) return false;

            var genericTypeDefinition = implementedInterface.GetGenericTypeDefinition();

            var isOrdinaryMessageHandler = genericTypeDefinition == typeof(IHandleMessages<>);
            var isAsyncMessageHandler = genericTypeDefinition == typeof(IHandleMessagesAsync<>);

            return isOrdinaryMessageHandler || isAsyncMessageHandler;
        }
    }
}