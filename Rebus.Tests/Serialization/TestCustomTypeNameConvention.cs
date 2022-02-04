using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization.Custom;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable CS1998

namespace Rebus.Tests.Serialization;

[TestFixture]
public class TestCustomTypeNameConvention : FixtureBase
{
    [Test]
    public void CheckPrettyErrors()
    {
        var convention = new CustomTypeNameConventionBuilder().GetConvention();

        Console.WriteLine(
            Assert.Throws<ArgumentException>(() => convention.GetTypeName(typeof(string)))
        );

        Console.WriteLine(
            Assert.Throws<ArgumentException>(() => convention.GetType("string"))
        );
    }

    [Test]
    public async Task IntegrationTest()
    {
        using var counter = new SharedCounter(initialValue: 3);

        var typeNames = new ConcurrentDictionary<Type, string>();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<object>(async (_, context, message) =>
        {
            typeNames[message.GetType()] = context.Headers.GetValue(Headers.Type);
            counter.Decrement();
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Serialization(s => s.UseCustomMessageTypeNames()
                .AddWithCustomName<FirstMessage>("FourthMessage") //< confuse the enemy!
                .AddWithShortName<SecondMessage>()
                .AllowFallbackToDefaultConvention())
            .Start();

        await activator.Bus.SendLocal(new FirstMessage());
        await activator.Bus.SendLocal(new SecondMessage());
        await activator.Bus.SendLocal(new ThirdMessage());

        counter.WaitForResetEvent();

        Assert.That(typeNames[typeof(FirstMessage)], Is.EqualTo("FourthMessage"));
        Assert.That(typeNames[typeof(SecondMessage)], Is.EqualTo("SecondMessage"));
        Assert.That(typeNames[typeof(ThirdMessage)], Is.EqualTo(typeof(ThirdMessage).GetSimpleAssemblyQualifiedName()));
    }

    record FirstMessage;

    record SecondMessage;

    record ThirdMessage;
}