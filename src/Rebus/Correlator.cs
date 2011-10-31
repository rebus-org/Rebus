using System;
using System.Linq.Expressions;
using Ponder;

namespace Rebus
{
    public class Correlator<TData, TMessage> : Correlation where TData : ISagaData
    {
        readonly Delegate messageProperty;
        readonly Saga<TData> saga;
        readonly string messagePropertyPath;
        string sagaDataPropertyPath;

        public Correlator(Expression<Func<TMessage, object>> messageProperty, Saga<TData> saga) 
        {
            this.messageProperty = messageProperty.Compile();
            this.saga = saga;
            messagePropertyPath = Reflect.Path(messageProperty);
        }

        internal override string SagaDataPropertyPath
        {
            get { return sagaDataPropertyPath; }
        }

        internal override string MessagePropertyPath
        {
            get { return messagePropertyPath; }
        }

        public override string FieldFromMessage<TMessage2>(TMessage2 message)
        {
            if (typeof(TMessage) != typeof(TMessage2))
            {
                throw new InvalidOperationException(
                    string.Format("Cannot extract {0} field from message of type {1} with func that takes a {2}",
                                  messagePropertyPath, typeof (TMessage2), typeof (TMessage)));
            }

            var property = (Func<TMessage2, object>)messageProperty;

            return (property(message) ?? "").ToString();
        }

        public void CorrelatesWith(Expression<Func<TData,object>> sagaDataProperty)
        {
            sagaDataPropertyPath = Reflect.Path(sagaDataProperty);

            saga.Correlations.TryAdd(typeof (TMessage), this);
        }
    }
}