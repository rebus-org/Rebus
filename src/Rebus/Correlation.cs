using System;
using System.Linq.Expressions;
using Ponder;

namespace Rebus
{
    public abstract class Correlation
    {
        internal abstract string SagaDataPropertyPath { get; }
        internal abstract string MessagePropertyPath { get; }
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

        internal override string MessagePropertyPath
        {
            get { return messagePropertyPath; }
        }

        public Func<TMessage, object> MessageProperty
        {
            get { return messageProperty.Compile(); }
        }

        internal abstract string FieldFromSagaData(object sagaData);
    }
}