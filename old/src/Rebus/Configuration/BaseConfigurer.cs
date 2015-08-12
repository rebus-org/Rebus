using System;
using Rebus.Bus;

namespace Rebus.Configuration
{
    /// <summary>
    /// Base class of all configurers. Defines what a configurer has access to, and might provide some
    /// conveniene methods to do common configuration stuff.
    /// </summary>
    public abstract class BaseConfigurer
    {
        readonly ConfigurationBackbone backbone;

        /// <summary>
        /// Uses the specified <see cref="ConfigurationBackbone"/> to store references to implementations of all Rebus' abstractions
        /// </summary>
        internal protected BaseConfigurer(ConfigurationBackbone backbone)
        {
            if (backbone == null) throw new ArgumentNullException("backbone", "Dude, don't try to create a configurer without a backbone!");

            this.backbone = backbone;
        }

        /// <summary>
        /// Accesses the backbone that this configurer is currently applying its configurations to
        /// </summary>
        public ConfigurationBackbone Backbone
        {
            get { return backbone; }
        }

        /// <summary>
        /// Adds the specified function as a decoration step which will be executed when it's time to instantiate <see cref="RebusBus"/>
        /// </summary>
        public void AddDecoration(Action<ConfigurationBackbone> decorationStep)
        {
            Backbone.AddConfigurationStep(decorationStep);
        }
    }
}