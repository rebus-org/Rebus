using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Examples;

[TestFixture]
[Description("Demonstrates how the incoming pipeline can be used to expose Rebus' internal services, in this case its error tracker.")]
public class ExposeInternalService_ErrorTracker : FixtureBase
{
    [Test]
    public async Task CanExposeErrorTrackerViaIncomingStepContext()
    {
        var activator = Using(new BuiltinHandlerActivator());
        var done = Using(new ManualResetEvent(initialState: false));

        activator.Handle<string>(async (bus, context, message) =>
        {
            var errorTracker = context.IncomingStepContext.Load<IErrorTracker>();
            var messageId = context.Headers.GetValue(Headers.MessageId);

            var exceptions = errorTracker.GetExceptions(messageId);

            // do stuff with the exceptions
            done.Set();
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who-cares"))
            .Options(o => o.ExposeErrorTracker())
            .Start();

        await activator.Bus.SendLocal("HEJ");

        done.WaitOrDie(timeout: TimeSpan.FromSeconds(5));
    }
}

static class ExposeErrorTrackerExtensions
{
    public static void ExposeErrorTracker(this OptionsConfigurer configurer) =>
        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();
            var errorTracker = c.Get<IErrorTracker>();
            var step = new ExposeErrorTrackerStep(errorTracker);

            return new PipelineStepConcatenator(pipeline)
                .OnReceive(step, PipelineAbsolutePosition.Front);
        });

    [StepDocumentation("Makes Rebus' IErrorTracker available in the incoming step context.")]
    class ExposeErrorTrackerStep : IIncomingStep
    {
        readonly IErrorTracker _errorTracker;

        public ExposeErrorTrackerStep(IErrorTracker errorTracker) => _errorTracker = errorTracker;

        public Task Process(IncomingStepContext context, Func<Task> next)
        {
            context.Save(_errorTracker);
            return next();
        }
    }
}