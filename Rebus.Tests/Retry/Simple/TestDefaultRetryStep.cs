using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Moq;
using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Retry.FailFast;
using Rebus.Retry.Simple;

namespace Rebus.Tests.Retry.Simple;

[TestFixture]
public class TestDefaultRetryStep
{
    [Test, AutoMoq]
    public async Task CreatesInfoForEmptyMessageException(
        [Frozen] Mock<IExceptionInfoFactory> exceptionInfoFactory,
        DefaultRetryStep step, IncomingStepContext context)
    {
        await step.Process(context, () => Task.CompletedTask);

        exceptionInfoFactory.Verify(eif => eif.CreateInfo(It.Is<RebusApplicationException>(e => e.Message.Contains("empty"))));
    }

    [Test, AutoMoq]
    public async Task CreatesInfoForStepExceptionWhenShouldFailFast(
        [Frozen] Mock<IFailFastChecker> failFastChecker,
        [Frozen] Mock<IExceptionInfoFactory> exceptionInfoFactory,
        DefaultRetryStep step, IncomingStepContext context,
        TransportMessage message, string id, Exception nextException)
    {
        failFastChecker.Setup(ffc => ffc.ShouldFailFast(id, nextException)).Returns(true);
        message.Headers[Headers.MessageId] = id;
        context.Save(message);

        await step.Process(context, () => throw nextException);

        exceptionInfoFactory.Verify(eif => eif.CreateInfo(nextException));
    }

    [Test, AutoMoq]
    public async Task CreatesInfoForSecondLevelRetryException(
        Mock<IRebusLoggerFactory> rebusLoggerFactory,
        Mock<IErrorHandler> errorHandler,
        Mock<IErrorTracker> errorTracker,
        Mock<IFailFastChecker> failFastChecker,
        Mock<IExceptionInfoFactory> exceptionInfoFactory,
        IncomingStepContext context,
        TransportMessage message, string id,
        Exception[] nextExceptions)
    {
        failFastChecker.Setup(ffc => ffc.ShouldFailFast(It.IsAny<string>(), It.IsAny<Exception>())).Returns(false);
        errorTracker.Setup(et => et.HasFailedTooManyTimes(It.IsAny<string>())).ReturnsAsync(true);
        var step = new DefaultRetryStep(rebusLoggerFactory.Object, errorHandler.Object, errorTracker.Object, failFastChecker.Object, exceptionInfoFactory.Object, new(secondLevelRetriesEnabled: true), CancellationToken.None);
        message.Headers[Headers.MessageId] = id;
        context.Save(message);

        int i = 0;
        await step.Process(context, () => throw nextExceptions[i++]);

        exceptionInfoFactory.Verify(eif => eif.CreateInfo(nextExceptions[1]));
    }
}