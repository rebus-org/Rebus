using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Serialization;

[TestFixture]
public class TestJsonTypeNameHandlingModes : FixtureBase
{
    [Test]
    public async Task CanSerializeWithTypeNames()
    {
        var serializer = SnatchTheSerializer(JsonInteroperabilityMode.FullTypeInformation);
        var jsonText = await GetJsonText(serializer, new SomeRandomMessage("hej med dig min ven"));

        Console.WriteLine($@"


{jsonText}


");

        Assert.That(jsonText, Contains.Substring(typeof(SomeRandomMessage).GetSimpleAssemblyQualifiedName()));
    }

    [Test]
    public async Task CanSerializeWithoutTypeNames()
    {
        var serializer = SnatchTheSerializer(JsonInteroperabilityMode.PureJson);
        var jsonText = await GetJsonText(serializer, new SomeRandomMessage("hej med dig min ven"));

        Console.WriteLine($@"


{jsonText}


");

        Assert.That(jsonText, Does.Not.Contain(typeof(SomeRandomMessage).GetSimpleAssemblyQualifiedName()));
    }

    static async Task<string> GetJsonText(ISerializer serializer, SomeRandomMessage body)
    {
        var headers = new Dictionary<string, string>();
        var message = new Message(headers, body);
        var transportMessage = await serializer.Serialize(message);
        var jsonText = Encoding.UTF8.GetString(transportMessage.Body);
        return jsonText;
    }

    class SomeRandomMessage
    {
        public SomeRandomMessage(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    ISerializer SnatchTheSerializer(JsonInteroperabilityMode mode)
    {
        ISerializer serializerToReturn = null;

        Configure.With(Using(new BuiltinHandlerActivator()))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who cares"))
            .Serialization(s => s.UseNewtonsoftJson(mode))
            .Options(o =>
            {
                o.Decorate(c =>
                {
                    var serializer = c.Get<ISerializer>();
                    serializerToReturn = serializer;
                    return serializer;
                });
            })
            .Start();

        return serializerToReturn;
    }
}