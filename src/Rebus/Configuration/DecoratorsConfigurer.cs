using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that allows for decorators to be added in the form of "decoration steps"
    /// </summary>
    public class DecoratorsConfigurer : BaseConfigurer
    {
        public DecoratorsConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        public void AddDecoration(Action<ConfigurationBackbone> decorationStep)
        {
            Backbone.AddDecoration(decorationStep);
        }
    }
}