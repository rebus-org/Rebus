using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Tests.Contracts;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.Tests.Assumptions;

[TestFixture]
[Description("Checks how Thread.CurrentPrincipal flows/doesn't flow in various places")]
public class TestThreadCurrentPrincipalFlowAndTransactionContextCallbacks : FixtureBase
{
    [Test]
    public async Task CheckHowItWorks()
    {
        var futureUsernameInHandlerBeforeAwait = new TaskCompletionSource<string>();
        var futureUsernameInHandlerAfterAwait = new TaskCompletionSource<string>();
        var futureUsernameInTxCtxCommitted = new TaskCompletionSource<string>();
        var futureUsernameInTxCtxDisposed = new TaskCompletionSource<string>();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<object>(async _ =>
        {
            SetResult(futureUsernameInHandlerBeforeAwait);
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            SetResult(futureUsernameInHandlerAfterAwait);
        });

        var customUsername = $"custom username {Guid.NewGuid():N}";

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "flowtest"))
            .Options(o =>
            {
                o.Decorate<IPipeline>(c => new PipelineStepInjector(pipeline: c.Get<IPipeline>())
                    .OnReceive(
                        step: new TransactionContextCallbacksStep(
                            customUsername: customUsername,
                            futureUsernameInTxCtxCommitted: futureUsernameInTxCtxCommitted,
                            futureUsernameInTxCtxDisposed: futureUsernameInTxCtxDisposed
                        ),
                        position: PipelineRelativePosition.Before,
                        anchorStep: typeof(DeserializeIncomingMessageStep)
                    ));
            })
            .Start();

        await activator.Bus.SendLocal("HEJ");

        await Task.WhenAll(
                futureUsernameInHandlerBeforeAwait.Task,
                futureUsernameInHandlerAfterAwait.Task,
                futureUsernameInTxCtxCommitted.Task,
                futureUsernameInTxCtxDisposed.Task
            )
            .WithTimeout(TimeSpan.FromSeconds(2));

        var usernameInHandlerBeforeAwait = await futureUsernameInHandlerBeforeAwait.Task;
        var usernameInHandlerAfterAwait = await futureUsernameInHandlerAfterAwait.Task;
        var usernameInTxCtxCommitted = await futureUsernameInTxCtxCommitted.Task;
        var usernameInTxCtxDisposed = await futureUsernameInTxCtxDisposed.Task;

        Assert.That(usernameInHandlerBeforeAwait, Is.EqualTo(customUsername));
        Assert.That(usernameInHandlerAfterAwait, Is.EqualTo(customUsername));
        Assert.That(usernameInTxCtxCommitted, Is.EqualTo(customUsername));
        Assert.That(usernameInTxCtxDisposed, Is.EqualTo(customUsername));
    }

    [StepDocumentation("Incoming pipeline step that changes Thread.CurrentPrincipal for the rest of the pipeline anf checks the current principal's name in a few, crucial places.")]
    class TransactionContextCallbacksStep : IIncomingStep
    {
        readonly TaskCompletionSource<string> _futureUsernameInTxCtxCommitted;
        readonly TaskCompletionSource<string> _futureUsernameInTxCtxDisposed;
        readonly string _customUsername;

        public TransactionContextCallbacksStep(
            string customUsername,
            TaskCompletionSource<string> futureUsernameInTxCtxCommitted,
            TaskCompletionSource<string> futureUsernameInTxCtxDisposed)
        {
            _customUsername = customUsername;
            _futureUsernameInTxCtxCommitted = futureUsernameInTxCtxCommitted;
            _futureUsernameInTxCtxDisposed = futureUsernameInTxCtxDisposed;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionContext = context.Load<ITransactionContext>();
            var originalPrincipal = Thread.CurrentPrincipal;

            transactionContext.OnCommitted(async _ => SetResult(_futureUsernameInTxCtxCommitted));
            transactionContext.OnDisposed(async _ =>
            {
                SetResult(_futureUsernameInTxCtxDisposed);
                Thread.CurrentPrincipal = originalPrincipal;
            });

            Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(_customUsername), Array.Empty<string>());

            await next();
        }
    }

    static void SetResult(TaskCompletionSource<string> taskCompletionSource) => Task.Run(() => taskCompletionSource.SetResult(Thread.CurrentPrincipal?.Identity?.Name));
}