using System;

namespace Rebus.Configuration
{
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