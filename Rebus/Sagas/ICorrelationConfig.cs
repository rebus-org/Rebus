using System;
using System.Linq.Expressions;
using Rebus.Pipeline;

namespace Rebus.Sagas;

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

    /// <summary>
    /// An overload of <see cref="Correlate{TMessage}(System.Func{TMessage,object},System.Linq.Expressions.Expression{System.Func{TSagaData,object}})"/> that allows the saga
    /// data property that is used for correlation to be specified as a string.
    /// You could for example do something like this:
    /// <code>
    /// config.Correlate&lt;TradeApproved&gt;(t => t.Id, nameof(ApprovedTradeSagaData.TradeId));
    /// </code>
    /// </summary>
    /// <typeparam name="TMessage">Specifies the message type to configure a correlation for</typeparam>
    /// <param name="messageValueExtractorFunction">Configures a function to extract a value from the message. Since this is just a function, it may contain logic that e.g.
    /// concatenates fields, calls other functions, etc.</param>
    /// <param name="sagaDataPropertyName">The name of the property of the chosen <see cref="ISagaStorage"/> that will be used when querying for the correlation value. It must
    /// point to a simple property of the relevant <typeparamref name="TSagaData"/>.</param>
    void Correlate<TMessage>(Func<TMessage, object> messageValueExtractorFunction, string sagaDataPropertyName);

    /// <summary>
    /// Correlates an incoming message of type <typeparamref name="TMessage"/> using the header with the given <paramref name="headerKey"/>. The value will be used when looking up a saga data instance using the specified <paramref name="sagaDataValueExpression"/>.
    /// You could for example do something like this:
    /// <code>
    /// config.CorrelateHeader&lt;TradeApproved&gt;("trade-corr-id", d => d.TradeId);
    /// </code>
    /// to look up a saga instance by the "TradeId" field, querying by the value of the "trade-corr-id" header of the incoming "TradeApproved" message.
    /// </summary>
    /// <typeparam name="TMessage">Specifies the message type to configure a correlation for</typeparam>
    /// <param name="headerKey">Configures a header key which will be extracted from the incoming message</param>
    /// <param name="sagaDataValueExpression">Configures an expression, which will be used when querying the chosen <see cref="ISagaStorage"/> - since this is an expression, it must point to a simple property of the relevant <typeparamref name="TSagaData"/>.</param>
    void CorrelateHeader<TMessage>(string headerKey, Expression<Func<TSagaData, object>> sagaDataValueExpression);

    /// <summary>
    /// An overload of <see cref="CorrelateHeader{TMessage}(string,System.Linq.Expressions.Expression{System.Func{TSagaData,object}})"/> that allows the saga
    /// data property that is used for correlation to be specified as a string.
    /// You could for example do something like this:
    /// <code>
    /// config.CorrelateHeader&lt;TradeApproved&gt;("trade-corr-id", nameof(ApprovedTradeSagaData.TradeId));
    /// </code>
    /// to look up a saga instance by the "TradeId" field, querying by the value of the "trade-corr-id" header of the incoming "TradeApproved" message.
    /// </summary>
    /// <typeparam name="TMessage">Specifies the message type to configure a correlation for</typeparam>
    /// <param name="headerKey">Configures a header key which will be extracted from the incoming message</param>
    /// <param name="sagaDataPropertyName">The name of the property of the chosen <see cref="ISagaStorage"/> that will be used when querying for the correlation value. It must
    /// point to a simple property of the relevant <typeparamref name="TSagaData"/>.</param>
    void CorrelateHeader<TMessage>(string headerKey, string sagaDataPropertyName);

    /// <summary>
    /// Correlates an incoming message of type <typeparamref name="TMessage"/> using the message context to get a value (e.g. by selecting certain headers, combining them, etc)
    ///  The value will be used when looking up a saga data instance using the specified <paramref name="sagaDataValueExpression"/>.
    /// <typeparam name="TMessage">Specifies the message type to configure a correlation for</typeparam>
    /// <param name="contextValueExtractorFunction">Configures a function that can extract a value from the current <see cref="IMessageContext"/></param>
    /// <param name="sagaDataValueExpression">Configures an expression, which will be used when querying the chosen <see cref="ISagaStorage"/> - since this is an expression, it must point to a simple property of the relevant <typeparamref name="TSagaData"/>.</param>
    /// </summary>
    void CorrelateContext<TMessage>(Func<IMessageContext, object> contextValueExtractorFunction, Expression<Func<TSagaData, object>> sagaDataValueExpression);

    /// <summary>
    /// An overload of <see cref="CorrelateContext{TMessage}(System.Func{Rebus.Pipeline.IMessageContext,object},System.Linq.Expressions.Expression{System.Func{TSagaData,object}})"/>
    /// that allows correlation an incoming message of type <typeparamref name="TMessage"/> using the message context to get a value (e.g. by selecting certain headers, combining them, etc)
    ///  The value will be used when looking up a saga data instance is the value of the property whose name is specified by <paramref name="sagaDataPropertyName"/>.
    /// You could for example do something like this:
    /// <code>
    /// config.CorrelateContext&lt;TradeApproved&gt;(t => t.Id, nameof(ApprovedTradeSagaData.TradeId));
    /// </code>
    /// to look up a saga instance by the "TradeId" field, querying by the value of the "Id" property of the incoming "TradeApproved" message.
    /// </summary>
    /// <typeparam name="TMessage">Specifies the message type to configure a correlation for</typeparam>
    /// <param name="contextValueExtractorFunction">Configures a function that can extract a value from the current <see cref="IMessageContext"/></param>
    /// <param name="sagaDataPropertyName">The name of the property of the chosen <see cref="ISagaStorage"/> that will be used when querying for the correlation value. It must
    /// point to a simple property of the relevant <typeparamref name="TSagaData"/>.</param>
    void CorrelateContext<TMessage>(Func<IMessageContext, object> contextValueExtractorFunction, string sagaDataPropertyName);
}