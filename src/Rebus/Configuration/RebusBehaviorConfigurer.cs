using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that configures various behavioral aspects of Rebus
    /// </summary>
    public class RebusBehaviorConfigurer : BaseConfigurer
    {
        internal RebusBehaviorConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {


        }

        /// <summary>
        /// Customizes the max number of retries for exceptions of this type. Note that the order of calls will determine
        /// the priority of the customizations, and customizations of base classes will affect derivations as well.
        /// E.g. if you start out by setting max retries for <see cref="Exception"/> to 5 and subsequently set max retries
        /// for <see cref="ApplicationException"/> to 200, all exceptions will result in 5 retries because all exceptions
        /// are derived from <see cref="Exception"/>.
        /// </summary>
        public RebusBehaviorConfigurer SetMaxRetriesFor<TException>(int maxRetriesForThisExceptionType) where TException : Exception
        {
            Backbone.AddDecoration(b => b.ErrorTracker.SetMaxRetriesFor<TException>(maxRetriesForThisExceptionType));
            return this;
        }
    }
}