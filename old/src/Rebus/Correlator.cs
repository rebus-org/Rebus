using System;
using System.Linq.Expressions;
using Ponder;

namespace Rebus
{
    /// <summary>
    /// Element of the fluent syntax used to configure correlations between incoming messages and saga data
    /// </summary>
    public class Correlator<TData, TMessage> : Correlation where TData : ISagaData where TMessage : class
    {
        readonly Func<TMessage, object> messageProperty;
        readonly Saga<TData> saga;
        string sagaDataPropertyPath;

        internal Correlator(Func<TMessage, object> messageProperty, Saga<TData> saga) 
        {
            this.messageProperty = messageProperty;
            this.saga = saga;
        }

        internal override string SagaDataPropertyPath
        {
            get { return sagaDataPropertyPath; }
        }

        internal override object FieldFromMessage(object message)
        {
            var typedMessage = message as TMessage;
            if (typedMessage == null)
            {
                throw new InvalidOperationException(
                    string.Format("Message was {0}, but {1} was expected", message.GetType(), typeof (TMessage)));
            }

            return messageProperty(typedMessage);
        }

        /// <summary>
        /// Invokes the final part of the syntax that completes the configuration of this correlation
        /// </summary>
        public void CorrelatesWith(Expression<Func<TData,object>> sagaDataProperty)
        {
            sagaDataPropertyPath = Reflect.Path(sagaDataProperty);

            saga.Correlations.TryAdd(typeof (TMessage), this);
        }
    }
}