using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Rebus
{
    public abstract class Saga
    {
        internal abstract ConcurrentDictionary<Type, Correlation> Correlations { get; set; }
        internal abstract bool Complete { get; set; }
        public abstract void ConfigureHowToFindSaga();
    }

    public class Saga<TData> : Saga where TData : ISagaData
    {
        public Saga()
        {
            Correlations = new ConcurrentDictionary<Type, Correlation>();
        }

        internal override ConcurrentDictionary<Type, Correlation> Correlations { get; set; }
        
        internal override bool Complete { get; set; }

        public override void ConfigureHowToFindSaga()
        {
        }

        protected Correlator<TData, TMessage> Incoming<TMessage>(Expression<Func<TMessage, object>> messageProperty)
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