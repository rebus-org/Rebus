using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Tried to reproduce odd missing header situation. Could not replicate though, which makes it look like things are working as they should")]
public class CheckCompatibilityOfJsonSerializers : FixtureBase
{
    BuiltinHandlerActivator _systemJsonActivator;
    BuiltinHandlerActivator _newtonsoftJsonActivator;
    ManualResetEvent _systemJsonReceivedEvent;
    ManualResetEvent _newtonsoftJsonReceivedEvent;

    protected override void SetUp()
    {
        base.SetUp();

        _systemJsonReceivedEvent = Using(new ManualResetEvent(initialState: false));
        _systemJsonActivator = Using(new BuiltinHandlerActivator());
        
        _systemJsonActivator.Handle<SomeKindOfMessage>(async message =>
        {
            if (message.Text != "hej System.Json!") return; //< verify message has the data

            _systemJsonReceivedEvent.Set();
        });

        var network = new InMemNetwork();

        Configure.With(_systemJsonActivator)
            .Transport(t => t.UseInMemoryTransport(network, "system.json"))
            .Serialization(s => s.UseSystemTextJson(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }))
            .Routing(r => r.TypeBased().Map<SomeKindOfMessage>("newtonsoft.json"))
            .Start();

        _newtonsoftJsonActivator = Using(new BuiltinHandlerActivator());
        _newtonsoftJsonReceivedEvent = Using(new ManualResetEvent(initialState: false));
        
        _newtonsoftJsonActivator.Handle<SomeKindOfMessage>(async message =>
        {
            if (message.Text != "hej Newtonsoft.Json!") return; //< verify message has the data

            _newtonsoftJsonReceivedEvent.Set();
        });

        Configure.With(_newtonsoftJsonActivator)
            .Transport(t => t.UseInMemoryTransport(network, "newtonsoft.json"))
            .Serialization(s => s.UseNewtonsoftJson(new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        OverrideSpecifiedNames = false
                    }
                }
            }))
            .Routing(r => r.TypeBased().Map<SomeKindOfMessage>("system.json"))
            .Start();
    }

    [Test]
    public async Task CanSendFromNewtonsoftJsonToSystemJson()
    {
        await _newtonsoftJsonActivator.Bus.Send(new SomeKindOfMessage("hej System.Json!"));

        _systemJsonReceivedEvent.WaitOrDie(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CanSendFromSystemJsonToNewtonsoftJson()
    {
        await _systemJsonActivator.Bus.Send(new SomeKindOfMessage("hej Newtonsoft.Json!"));

        _newtonsoftJsonReceivedEvent.WaitOrDie(TimeSpan.FromSeconds(5));
    }

    record SomeKindOfMessage(string Text);
}