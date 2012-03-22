using System;
using System.Linq.Expressions;
using Ponder;

namespace Rebus
{
    public class Correlator<TData, TMessage> : Correlation where TData : ISagaData where TMessage : class
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

        public override object FieldFromMessage(object message)
        {
            var typedMessage = message as TMessage;
            if (typedMessage == null)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot extract {0} field from message of type {1} with func that takes a {2}",
                                  messagePropertyPath, message.GetType(), typeof (TMessage)));
            }

            var property = (Func<TMessage, object>)messageProperty;

            return property(typedMessage);
        }

        public void CorrelatesWith(Expression<Func<TData,object>> sagaDataProperty)
        {
            sagaDataPropertyPath = Reflect.Path(sagaDataProperty);

            saga.Correlations.TryAdd(typeof (TMessage), this);
        }
    }
}