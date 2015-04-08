using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Injection;

namespace Rebus.Tests.Injection
{
    [TestFixture]
    public class TestBusDisposal : FixtureBase
    {
        [Test]
        public void InjectedWhateverWithWhateverInsideIsProperlyDisposed()
        {
            var injectionist = new Injectionist();
            var eventTracker = new EventTracker();

            injectionist.Register(c =>
            {
                var fakeBus = new FakeBus(c.Get<Disposable1>(), c.Get<EventTracker>());

                fakeBus.FakeBusDisposed += c.DisposeTrackedInstances;

                return fakeBus;
            });
            injectionist.Register(c => new Disposable1(c.Get<Disposable2>(), c.Get<EventTracker>()));
            injectionist.Register(c => new Disposable2(c.Get<EventTracker>()));
            injectionist.Register(c => eventTracker);

            using (var bus = injectionist.Get<FakeBus>())
            {
                Console.WriteLine("Using the bus....");

                Console.WriteLine("Disposing it");
            }

            Console.WriteLine(@"Here's what happened:
{0}", string.Join(Environment.NewLine, eventTracker.Events.Select(e => "- " + e)));

            Assert.That(eventTracker.Events, Is.EqualTo(new[]
            {
                "Disposable2 disposed",
                "Disposable1 disposed",
                "FakeBus disposed",
            }));
        }

        class FakeBus : IDisposable
        {
            readonly EventTracker _tracker;

            public FakeBus(Disposable1 disposable1, EventTracker tracker)
            {
                _tracker = tracker;
            }

            public event Action FakeBusDisposed = delegate { };

            bool _disposing;

            public void Dispose()
            {
                if (_disposing) return;

                try
                {
                    _disposing = true;

                    FakeBusDisposed();
                    
                    _tracker.Events.Add("FakeBus disposed");
                }
                finally
                {
                    _disposing = false;
                }
            }
        }

        class EventTracker
        {
            public readonly List<string> Events = new List<string>();
        }

        class Disposable1 : IDisposable
        {
            readonly EventTracker _tracker;

            public Disposable1(Disposable2 innerDisposable, EventTracker tracker)
            {
                _tracker = tracker;
            }

            public void Dispose()
            {
                _tracker.Events.Add("Disposable1 disposed");
            }
        }

        class Disposable2 : IDisposable
        {
            readonly EventTracker _tracker;

            public Disposable2(EventTracker tracker)
            {
                _tracker = tracker;
            }

            public void Dispose()
            {
                _tracker.Events.Add("Disposable2 disposed");
            }
        }
    }
}