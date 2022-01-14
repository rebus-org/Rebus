using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class CanDoReflection
{
    [Test, Ignore("played around with assembly-scanning")]
    public void YeahItWorks()
    {
        IBus bus = GetBus();

        var assemblyToScan = typeof(CanDoReflection).GetTypeInfo().Assembly;

        var handledMessageTypes = assemblyToScan
            .GetTypes()
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>))
                .Select(i => i.GetGenericArguments().Single()))
            .Distinct()
            .ToList();

        foreach (var messageType in handledMessageTypes)
        {
            bus.Advanced.Topics.Subscribe(messageType.GetSimpleAssemblyQualifiedName());
        }
    }

    IBus GetBus()
    {
        throw new NotImplementedException();
    }
}