using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.Tests.Deadlettering;

[TestFixture]
public class TestDeadlettering : FixtureBase
{
    const LogLevel LogLevel = Rebus.Logging.LogLevel.Error;

    [Test]
    public async Task PrintStepPipeline()
    {
        using var activator = new BuiltinHandlerActivator();

        Configure.With(activator)
            .Logging(l => l.Console())
            .Transport(t => t.UseInMemoryTransport(new(), "deadlettering"))
            .Options(UseNewRetryStrategy)
            .Start();

    }

    [Test]
    public async Task CanDeadletterMessage()
    {
        var network = new InMemNetwork();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<PoisonMessage>(async _ => throw new ArgumentException("fail!"));

        var bus = Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel))
            .Transport(t => t.UseInMemoryTransport(network, "deadlettering"))
            .Options(UseNewRetryStrategy)
            .Start();

        await bus.SendLocal(new PoisonMessage(), new Dictionary<string, string> { ["iknowu"] = "" });

        var transportMessage = await network.WaitForNextMessageFrom("error");
        Assert.That(transportMessage.Headers, Contains.Key("iknowu"));
    }

    [Test]
    public async Task CanDeadletterMessage_DoesNotSendOutgoingMessages()
    {
        var network = new InMemNetwork();

        network.CreateQueue("sent-from-handler");

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<PoisonMessage>(async (bus, _, __) =>
        {
            await bus.Send(new MessageSentFromHandler());
            throw new ArgumentException("fail!");
        });

        var bus = Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel))
            .Transport(t => t.UseInMemoryTransport(network, "deadlettering"))
            .Routing(r => r.TypeBased().Map<MessageSentFromHandler>("sent-from-handler"))
            .Options(UseNewRetryStrategy)
            .Start();

        await bus.SendLocal(new PoisonMessage(), new Dictionary<string, string> { ["iknowu"] = "" });

        var transportMessage = await network.WaitForNextMessageFrom("error");
        Assert.That(transportMessage.Headers, Contains.Key("iknowu"));

        Assert.That(network.GetCount("sent-from-handler"), Is.EqualTo(0));
    }

    [Test]
    public async Task CanInvokeSecondLevelRetryHandler()
    {
        var network = new InMemNetwork();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<PoisonMessage>(async _ => throw new ArgumentException("fail!"));

        var bus = Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel))
            .Transport(t => t.UseInMemoryTransport(network, "deadlettering"))
            .Options(UseNewRetryStrategy)
            .Start();

        await bus.SendLocal(new PoisonMessage(), new Dictionary<string, string> { ["iknowu"] = "" });

        var transportMessage = await network.WaitForNextMessageFrom("error");
        Assert.That(transportMessage.Headers, Contains.Key("iknowu"));
    }


    void UseNewRetryStrategy(OptionsConfigurer configurer)
    {
        configurer
            .Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();

                var removeOriginalRetryStrategyStep = new PipelineStepRemover(pipeline)
                    .RemoveIncomingStep(s => s is SimpleRetryStrategyStep);

                return new PipelineStepConcatenator(removeOriginalRetryStrategyStep)
                    .OnReceive(new DefaultRetryStrategyStep(c.Get<IErrorHandler>(), c.Get<IErrorTracker>(), c.Get<CancellationToken>()), PipelineAbsolutePosition.Front);
            });

        configurer.LogPipeline(verbose: true);
    }

    record PoisonMessage;

    record MessageSentFromHandler;
}