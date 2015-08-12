using System;
using System.Linq.Expressions;

namespace Rebus.Sagas
{
    /// <summary>
    /// Sets up the saga instance correlation configuration, i.e. it configures how the following question should be answered:
    /// "given this incoming message, how should Rebus figure out which saga instance should be loaded to handle it?"
    /// </summary>
    public interface ICorrelationConfig<TSagaData>
    {
        /// <summary>
        /// Correlates an incoming message of type <typeparamref name="TMessage"/>, using the specified <paramref name="messageValueExtractorFunction"/> to
        /// extract a value from the message. The value will be used when looking up a saga data instance using the specified <paramref name="sagaDataValueExpression"/>.
        /// You could for example do something like this:
        /// <code>
        /// config.Correlate&lt;TradeApproved&gt;(t => t.Id, d => d.TradeId);
        /// </code>
        /// to look up a saga instance by the "TradeId" field, querying by the value of the "Id" property of the incoming "TradeApproved" message.
        /// </summary>
        /// <typeparam name="TMessage">Specifies the message type to configure a correlation for</typeparam>
        /// <param name="messageValueExtractorFunction">Configures a function to extract a value from the message. Since this is just a function, it may contain logic that e.g. concatenates fields, calls other functions, etc.</param>
        /// <param name="sagaDataValueExpression">Configures an expression, which will be used when querying the chosen <see cref="ISagaStorage"/> - since this is an expression, it must point to a simple property of the relevant <typeparamref name="TSagaData"/>.</param>
        void Correlate<TMessage>(Func<TMessage, object> messageValueExtractorFunction, Expression<Func<TSagaData, object>> sagaDataValueExpression);
    }
}