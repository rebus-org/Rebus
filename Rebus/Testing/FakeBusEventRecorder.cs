using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rebus.Testing.Events;

namespace Rebus.Testing
{
    class FakeBusEventRecorder
    {
        readonly ConcurrentQueue<FakeBusEvent> _events = new ConcurrentQueue<FakeBusEvent>();
        readonly List<Delegate> _callbacks = new List<Delegate>();

        public IEnumerable<FakeBusEvent> Events => _events.ToList();

        public IEnumerable<FakeBusEvent> GetEvents()
        {
            return _events.ToList();
        }

        public void AddCallback<TEvent>(Action<TEvent> callback)
        {
            _callbacks.Add(callback);
        }

        public void Clear()
        {
            FakeBusEvent instance;
            while (_events.TryDequeue(out instance)) { }
        }

        public void Record(FakeBusEvent fakeBusEvent)
        {
            AddFakeBusEvent(fakeBusEvent);

            InvokeCompatibleCallbacks(fakeBusEvent);
        }

        void AddFakeBusEvent(FakeBusEvent fakeBusEvent)
        {
            _events.Enqueue(fakeBusEvent);
        }

        void InvokeCompatibleCallbacks(FakeBusEvent fakeBusEvent)
        {
            foreach (var callback in _callbacks)
            {
                var compatibleHandlerType = typeof(Action<>).MakeGenericType(fakeBusEvent.GetType());

                if (!compatibleHandlerType.IsInstanceOfType(callback)) continue;

                try
                {
                    callback.DynamicInvoke(fakeBusEvent);
                }
                catch (Exception exception)
                {
                    throw new TargetInvocationException($"Error invoking callback for fake bus event {fakeBusEvent}", exception);
                }
            }
        }
    }
}