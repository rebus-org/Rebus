using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Rebus
{
    public class Saga<TData> : ISaga<TData> where TData:ISagaData
    {
        internal ConcurrentDictionary<Type, Correlation> correlations = new ConcurrentDictionary<Type, Correlation>();

        public ConcurrentDictionary<Type, Correlation> Correlations
        {
            get { return correlations; }
        }

        public virtual void ConfigureHowToFindSaga()
        {
        }

        protected Correlator<TData, TMessage> Incoming<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            return new Correlator<TData, TMessage>(messageProperty, this);
        }

        public TData Data { get; set; }
    }
}