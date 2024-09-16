using System;
using System.Threading;

namespace Rebus.Persistence.FileSystem;

static class ReaderWriterLockSlimExtensions
{
    public static IDisposable ReadLock(this ReaderWriterLockSlim readerWriterLockSlim)
    {
        return new DisposableReadLock(readerWriterLockSlim);
    }

    public static IDisposable WriteLock(this ReaderWriterLockSlim readerWriterLockSlim)
    {
        return new DisposableWriteLock(readerWriterLockSlim);
    }

    sealed class DisposableReadLock : IDisposable
    {
        readonly ReaderWriterLockSlim _readerWriterLockSlim;

        public DisposableReadLock(ReaderWriterLockSlim readerWriterLockSlim)
        {
            _readerWriterLockSlim = readerWriterLockSlim;
            _readerWriterLockSlim.EnterReadLock();
        }

        public void Dispose()
        {
            _readerWriterLockSlim.ExitReadLock();
        }
    }

    sealed class DisposableWriteLock : IDisposable
    {
        readonly ReaderWriterLockSlim _readerWriterLockSlim;

        public DisposableWriteLock(ReaderWriterLockSlim readerWriterLockSlim)
        {
            _readerWriterLockSlim = readerWriterLockSlim;
            _readerWriterLockSlim.EnterWriteLock();
        }

        public void Dispose()
        {
            _readerWriterLockSlim.ExitWriteLock();
        }
    }
}