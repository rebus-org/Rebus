using System;

namespace Rebus.Bus
{
    /// <summary>
    /// Has events that can be subscribed to if one wants to be notified when certain things happen
    /// </summary>
    public class BusLifetimeEvents
    {
        /// <summary>
        /// Event that is raised when the bus is disposed BEFORE the bus raises its own <see cref="RebusBus.Disposed"/> event
        /// </summary>
        public event Action BusDisposing;

        /// <summary>
        /// Event that is raised when the bus is disposed AFTER the bus raises its own <see cref="RebusBus.Disposed"/> event
        /// </summary>
        public event Action BusDisposed;

        internal void RaiseBusDisposing()
        {
            BusDisposing?.Invoke();
        }

        internal void RaiseBusDisposed()
        {
            BusDisposed?.Invoke();
        }
    }
}