using System;
using System.Collections.Concurrent;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Time;

namespace Rebus.Tests
{
    public abstract class FixtureBase
    {
        readonly ConcurrentStack<IDisposable> _disposables = new ConcurrentStack<IDisposable>();

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

        protected virtual void TearDown()
        {
        }

        protected TDisposable Using<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
        {
            _disposables.Push(disposable);
            return disposable;
        }

        protected void CleanUpDisposables()
        {
            IDisposable disposable;

            while (_disposables.TryPop(out disposable))
            {
                Console.WriteLine("Disposing {0}", disposable);
                
                disposable.Dispose();
            }
        }
    }
}
