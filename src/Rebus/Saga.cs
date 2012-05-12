using System;
using System.Collections.Concurrent;

namespace Rebus
{
    public abstract class Saga
    {
        internal ConcurrentDictionary<Type, Correlation> Correlations { get; set; }
        internal bool Complete { get; set; }
        public bool IsNew { get; internal set; }
        public abstract void ConfigureHowToFindSaga();
    }

    public abstract class Saga<TData> : Saga where TData : ISagaData
    {
        protected Saga()
        {
            Correlations = new ConcurrentDictionary<Type, Correlation>();
        }

        public TData Data { get; internal set; }

        protected Correlator<TData, TMessage> Incoming<TMessage>(Func<TMessage, object> messageProperty) where TMessage : class
        {
            return new Correlator<TData, TMessage>(messageProperty, this);
        }

        protected void MarkAsComplete()
        {
            Complete = true;
        }
    }
}