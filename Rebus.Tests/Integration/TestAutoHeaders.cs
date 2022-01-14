using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestAutoHeaders : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _bus;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _bus = Configure.With(_activator)
            .Logging(l => l.Console())
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "headers"))
            .Create();
    }

    [Test]
    public async Task AutomaticallyAssignsHeadersFromAttribute()
    {
        var headers = await GetHeaders(new SomeMessage());

        AssertHeader(headers, "greeting", "hej");
    }

    [Test]
    public async Task DoesNotOverwriteExistingHeaders()
    {
        var headers = await GetHeaders(new SomeMessage(), new Dictionary<string, string>
        {
            {"greeting", "customizzle!"}
        });

        AssertHeader(headers, "greeting", "customizzle!");
    }

    [Header("greeting", "hej")]
    class SomeMessage
    {

    }

    [Test]
    public async Task WorksWithCustomizedPrefabHeader()
    {
        var headers = await GetHeaders(new MessageWithPrefabHeader());

        AssertHeader(headers, "PREFAB", "");
    }

    [PrefabHeader]
    class MessageWithPrefabHeader { }

    class PrefabHeaderAttribute : HeaderAttribute
    {
        public PrefabHeaderAttribute()
            : base("PREFAB", "")
        {
        }
    }

    async Task<Dictionary<string, string>> GetHeaders(object message, Dictionary<string, string> optionalHeaders = null)
    {
        Dictionary<string, string> headers = null;
        var gotIt = new ManualResetEvent(false);
        
        _activator.Handle<object>(async (_, context, msg) =>
        {
            headers = context.TransportMessage.Headers;
            gotIt.Set();
        });

        var bus = _bus.Start();
        await bus.SendLocal(message, optionalHeaders);

        gotIt.WaitOrDie(TimeSpan.FromSeconds(1));

        return headers;
    }

    static void AssertHeader(IReadOnlyDictionary<string, string> headers, string expectedKey, string expectedValue)
    {
        Assert.That(headers, Is.Not.Null, "Did not get the headers at all!!");
        Assert.That(headers.ContainsKey(expectedKey), Is.True, "Headers did not have the '{0}' key", expectedKey);
        Assert.That(headers[expectedKey], Is.EqualTo(expectedValue));
    }
}