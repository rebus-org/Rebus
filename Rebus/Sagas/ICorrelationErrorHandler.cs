using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline.Receive;

namespace Rebus.Sagas;

/// <summary>
/// Interface of Rebus' correlation error handler, which will be invoked when an incoming message cannot be correlated
/// with an existing instance of saga data, and the message is not allowed to initiate a new saga.
/// </summary>
public interface ICorrelationErrorHandler
{
    /// <summary>
    /// This method will be invoked when an incoming message cannot be correlated
    /// with an existing instance of saga data, and the message is not allowed to initiate a new saga.
    /// </summary>
    /// <param name="correlationProperties">
    /// Full collection of <see cref="CorrelationProperty"/> instances available for this Rebus instance. Call <see cref="SagaDataCorrelationProperties.ForMessage"/>
    /// </param>
    /// <param name="handlerInvoker"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    Task HandleCorrelationError(SagaDataCorrelationProperties correlationProperties, HandlerInvoker handlerInvoker, Message message);
}