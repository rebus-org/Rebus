using System;
using System.Linq.Expressions;
using Ponder;

namespace Rebus
{
    public abstract class Correlation
    {
    }

    public abstract class Correlation<TMessage> : Correlation
    {
        readonly Expression<Func<TMessage, object>> messageProperty;
        string messagePropertyPath;

        protected Correlation(Expression<Func<TMessage, object>> messageProperty)
        {
            this.messageProperty = messageProperty;
            messagePropertyPath = Reflect.Path(messageProperty);
        }

        public string MessagePropertyPath
        {
            get { return messagePropertyPath; }
        }

        public Func<TMessage, object> MessageProperty
        {
            get { return messageProperty.Compile(); }
        }

        internal abstract string FieldFromSagaData(object sagaData);
        internal abstract string SagaDataPropertyPath { get; }
    }
}