using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Extensions;

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestBuiltinHandlerActivatorExtensions
{
    [Test]
    public async Task TheExtensionWorks()
    {
        var activator = new BuiltinHandlerActivator();

        activator.Register(typeof(SomeHandler));

        using (var scope = new FakeMessageContextScope())
        {
            var stringHandlers = await activator.GetHandlers("hej", scope.TransactionContext);

            Assert.That(stringHandlers.Count(), Is.EqualTo(1));
        }
    }

    class SomeHandler : IHandleMessages<string>, IHandleMessages<int>
    {
        public Task Handle(string message)
        {
            throw new NotImplementedException();
        }

        public Task Handle(int message)
        {
            throw new NotImplementedException();
        }
    }

}

/// <summary>
/// Just a little experiment
/// </summary>
static class BuiltinHandlerActivatorExtensions
{
    public static void Register(this BuiltinHandlerActivator activator, Type handlerType)
    {
        var defaultConstructor = handlerType.GetConstructor(new Type[0])
                                 ?? throw new ArgumentException($"The type {handlerType} cannot be registered as a Rebus handler this way, because it does not have a default constructor");

        var implementedHandlerInterfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));

        var genericRegistrationMethod = typeof(BuiltinHandlerActivatorExtensions)
                                            .GetMethod(nameof(RegisterGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                                        ?? throw new ArgumentException($"Could not find {nameof(RegisterGeneric)} method on {typeof(BuiltinHandlerActivatorExtensions)}");

        foreach (var handlerInterface in implementedHandlerInterfaces)
        {
            genericRegistrationMethod
                .MakeGenericMethod(handlerInterface)
                .Invoke(null, new object[] { activator, defaultConstructor });
        }
    }

    static void RegisterGeneric<THandler>(BuiltinHandlerActivator activator, ConstructorInfo constructor) where THandler : IHandleMessages
    {
        activator.Register(() => (THandler)constructor.Invoke(new object[0]));
    }
}