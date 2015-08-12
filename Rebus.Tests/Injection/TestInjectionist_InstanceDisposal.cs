using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Injection;

namespace Rebus.Tests.Injection
{
    [TestFixture]
    public class TestInjectionist_InstanceDisposal : FixtureBase
    {
        [Test]
        public void InjectedWhateverWithWhateverInsideIsProperlyDisposed()
        {
            var injectionist = new Injectionist();
            var eventTracker = new EventTracker();

            injectionist.Register(c =>
            {
                var fakeBus = new FakeBus(c.Get<Disposable1>(), c.Get<EventTracker>());

                fakeBus.FakeBusDisposed += () =>
                {
                    foreach (var disposable in c.GetTrackedInstancesOf<IDisposable>().Reverse())
                    {
                        disposable.Dispose();
                    }
                };

                foreach (var disposable in c.GetTrackedInstancesOf<IInitializable>())
                {
                    disposable.Initialize();
                }

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
                "EventTracker initialized",
                "Disposable2 initialized",
                "Disposable1 initialized",
                "Disposable1 disposed",
                "Disposable2 disposed",
                "EventTracker disposed",
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

        class EventTracker: IDisposable, IInitializable
        {
            public readonly List<string> Events = new List<string>();
            
            public void Dispose()
            {
                Events.Add("EventTracker disposed");
            }

            public void Initialize()
            {
                Events.Add("EventTracker initialized");
            }
        }

        class Disposable1 : IDisposable, IInitializable
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

            public void Initialize()
            {
                _tracker.Events.Add("Disposable1 initialized");
            }
        }

        class Disposable2 : IDisposable, IInitializable
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

            public void Initialize()
            {
                _tracker.Events.Add("Disposable2 initialized");
            }
        }
    }
}