using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Rebus
{
    public abstract class Saga
    {
        internal ConcurrentDictionary<Type, Correlation> Correlations { get; set; }
        internal bool Complete { get; set; }
        public abstract void ConfigureHowToFindSaga();
    }

    public abstract class Saga<TData> : Saga where TData : ISagaData
    {
        protected Saga()
        {
            Correlations = new ConcurrentDictionary<Type, Correlation>();
        }

        protected Correlator<TData, TMessage> Incoming<TMessage>(Func<TMessage, object> messageProperty) where TMessage : class
        {
            return new Correlator<TData, TMessage>(messageProperty, this);
        }

        public TData Data { get; set; }

        protected void MarkAsComplete()
        {
            Complete = true;
        }
    }
}