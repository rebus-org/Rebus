using System;
using System.Collections.Concurrent;
using System.IO;
using NUnit.Framework;
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

        protected virtual void TearDown()
        {
        }

        protected TDisposable Using<TDisposable>(TDisposable disposable) where TDisposable : IDisposable
        {
            _disposables.Push(disposable);
            return disposable;
        }

        protected string GetTempFilePath()
        {
            var tempFile = Path.GetTempFileName();
            Using(new FileDeleter(tempFile));
            return tempFile;
        }

        class FileDeleter : IDisposable
        {
            readonly string _filePath;

            public FileDeleter(string filePath)
            {
                _filePath = filePath;
            }

            public void Dispose()
            {
                try
                {
                    File.Delete(_filePath);
                }
                catch (FileNotFoundException)
                {
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Could not delete file {_filePath}: {exception}");
                }
            }
        }

        protected void CleanUpDisposables()
        {
            IDisposable disposable;

            while (_disposables.TryPop(out disposable))
            {
                Console.WriteLine($"Disposing {disposable}");
                disposable.Dispose();
            }
        }
    }
}
