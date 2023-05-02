using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
using static Rebus.Tests.Examples.AddInputQueueNameToIncomingStepContext;
#pragma warning disable CS4014
#pragma warning disable CS1998

namespace Rebus.Tests.Examples;

[TestFixture]
public class AddInputQueueNameToIncomingStepContext : FixtureBase
{
    [Test]
    public async Task DemonstratesHowTheInputQueueNameCanBeRetrieved()
    {
        var unguessableQueueName = Guid.NewGuid().ToString();
        var futureStrings = new ConcurrentQueue<string>();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<string>(async (_, messageContext, _) =>
        {
            var incomingStepContext = messageContext.IncomingStepContext;
            var inputQueueName = incomingStepContext.Load<InputQueueName>();
            futureStrings.Enqueue(inputQueueName?.QueueName);
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), unguessableQueueName))
            .Options(o => o.AddInputQueueNameToStepContext())
            .Start();

        await activator.Bus.SendLocal("HEJ");

        await futureStrings.WaitUntil(q => q.Count == 1);

        Assert.That(futureStrings.First(), Is.EqualTo(unguessableQueueName));
    }

    public record InputQueueName(string QueueName);
}

static class NiftyRebusConfigurationExtensions
{
    public static void AddInputQueueNameToStepContext(this OptionsConfigurer configurer)
    {
        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();
            var inputQueueName = c.Get<ITransport>().Address;
            var step = new AddInputQueueNameToPipelineStep(new(inputQueueName));

            return new PipelineStepInjector(pipeline)
                .OnReceive(step, PipelineRelativePosition.After, typeof(DefaultRetryStep));
        });
    }

    [StepDocumentation("Adds the bus' own input queue name to the incoming step context in the form of an InputQueueName object containing the queue name.")]
    class AddInputQueueNameToPipelineStep : IIncomingStep
    {
        readonly InputQueueName _inputQueueName;

        public AddInputQueueNameToPipelineStep(InputQueueName inputQueueName) => _inputQueueName = inputQueueName;

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            context.Save(_inputQueueName);

            await next();
        }
    }
}


