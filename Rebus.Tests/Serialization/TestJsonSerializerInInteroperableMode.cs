using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization.Custom;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Serialization;

[TestFixture]
public class TestJsonSerializerInInteroperableMode : FixtureBase
{
    [Test]
    public async Task WorksWithCustomizedTypeName_Emoji2()
    {
        var activator = Using(new BuiltinHandlerActivator());
        var gotString = new ManualResetEvent(initialState: false);
        var serializedTypeName = "<not-set>";

        activator.Handle<string>(async (bus, context, str) =>
        {
            serializedTypeName = context.Headers.GetValue(Headers.Type);
            Console.WriteLine($"THE SERIALIZED TYPE NAME WAS '{serializedTypeName}'");
            gotString.Set();
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Serialization(s =>
            {
                s.UseCustomMessageTypeNames()
                    .AddWithCustomName<string>("😈");
            })
            .Start();

        await activator.Bus.SendLocal("hej 🥓");

        gotString.WaitOrDie(TimeSpan.FromSeconds(2));

        Assert.That(serializedTypeName, Is.EqualTo("😈"));
    }

    [Test]
    public async Task WorksWithCustomizedTypeName_String()
    {
        var activator = Using(new BuiltinHandlerActivator());
        var gotString = new ManualResetEvent(initialState: false);
        var serializedTypeName = "<not-set>";

        activator.Handle<string>(async (bus, context, str) =>
        {
            serializedTypeName = context.Headers.GetValue(Headers.Type);
            Console.WriteLine($"THE SERIALIZED TYPE NAME WAS '{serializedTypeName}'");
            gotString.Set();
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Serialization(s => s.UseCustomMessageTypeNames()
                .AddWithShortNames(new[] { typeof(string) }))
            .Start();

        await activator.Bus.SendLocal("hej 🥓");

        gotString.WaitOrDie(TimeSpan.FromSeconds(2));

        Assert.That(serializedTypeName, Is.EqualTo("String"));
    }

    [Test]
    public async Task WorksWithCustomizedTypeName_MyLittleMessage()
    {
        var activator = Using(new BuiltinHandlerActivator());
        var gotString = new ManualResetEvent(initialState: false);
        var serializedTypeName = "<not-set>";

        activator.Handle<MyLittleMessage>(async (bus, context, str) =>
        {
            serializedTypeName = context.Headers.GetValue(Headers.Type);
            Console.WriteLine($"THE SERIALIZED TYPE NAME WAS '{serializedTypeName}'");
            gotString.Set();
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "whatever"))
            .Serialization(s => s.UseCustomMessageTypeNames()
                .AddWithShortNames(new[] { typeof(MyLittleMessage) }))
            .Start();

        await activator.Bus.SendLocal(new MyLittleMessage());

        gotString.WaitOrDie(TimeSpan.FromSeconds(2));

        Assert.That(serializedTypeName, Is.EqualTo("MyLittleMessage"));
    }
}

class MyLittleMessage { }