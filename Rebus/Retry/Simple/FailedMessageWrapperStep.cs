using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Pipeline;
// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable UnusedMethodReturnValue.Local

namespace Rebus.Retry.Simple;

[StepDocumentation(@"When 2nd level retries are enabled, a message that has failed too many times must be dispatched as a IFailed<TMessage>.

This is carried out by having the retry step add the '" + DefaultRetryStep.DispatchAsFailedMessageKey + @"' key to the context,
which is then detected by this wrapper step.")]
sealed class FailedMessageWrapperStep : IIncomingStep
{
    readonly IErrorTracker _errorTracker;

    public FailedMessageWrapperStep(IErrorTracker errorTracker) => _errorTracker = errorTracker ?? throw new ArgumentNullException(nameof(errorTracker));

    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        if (context.Load<bool>(DefaultRetryStep.DispatchAsFailedMessageKey))
        {
            var originalMessage = context.Load<Message>();

            var messageId = originalMessage.GetMessageId();
            var fullErrorDescription = await _errorTracker.GetFullErrorDescription(messageId) ?? "(not available in the error tracker!)";
            var exceptions = await _errorTracker.GetExceptions(messageId);
            var headers = originalMessage.Headers;
            var body = originalMessage.Body;
            var wrappedBody = WrapInFailed(headers, body, fullErrorDescription, exceptions);

            context.Save(new Message(headers, wrappedBody));
        }

        await next();
    }

    static readonly ConcurrentDictionary<Type, MethodInfo> WrapperMethods = new();

    static readonly MethodInfo GenericWrapMethod =
        typeof(FailedMessageWrapperStep).GetMethod(nameof(Wrap), BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidProgramException($"Could not find non-public instance method named {nameof(Wrap)} on {typeof(FailedMessageWrapperStep)}");

    static object WrapInFailed(Dictionary<string, string> headers, object body, string errorDescription, IEnumerable<ExceptionInfo> exceptions)
    {
        if (headers == null) throw new ArgumentNullException(nameof(headers));
        if (body == null) throw new ArgumentNullException(nameof(body));

        try
        {
            return WrapperMethods
                .GetOrAdd(body.GetType(), type => GenericWrapMethod.MakeGenericMethod(type))
                .Invoke(null, new[] { headers, body, errorDescription, exceptions });
        }
        catch (Exception exception)
        {
            throw new RebusApplicationException(exception, $"Could not wrap {body} in FailedMessageWrapper<>");
        }
    }

    static IFailed<TMessage> Wrap<TMessage>(Dictionary<string, string> headers, TMessage body, string errorDescription, IEnumerable<ExceptionInfo> exceptions)
    {
        return new FailedMessageWrapper<TMessage>(headers, body, errorDescription, exceptions);
    }
}