using System;

namespace Rebus
{
    internal class Disposable : IDisposable
    {
        readonly Action disposer;

        Disposable(Action disposer)
        {
            this.disposer = disposer;
        }

        public static IDisposable Create(Action disposer)
        {
            return new Disposable(disposer);
        }

        public void Dispose()
        {
            disposer();
        }
    }
}