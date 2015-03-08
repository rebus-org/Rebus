using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Time;

namespace Rebus.Tests
{
    public abstract class FixtureBase
    {
        readonly List<IDisposable> _disposables = new List<IDisposable>();

        [SetUp]
        public void _SetUp()
        {
            RebusTimeMachine.Reset();

            AdjustLogging(LogLevel.Debug);

            _disposables.Clear();

            SetUp();
        }

        protected static void AdjustLogging(LogLevel minLevel)
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false)
            {
                MinLevel = minLevel
            };
        }

        [TearDown]
        public void _TearDown()
        {
            CleanUpDisposables();

            TearDown();
        }

        protected virtual void SetUp()
        {
        }

        protected TDisposable TrackDisposable<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
        {
            _disposables.Add(disposable);
            return disposable;
        }

        protected virtual void TearDown()
        {
        }

        protected void CleanUpDisposables()
        {
            _disposables.ForEach(d =>
            {
                Console.WriteLine("Disposing {0}", d);
                d.Dispose();
            });
            _disposables.Clear();
        }
    }
}
