using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
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
            .Options(o => UseNewRetryStrategy(o))
            .Start();

    }

    [Test]
    public async Task CanDeadletterMessage()
    {
        var network = new InMemNetwork();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<MyPoisonousMessage>(async _ => throw new ArgumentException("fail!"));

        var bus = Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel))
            .Transport(t => t.UseInMemoryTransport(network, "deadlettering"))
            .Options(o => UseNewRetryStrategy(o))
            .Start();

        await bus.SendLocal(new MyPoisonousMessage(), new Dictionary<string, string> { ["iknowu"] = "" });

        var transportMessage = await network.WaitForNextMessageFrom("error");
        Assert.That(transportMessage.Headers, Contains.Key("iknowu"));
    }

    [Test]
    public async Task CanDeadletterMessage_DoesNotSendOutgoingMessages()
    {
        var network = new InMemNetwork();

        network.CreateQueue("sent-from-handler");

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<MyPoisonousMessage>(async (bus, _, __) =>
        {
            await bus.Send(new MessageSentFromHandler());
            throw new ArgumentException("fail!");
        });

        var bus = Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel))
            .Transport(t => t.UseInMemoryTransport(network, "deadlettering"))
            .Routing(r => r.TypeBased().Map<MessageSentFromHandler>("sent-from-handler"))
            .Options(o => UseNewRetryStrategy(o))
            .Start();

        await bus.SendLocal(new MyPoisonousMessage(), new Dictionary<string, string> { ["iknowu"] = "" });

        var transportMessage = await network.WaitForNextMessageFrom("error");
        Assert.That(transportMessage.Headers, Contains.Key("iknowu"));

        Assert.That(network.GetCount("sent-from-handler"), Is.EqualTo(0));
    }

    [Test]
    public async Task CanInvokeSecondLevelRetryHandler()
    {
        var network = new InMemNetwork();

        using var activator = new BuiltinHandlerActivator();
        using var secondLevelHandlerInvoked = new ManualResetEvent(initialState: false);

        activator.Handle<MyPoisonousMessage>(async _ => throw new ArgumentException("fail!"));
        activator.Handle<IFailed<MyPoisonousMessage>>(async _ => secondLevelHandlerInvoked.Set());

        var bus = Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel))
            .Transport(t => t.UseInMemoryTransport(network, "deadlettering"))
            .Options(configurer => UseNewRetryStrategy(configurer, secondLevelRetriesEnabled: true))
            .Start();

        await bus.SendLocal(new MyPoisonousMessage(), new Dictionary<string, string> { ["iknowu"] = "" });

        secondLevelHandlerInvoked.WaitOrDie(
            timeout: TimeSpan.FromSeconds(30),
            errorMessage: "The failing MyPoisonousMessage message was not dispatched as IFailed<MyPoisonousMessage> within 3 s"
        );
    }


    void UseNewRetryStrategy(OptionsConfigurer configurer, bool secondLevelRetriesEnabled = false)
    {
        configurer
            .Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();

                var remover = new PipelineStepRemover(pipeline)
                    .RemoveIncomingStep(s => s is SimpleRetryStrategyStep);

                var step = new DefaultRetryStrategyStep(
                    rebusLoggerFactory: c.Get<IRebusLoggerFactory>(),
                    errorHandler: c.Get<IErrorHandler>(),
                    errorTracker: c.Get<IErrorTracker>(),
                    secondLevelRetriesEnabled: secondLevelRetriesEnabled,
                    cancellationToken: c.Get<CancellationToken>()
                );

                var concatenator = new PipelineStepConcatenator(remover)
                    .OnReceive(step, PipelineAbsolutePosition.Front);

                if (secondLevelRetriesEnabled)
                {
                    var incomingStep = new FailedMessageWrapperStep(c.Get<IErrorTracker>());
                    var outgoingStep = new VerifyCannotSendFailedMessageWrapperStep();

                    return new PipelineStepInjector(concatenator)
                        .OnReceive(incomingStep, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep))
                        .OnSend(outgoingStep, PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));
                }

                return concatenator;
            });

        configurer.LogPipeline(verbose: true);
    }

    record MyPoisonousMessage;

    record MessageSentFromHandler;
}