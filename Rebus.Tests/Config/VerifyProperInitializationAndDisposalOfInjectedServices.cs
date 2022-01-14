using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Injection;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Config;

[TestFixture]
public class VerifyProperInitializationAndDisposalOfInjectedServices : FixtureBase
{
    [Test]
    public void DecoratorsAreInitializedAndDisposedInTheCorrectOrder()
    {
        var events = new ConcurrentQueue<string>();

        using (var activator = new BuiltinHandlerActivator())
        {
            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                .Options(o =>
                {
                    o.Decorate<ITransport>(t => new InitializableDisposableLoggingDecorator("first", t.Get<ITransport>(), events));
                    o.Decorate<ITransport>(t => new InitializableDisposableLoggingDecorator("middle", t.Get<ITransport>(), events));
                    o.Decorate<ITransport>(t => new InitializableDisposableLoggingDecorator("last", t.Get<ITransport>(), events));
                })
                .Start();
        }

        Assert.That(events, Is.EqualTo(new[]
        {
            "Initializing first",
            "Initializing middle",
            "Initializing last",
            "Disposing last",
            "Disposing middle",
            "Disposing first",
        }));
    }

    class InitializableDisposableLoggingDecorator : IInitializable, IDisposable, ITransport
    {
        readonly string _description;
        readonly ITransport _transport;
        readonly ConcurrentQueue<string> _events;

        public InitializableDisposableLoggingDecorator(string description, ITransport transport, ConcurrentQueue<string> events)
        {
            _description = description;
            _transport = transport;
            _events = events;
        }

        public void Initialize() => WriteEvent($"Initializing {_description}");

        public void Dispose() => WriteEvent($"Disposing {_description}");

        void WriteEvent(string text)
        {
            Console.WriteLine($"Writing event text '{text}'");
            _events.Enqueue(text);
        }

        public void CreateQueue(string address) => _transport.CreateQueue(address);

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context) => _transport.Send(destinationAddress, message, context);

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken) => _transport.Receive(context, cancellationToken);

        public string Address => _transport.Address;
    }

    [Test]
    public void InitializedThingsAreAlwaysDisposed()
    {
        var first = new InitializableDisposableDecorator("first");
        var failing = new InitializableDisposableDecorator("failing", failDuringInitialization: true);
        var last = new InitializableDisposableDecorator("last");

        using (var activator = new BuiltinHandlerActivator())
        {
            var exception = Assert.Throws<ResolutionException>(() =>
            {
                Configure.With(activator)
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimse"))
                    .Options(o =>
                    {
                        o.Decorate<ITransport>(t =>
                        {
                            first.DecoratedTransport = t.Get<ITransport>();
                            return first;
                        });
                        o.Decorate<ITransport>(t =>
                        {
                            failing.DecoratedTransport = t.Get<ITransport>();
                            return failing;
                        });
                        o.Decorate<ITransport>(t =>
                        {
                            last.DecoratedTransport = t.Get<ITransport>();
                            return last;
                        });
                    })
                    .Start();
            });

            Console.WriteLine(exception);
        }

        Assert.That(first.WasInitialized, Is.True, "Expected that the first decorator would have been initialized");
        Assert.That(first.WasDisposed, Is.True, "Expected that the first decorator was disposed too, because it was initialized!!");

        Assert.That(failing.WasDisposed, Is.True, "Expected the failing decorator to have been disposed because it too was tracked and implements IDisposable");

        Assert.That(last.WasInitialized, Is.False, "Did NOT expect the last decorator to have been initialized, because the failing decorator was registered before this one");
        Assert.That(last.WasDisposed, Is.True, "Expected the last decorator to have been disposed too");
    }

    class InitializableDisposableDecorator : IInitializable, IDisposable, ITransport
    {
        readonly string _description;
        readonly bool _failDuringInitialization;

        public bool WasInitialized { get; private set; }
        public bool WasDisposed { get; private set; }

        public ITransport DecoratedTransport { get; set; }

        public InitializableDisposableDecorator(string description, bool failDuringInitialization = false)
        {
            _description = description;
            _failDuringInitialization = failDuringInitialization;
        }

        public void Initialize()
        {
            Console.WriteLine($"Initializing {_description}...");

            if (_failDuringInitialization)
            {
                Console.WriteLine("THrowing11111111!!!!!");
                throw new RebusApplicationException("I am a failure");
            }

            WasInitialized = true;

            Console.WriteLine($"{_description} was initialized");
        }

        public void Dispose()
        {
            Console.WriteLine($"Disposing {_description}...");

            WasDisposed = true;

            Console.WriteLine($"{_description} was disposed");
        }

        public void CreateQueue(string address) => DecoratedTransport?.CreateQueue(address);

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context) => DecoratedTransport.Send(destinationAddress, message, context);

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken) => DecoratedTransport.Receive(context, cancellationToken);

        public string Address => DecoratedTransport.Address;
    }
}