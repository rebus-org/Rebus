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
        public static void EnableUnitOfWork<TUnitOfWork>(this OptionsConfigurer configurer,
            Func<IMessageContext, TUnitOfWork> unitOfWorkFactoryMethod,
            Action<IMessageContext, TUnitOfWork> commitAction,
            Action<IMessageContext, TUnitOfWork> rollbackAction = null,
            Action<IMessageContext, TUnitOfWork> cleanupAction = null)
        {
            if (unitOfWorkFactoryMethod == null) throw new ArgumentNullException(nameof(unitOfWorkFactoryMethod), "You need to provide a factory method that is capable of creating new units of work");
            if (commitAction == null) throw new ArgumentNullException(nameof(commitAction), "You need to provide a commit action that commits the current unit of work");

            configurer.EnableUnitOfWork(
                unitOfWorkFactoryMethod: async context => unitOfWorkFactoryMethod(context),
                commitAction: async (context, unitOfWork) => commitAction(context, unitOfWork),
                rollbackAction: async (context, unitOfWork) => rollbackAction?.Invoke(context, unitOfWork),
                cleanupAction: async (context, unitOfWork) => cleanupAction?.Invoke(context, unitOfWork)
                );
        }

        /// <summary>
        /// Wraps the invocation of the incoming pipeline in a step that creates a unit of work, committing/rolling back depending on how the invocation of the pipeline went. The cleanup action is always called.
        /// </summary>
        public static void EnableUnitOfWork<TUnitOfWork>(this OptionsConfigurer configurer,
            Func<IMessageContext, Task<TUnitOfWork>> unitOfWorkFactoryMethod,
            Func<IMessageContext, TUnitOfWork, Task> commitAction,
            Func<IMessageContext, TUnitOfWork, Task> rollbackAction = null,
            Func<IMessageContext, TUnitOfWork, Task> cleanupAction = null)
        {
            if (unitOfWorkFactoryMethod == null) throw new ArgumentNullException(nameof(unitOfWorkFactoryMethod), "You need to provide a factory method that is capable of creating new units of work");
            if (commitAction == null) throw new ArgumentNullException(nameof(commitAction), "You need to provide a commit action that commits the current unit of work");

            configurer.Decorate<IPipeline>(context =>
            {
                var pipeline = context.Get<IPipeline>();
                var unitOfWorkStep = new UnitOfWorkStep<TUnitOfWork>(unitOfWorkFactoryMethod, commitAction, rollbackAction, cleanupAction);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(unitOfWorkStep, PipelineRelativePosition.Before, typeof(ActivateHandlersStep));
            });
        }
    }
}
