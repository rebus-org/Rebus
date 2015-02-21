using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tests
{
    public abstract class FixtureBase
    {
        readonly List<IDisposable> _disposables = new List<IDisposable>();

        [SetUp]
        public void _SetUp()
        {
            _disposables.Clear();

            SetUp();
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
