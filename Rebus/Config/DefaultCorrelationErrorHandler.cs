using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;

namespace Rebus.Config;

class DefaultCorrelationErrorHandler : ICorrelationErrorHandler
{
    readonly ILog _log;

    public DefaultCorrelationErrorHandler(IRebusLoggerFactory rebusLoggerFactory)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        _log = rebusLoggerFactory.GetLogger<DefaultCorrelationErrorHandler>();
    }

    public Task HandleCorrelationError(SagaDataCorrelationProperties properties, HandlerInvoker handlerInvoker,
        Message message)
    {
        _log.Debug("Could not find existing saga data for message {messageLabel}", message.GetMessageLabel());
        handlerInvoker.SkipInvocation();
        return Task.CompletedTask;
    }
}