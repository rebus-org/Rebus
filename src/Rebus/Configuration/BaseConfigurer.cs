using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Base class of all configurers. Defines what a configurer has access to, and might provide some
    /// conveniene methods to do common configuration stuff.
    /// </summary>
    public abstract class BaseConfigurer
    {
        readonly ConfigurationBackbone backbone;

        protected BaseConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        internal ConfigurationBackbone Backbone
        {
            get { return backbone; }
        }

        public void AddDecoration(Action<ConfigurationBackbone> decorationStep)
        {
            Backbone.AddDecoration(decorationStep);
        }
    }
}