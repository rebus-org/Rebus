using System;
using System.Linq.Expressions;

namespace Rebus.Sagas
{
    public interface ICorrelationConfig<TSagaData>
    {
        void Correlate<TMessage>(Func<TMessage, object> messageValueExtractor,
            Expression<Func<TSagaData, object>> sagaDataValueExpression);
    }
}