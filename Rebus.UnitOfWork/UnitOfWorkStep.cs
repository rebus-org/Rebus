using System;
using System.Threading.Tasks;
using Rebus.Pipeline;

namespace Rebus.UnitOfWork
{
    [StepDocumentation("Wraps the invocation of the rest of the pipeline in a unit of work, which will get committed/rolled back depending on the outcome of calling the rest of the pipeline.")]
    class UnitOfWorkStep<TUnitOfWork> : IIncomingStep
    {
        readonly Func<Task<TUnitOfWork>> _unitOfWorkFactoryMethod;
        readonly Func<TUnitOfWork, Task> _commitAction;
        readonly Func<TUnitOfWork, Task> _rollbackAction;
        readonly Func<TUnitOfWork, Task> _cleanupAction;

        public UnitOfWorkStep(Func<Task<TUnitOfWork>> unitOfWorkFactoryMethod, Func<TUnitOfWork,Task> commitAction, Func<TUnitOfWork, Task> rollbackAction, Func<TUnitOfWork, Task> cleanupAction)
        {
            _unitOfWorkFactoryMethod = unitOfWorkFactoryMethod;
            _commitAction = commitAction;
            _rollbackAction = rollbackAction;
            _cleanupAction = cleanupAction;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var unitOfWork = await _unitOfWorkFactoryMethod();

            try
            {
                await next();

                await _commitAction(unitOfWork);
            }
            catch (Exception)
            {
                await _rollbackAction(unitOfWork);
                throw;
            }
            finally
            {
                await _cleanupAction(unitOfWork);
            }
        }
    }
}