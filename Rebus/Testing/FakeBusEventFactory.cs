using System;
using System.Linq;
using System.Reflection;
using Rebus.Testing.Events;

namespace Rebus.Testing
{
    class FakeBusEventFactory
    {
        public TEvent CreateEventGeneric<TEvent>(Type openGeneric, Type closingType, params object[] args) where TEvent : FakeBusEvent
        {
            var eventType = CloseEventType(openGeneric, closingType);
            var constructor = GetConstructor(eventType);
            var instance = CreateInstance(constructor, args);
            try
            {
                return (TEvent)instance;
            }
            catch (Exception exception)
            {
                throw new InvalidCastException($"Could not turn created instance {instance} into a {typeof(TEvent)}", exception);
            }
        }

        static object CreateInstance(ConstructorInfo constructor, object[] args)
        {
            try
            {
                return constructor.Invoke(args);
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Invocation of constructor with signature ({string.Join(", ", constructor.GetParameters().Select(p => p.ParameterType))}) failed with args ({string.Join(", ", args)})", exception);
            }
        }

        static ConstructorInfo GetConstructor(Type eventType)
        {
#if NETSTANDARD1_3
            // TODO: BindingFlags.CreateInstance is not available on .NET Standard 1.0-1.4
            // Should we care about it? Will it hurt us?
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
#else
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance;
#endif

            var constructor = eventType.GetConstructors(flags).FirstOrDefault();
            if (constructor != null)
            {
                return constructor;
            }

            throw new InvalidOperationException($"Could not find (non-public, instance-, create-instance-) constructor on {eventType}");
        }

        static Type CloseEventType(Type openGeneric, Type closingType)
        {
            try
            {
                return openGeneric.MakeGenericType(closingType);
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Could not close {openGeneric} with {closingType}", exception);
            }
        }
    }
}