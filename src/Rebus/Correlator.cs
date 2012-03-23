using System;
using System.Linq.Expressions;
using Ponder;

namespace Rebus
{
    public class Correlator<TData, TMessage> : Correlation where TData : ISagaData where TMessage : class
    {
        readonly Func<TMessage, object> messageProperty;
        readonly Saga<TData> saga;
        string sagaDataPropertyPath;

        public Correlator(Func<TMessage, object> messageProperty, Saga<TData> saga) 
        {
            this.messageProperty = messageProperty;
            this.saga = saga;
        }

        internal override string SagaDataPropertyPath
        {
            get { return sagaDataPropertyPath; }
        }

        public override object FieldFromMessage(object message)
        {
            var typedMessage = message as TMessage;
            if (typedMessage == null)
            {
                throw new InvalidOperationException(
                    string.Format("Message was {0}, but {1} was expected", message.GetType(), typeof (TMessage)));
            }

            return messageProperty(typedMessage);
        }

        public void CorrelatesWith(Expression<Func<TData,object>> sagaDataProperty)
        {
            sagaDataPropertyPath = Reflect.Path(sagaDataProperty);

            saga.Correlations.TryAdd(typeof (TMessage), this);
        }
    }
}