using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998

namespace Rebus.Tests.Examples;

[TestFixture]
[Description("Example on how C# generics can make installing a custom exception handling pipeline step nifty")]
public class AddExceptionHandlerMiddlewareIntoIncomingPipeline : FixtureBase
{
    [Test]
    public async Task ThisIsHowYouDoIt()
    {
        using var gotTheOome = new ManualResetEvent(initialState: false);
        using var gotTheAeWithOhNo = new ManualResetEvent(initialState: false);

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<ThrowException>(async msg => throw (msg.ExceptionTypeName switch
        {
            nameof(OutOfMemoryException) => new OutOfMemoryException(msg.Message),

            nameof(ArgumentException) => new ArgumentException(msg.Message),

            _ => throw new ArgumentOutOfRangeException(nameof(ThrowException.ExceptionTypeName), "unknown exception type")
        }));

        var network = new InMemNetwork();

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "doesn't matter"))
            .Options(o =>
            {
                o.SetMaxParallelism(1);

                // handle OOME
                o.HandleException<OutOfMemoryException>(async (_, _) => gotTheOome.Set());

                // handle AE when the message is what we expect it to be
                o.HandleException<ArgumentException>(async (_, _) => gotTheAeWithOhNo.Set(), when: e => e.Message.Contains("oh no!"));
            })
            .Start();

        await bus.SendLocal(new ThrowException(nameof(ArgumentException), Message: "this one should end up in the error queue"));
        await bus.SendLocal(new ThrowException(nameof(OutOfMemoryException)));
        await bus.SendLocal(new ThrowException(nameof(ArgumentException)));

        gotTheOome.WaitOrDie(TimeSpan.FromSeconds(2));
        gotTheAeWithOhNo.WaitOrDie(TimeSpan.FromSeconds(2));

        var failedMessage = await network.WaitForNextMessageFrom("error");

        var json = Encoding.UTF8.GetString(failedMessage.Body);
        var msg = JsonSerializer.Deserialize<ThrowException>(json);
        Assert.That(msg.ExceptionTypeName, Is.EqualTo(nameof(ArgumentException)));
        Assert.That(msg.Message, Is.EqualTo("this one should end up in the error queue"));
    }

    record ThrowException(string ExceptionTypeName, string Message = "oh no!");
}

static class MyCustomExceptionHandlerConfigurationExtensions
{
    public static void HandleException<TException>(this OptionsConfigurer configurer,
        Func<IBus, IMessageContext, Task> handler,
        Func<TException, bool> when = null) where TException : Exception
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();
            var step = new ExceptionHandlerIncomingStep<TException>(handler, when ?? (_ => true), new Lazy<IBus>(c.Get<IBus>));
            return new PipelineStepInjector(pipeline)
                .OnReceive(step, PipelineRelativePosition.Before, typeof(DispatchIncomingMessageStep));
        });
    }

    class ExceptionHandlerIncomingStep<TException> : IIncomingStep where TException : Exception
    {
        readonly Func<IBus, IMessageContext, Task> _handler;
        readonly Func<TException, bool> _when;
        readonly Lazy<IBus> _lazyBus;

        public ExceptionHandlerIncomingStep(Func<IBus, IMessageContext, Task> handler, Func<TException, bool> when, Lazy<IBus> lazyBus)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _when = when ?? throw new ArgumentNullException(nameof(when));
            _lazyBus = lazyBus ?? throw new ArgumentNullException(nameof(lazyBus));
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (TException exception) when (_when(exception))
            {
                await _handler(_lazyBus.Value, MessageContext.Current);
            }
        }
    }
}