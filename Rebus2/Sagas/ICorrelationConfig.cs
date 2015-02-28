using System;
using System.Linq.Expressions;

namespace Rebus2.Sagas
{
    public interface ICorrelationConfig<TSagaData>
    {
        void Correlate<TMessage>(Func<TMessage, object> messageValueExtractor,
            Expression<Func<TSagaData, object>> sagaDataValueExpression);
    }
}