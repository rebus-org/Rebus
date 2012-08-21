using System;

namespace Rebus.Configuration
{
    public class DecoratorsConfigurer
    {
        readonly ConfigurationBackbone backbone;

        public DecoratorsConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }

        public void AddDecoration(Action<ConfigurationBackbone> decorationStep)
        {
            backbone.AddDecoration(decorationStep);
        }
    }
}