using System;
using System.Threading.Tasks;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
#pragma warning disable 1998

namespace Rebus.UnitOfWork
{
    /// <summary>
    /// Configuration extensions for the unit of work API
    /// </summary>
    public static class UnitOfWorkConfigurationExtensions
    {
        /// <summary>
        /// Wraps the invocation of the incoming pipeline in a step that creates a unit of work, committing/rolling back depending on how the invocation of the pipeline went. The cleanup action is always called.
        /// </summary>
        public static void EnableUnitOfWork<TUnitOfWork>(this OptionsConfigurer configurer, Func<TUnitOfWork> unitOfWorkFactoryMethod, Action<TUnitOfWork> commitAction, Action<TUnitOfWork> rollbackAction = null, Action<TUnitOfWork> cleanupAction = null)
        {
            configurer.EnableUnitOfWork(
                unitOfWorkFactoryMethod: async () => unitOfWorkFactoryMethod(),
                commitAction: async unitOfWork => commitAction(unitOfWork),
                rollbackAction: async unitOfWork => rollbackAction?.Invoke(unitOfWork),
                cleanupAction: async unitOfWork => cleanupAction?.Invoke(unitOfWork)
                );
        }

        /// <summary>
        /// Wraps the invocation of the incoming pipeline in a step that creates a unit of work, committing/rolling back depending on how the invocation of the pipeline went. The cleanup action is always called.
        /// </summary>
        public static void EnableUnitOfWork<TUnitOfWork>(this OptionsConfigurer configurer, Func<Task<TUnitOfWork>> unitOfWorkFactoryMethod, Func<TUnitOfWork, Task> commitAction, Func<TUnitOfWork, Task> rollbackAction = null, Func<TUnitOfWork, Task> cleanupAction = null)
        {
            configurer.Decorate<IPipeline>(context =>
            {
                var pipeline = context.Get<IPipeline>();
                var unitOfWorkStep = new UnitOfWorkStep<TUnitOfWork>(unitOfWorkFactoryMethod, commitAction, rollbackAction, cleanupAction);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(unitOfWorkStep, PipelineRelativePosition.Before, typeof (ActivateHandlersStep));
            });
        }
    }
}
