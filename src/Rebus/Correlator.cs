using System;
using System.Linq.Expressions;
using Ponder;

namespace Rebus
{
    public class Correlator<TData, TMessage> : Correlation<TMessage> where TData : ISagaData
    {
        readonly Saga<TData> saga;
        string sagaDataPropertyPath;

        public Correlator(Expression<Func<TMessage, object>> messageProperty, Saga<TData> saga) 
            :base(messageProperty)
        {
            this.saga = saga;
        }

        internal override string SagaDataPropertyPath
        {
            get { return sagaDataPropertyPath; }
        }

        public void CorrelatesWith(Expression<Func<TData,object>> sagaDataProperty)
        {
            sagaDataPropertyPath = Reflect.Path(sagaDataProperty);

            saga.Correlations.TryAdd(typeof (TMessage), this);
        }

        internal override string FieldFromSagaData(object sagaData)
        {
            return "";
        }
    }
}