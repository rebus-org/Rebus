using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.Tests.Examples;

[TestFixture]
[Description("Demonstrates how Rebus can be extended to add a retry count header to each incoming message")]
public class AddRetryCountHeaderExample : FixtureBase
{
    [Test]
    public async Task CanRevealNumberOfRetries()
    {
        var retryCounts = new ConcurrentQueue<int>();
        var activator = Using(new BuiltinHandlerActivator());

        // create random header key every time to verify that it is actually used
        var headerKey = $"retry-count/{Guid.NewGuid()}";

        activator.Handle<Fail>(async (_, context, _) =>
        {
            Console.WriteLine($@"Got message {context.TransportMessage.GetMessageId()}:
{string.Join(Environment.NewLine, context.Headers.Select(kvp => $"    {kvp.Key}: {kvp.Value}"))}");

            retryCounts.Enqueue(int.Parse(context.Headers.GetValue(headerKey)));

            throw new Exception("oh no!");
        });

        var network = new InMemNetwork();

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "who-cares"))
            .Options(o => o.AddRetryCountHeader(key: headerKey))
            .Options(o => o.LogPipeline(verbose: true))
            .Start();

        // send message that will fail
        await bus.SendLocal(new Fail());

        // wait until the message is dead-lettered
        _ = await network.WaitForNextMessageFrom("error");

        Assert.That(retryCounts.Count, Is.EqualTo(5));
        Assert.That(retryCounts.ToArray(), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
    }

    class Fail { }
}

/// <summary>
/// Extension method to configure Rebus to add an artifial retry count header to incoming messages
/// </summary>
static class RetryCountHeaderExtensions
{
    /// <summary>
    /// Adds an artificial header with retry count under the key specified by <paramref name="key"/> to each message being handled.
    /// It counts the number of tracked exceptions for the message, meaning that it will 0 on the initial delivery attempt.
    /// </summary>
    public static void AddRetryCountHeader(this OptionsConfigurer configurer, string key = "retry-count")
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (key == null) throw new ArgumentNullException(nameof(key));

        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();
            var errorTracker = c.Get<IErrorTracker>();

            var step = new AddRetryInfoHeaderStep(errorTracker, key);

            return new PipelineStepInjector(pipeline)
                .OnReceive(step, PipelineRelativePosition.After, typeof(DefaultRetryStrategyStep));
        });
    }

    [StepDocumentation("Loads the transport message, counts the number of exceptions tracked for the message ID, and the adds an artificial header to the transport message with the retry count.")]
    class AddRetryInfoHeaderStep : IIncomingStep
    {
        readonly IErrorTracker _errorTracker;
        readonly string _key;

        public AddRetryInfoHeaderStep(IErrorTracker errorTracker, string key)
        {
            _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<TransportMessage>();
            var messageId = message.GetMessageId();
            var exceptionCount = (await _errorTracker.GetExceptions(messageId)).Count;

            message.Headers[_key] = exceptionCount.ToString(CultureInfo.InvariantCulture);

            await next();
        }
    }
}