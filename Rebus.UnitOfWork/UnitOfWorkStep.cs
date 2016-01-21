using System;
using System.Threading.Tasks;
using Rebus.Pipeline;

namespace Rebus.UnitOfWork
{
    [StepDocumentation("Wraps the invocation of the rest of the pipeline in a unit of work, which will get committed/rolled back depending on the outcome of calling the rest of the pipeline.")]
    class UnitOfWorkStep<TUnitOfWork> : IIncomingStep
    {
        readonly Func<IMessageContext, Task<TUnitOfWork>> _unitOfWorkFactoryMethod;
        readonly Func<IMessageContext, TUnitOfWork, Task> _commitAction;
        readonly Func<IMessageContext, TUnitOfWork, Task> _rollbackAction;
        readonly Func<IMessageContext, TUnitOfWork, Task> _cleanupAction;

        public UnitOfWorkStep(Func<IMessageContext, Task<TUnitOfWork>> unitOfWorkFactoryMethod, Func<IMessageContext, TUnitOfWork, Task> commitAction, Func<IMessageContext, TUnitOfWork, Task> rollbackAction, Func<IMessageContext, TUnitOfWork, Task> cleanupAction)
        {
            _unitOfWorkFactoryMethod = unitOfWorkFactoryMethod;
            _commitAction = commitAction;
            _rollbackAction = rollbackAction;
            _cleanupAction = cleanupAction;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var messageContext = MessageContext.Current;

            if (messageContext == null)
            {
                throw new ApplicationException("Could not find a message context! Something is clearly wrong...");
            }

            var unitOfWork = await _unitOfWorkFactoryMethod(messageContext);

            try
            {
                await next();
                await _commitAction(messageContext, unitOfWork);
            }
            catch (Exception)
            {
                await _rollbackAction(messageContext, unitOfWork);
                throw;
            }
            finally
            {
                await _cleanupAction(messageContext, unitOfWork);
            }
        }
    }
}